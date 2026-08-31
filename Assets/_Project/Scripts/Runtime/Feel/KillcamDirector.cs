using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BankShot
{
    /// <summary>
    /// La killcam (Fase 1.4, il generatore di clip): replay geometrico dell'INTERA
    /// traiettoria del proiettile letale, con camera all'inseguimento e barre
    /// cinematiche. Parte sempre quando muore il giocatore locale; per le kill
    /// sui bot solo se il colpo era spettacolare (>= minBouncesForBotKill sponde).
    /// </summary>
    [RequireComponent(typeof(Camera), typeof(AudioListener))]
    public class KillcamDirector : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] float maxDuration = 3.5f;
        [Tooltip("Pausa sul punto d'impatto a fine replay: la kill si deve vedere")]
        [SerializeField] float killHoldDuration = 0.8f;
        [SerializeField] int minBouncesForBotKill = 1; // in sandbox quasi ogni kill merita il replay
        [SerializeField] float cameraDistance = 2.4f;
        [SerializeField] float cameraHeight = 0.7f;

        Camera killcamCamera;
        AudioListener killcamListener;
        Camera gameplayCamera;
        AudioListener gameplayListener;
        InputAction skipAction;
        string skipHint = "";
        bool playing;

        void Awake()
        {
            killcamCamera = GetComponent<Camera>();
            killcamListener = GetComponent<AudioListener>();
            killcamCamera.enabled = false;
            killcamListener.enabled = false;
            if (actions != null)
                skipAction = actions.FindActionMap("Player", throwIfNotFound: true)
                                    .FindAction("Jump", throwIfNotFound: true); // spazio: non confligge col fuoco
        }

        void OnEnable() => CombatEvents.Kill += OnKill;
        void OnDisable() => CombatEvents.Kill -= OnKill;

        void OnKill(DamageInfo info, Transform victim)
        {
            if (playing)
                return;
            if (info.Trajectory == null || info.Trajectory.Count < 2)
            {
                Debug.Log("[Killcam] saltata: nessuna traiettoria sul colpo letale");
                return;
            }

            bool victimIsLocalPlayer = PlayerAvatar.Local != null && victim == PlayerAvatar.Local.transform;
            if (!victimIsLocalPlayer && info.Bounces < minBouncesForBotKill)
            {
                Debug.Log($"[Killcam] saltata: kill su {victim.name} con {info.Bounces} sponde (minimo {minBouncesForBotKill})");
                return;
            }

            Debug.Log($"[Killcam] replay: vittima {victim.name}, {info.Bounces} sponde, {info.Trajectory.Count} punti");
            StartCoroutine(PlayRoutine(info));
        }

        IEnumerator PlayRoutine(DamageInfo info)
        {
            playing = true;

            // Il tasto mostrato segue il binding corrente: se l'utente rimappa, l'hint si aggiorna da solo
            skipHint = skipAction != null
                ? skipAction.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions)
                : "";

            gameplayCamera = Camera.main;
            gameplayListener = gameplayCamera != null ? gameplayCamera.GetComponent<AudioListener>() : null;
            if (gameplayCamera != null) gameplayCamera.enabled = false;
            if (gameplayListener != null) gameplayListener.enabled = false;
            killcamCamera.enabled = true;
            killcamListener.enabled = true;

            var ghost = CreateGhost();

            IReadOnlyList<TrajectoryPoint> path = info.Trajectory;
            float recordedTotal = path[path.Count - 1].Time;
            // Voli lunghi: si mostra la parte FINALE a velocità reale (la kill non si taglia mai)
            float startTime = Mathf.Max(0f, recordedTotal - maxDuration);
            float playback = recordedTotal - startTime;

            // Camera subito in posizione dietro al primo tratto mostrato (niente lerp dal nulla)
            SampleTrajectory(path, startTime, out Vector3 startPos, out Vector3 startDir);
            transform.position = startPos - startDir * cameraDistance + Vector3.up * cameraHeight;
            transform.rotation = Quaternion.LookRotation(startPos - transform.position);

            bool skipped = false;
            float t = 0f;
            while (t < playback)
            {
                if (skipAction != null && skipAction.WasPressedThisFrame())
                {
                    skipped = true;
                    break;
                }

                SampleTrajectory(path, startTime + t, out Vector3 pos, out Vector3 dir);
                ghost.transform.position = pos;

                Vector3 wantedPos = pos - dir * cameraDistance + Vector3.up * cameraHeight;
                transform.position = Vector3.Lerp(transform.position, wantedPos, 9f * Time.deltaTime);
                Quaternion wantedRot = Quaternion.LookRotation(pos + dir * 1.5f - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, wantedRot, 9f * Time.deltaTime);

                t += Time.deltaTime;
                yield return null;
            }

            // Pausa sull'impatto: la camera si assesta sul punto della kill
            if (!skipped)
            {
                Vector3 killPoint = path[path.Count - 1].Position;
                ghost.transform.position = killPoint;
                float hold = 0f;
                while (hold < killHoldDuration)
                {
                    if (skipAction != null && skipAction.WasPressedThisFrame())
                        break;
                    Quaternion wantedRot = Quaternion.LookRotation(killPoint - transform.position);
                    transform.rotation = Quaternion.Slerp(transform.rotation, wantedRot, 6f * Time.deltaTime);
                    hold += Time.deltaTime;
                    yield return null;
                }
            }

            Destroy(ghost);
            killcamCamera.enabled = false;
            killcamListener.enabled = false;
            if (gameplayCamera != null) gameplayCamera.enabled = true;
            if (gameplayListener != null) gameplayListener.enabled = true;
            playing = false;
        }

        /// <summary>Posizione e direzione lungo la traiettoria al tempo registrato richiesto.</summary>
        static void SampleTrajectory(IReadOnlyList<TrajectoryPoint> path, float time, out Vector3 position, out Vector3 direction)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                TrajectoryPoint a = path[i];
                TrajectoryPoint b = path[i + 1];
                if (time <= b.Time || i == path.Count - 2)
                {
                    float segment = Mathf.Max(1e-4f, b.Time - a.Time);
                    float k = Mathf.Clamp01((time - a.Time) / segment);
                    position = Vector3.Lerp(a.Position, b.Position, k);
                    Vector3 delta = b.Position - a.Position;
                    direction = delta.sqrMagnitude > 1e-6f ? delta.normalized : Vector3.forward;
                    return;
                }
            }
            position = path[path.Count - 1].Position;
            direction = Vector3.forward;
        }

        GameObject CreateGhost()
        {
            var ghost = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ghost.name = "KillcamGhost";
            Destroy(ghost.GetComponent<Collider>());
            ghost.transform.localScale = Vector3.one * 0.18f;

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                color = new Color(1f, 0.25f, 0.1f)
            };
            ghost.GetComponent<Renderer>().sharedMaterial = material;

            var trail = ghost.AddComponent<TrailRenderer>();
            trail.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            trail.time = 0.6f;
            trail.startWidth = 0.14f;
            trail.endWidth = 0f;
            trail.startColor = material.color;
            trail.endColor = new Color(material.color.r, material.color.g, material.color.b, 0f);
            return ghost;
        }

        void OnGUI()
        {
            if (!playing)
                return;

            // Barre cinematiche + etichetta
            float barHeight = Screen.height * 0.11f;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, barHeight), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - barHeight, Screen.width, barHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            style.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
            GUI.Label(new Rect(0, 0, Screen.width, barHeight), "KILLCAM", style);

            if (!string.IsNullOrEmpty(skipHint))
            {
                var hint = new GUIStyle(style) { fontSize = 13, alignment = TextAnchor.MiddleRight };
                hint.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
                GUI.Label(new Rect(0, Screen.height - barHeight, Screen.width - 16f, barHeight),
                    $"{skipHint.ToUpperInvariant()} per saltare", hint);
            }
        }
    }
}
