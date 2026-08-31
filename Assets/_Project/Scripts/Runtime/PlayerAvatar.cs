using System.Collections;
using UnityEngine;

namespace BankShot
{
    /// <summary>
    /// Il giocatore locale come bersaglio: HP a schermo, vignetta rossa quando
    /// incassa, morte e respawn al punto di partenza. I bot lo trovano via Local.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerAvatar : MonoBehaviour
    {
        [SerializeField] float respawnDelay = 1.5f;

        public static PlayerAvatar Local { get; private set; }

        public Health Health { get; private set; }

        Vector3 spawnPosition;
        CharacterController controller;
        float lastDamageTime = -10f;
        bool dead;

        void Awake()
        {
            Local = this;
            Health = GetComponent<Health>();
            controller = GetComponent<CharacterController>();
            spawnPosition = transform.position;
        }

        void OnDestroy()
        {
            if (Local == this)
                Local = null;
        }

        void OnEnable()
        {
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
        }

        void OnDisable()
        {
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDied;
        }

        void OnDamaged(DamageInfo info)
        {
            lastDamageTime = Time.time;
            Sfx.Play2D(Sfx.Bounce, pitch: 0.6f, volume: 0.5f); // tonfo: incassato
        }

        void OnDied(DamageInfo info)
        {
            if (!dead)
                StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            dead = true;
            yield return new WaitForSeconds(respawnDelay);

            // Teleport pulito col CharacterController disattivato
            controller.enabled = false;
            transform.position = spawnPosition;
            controller.enabled = true;
            Health.Revive();
            dead = false;
        }

        void OnGUI()
        {
            // Vignetta rossa che sfuma dopo il danno (o fissa da morto)
            float sinceHit = Time.time - lastDamageTime;
            float alpha = dead ? 0.45f : Mathf.Clamp01(1f - sinceHit / 0.5f) * 0.35f;
            if (alpha > 0.01f)
            {
                GUI.color = new Color(0.8f, 0f, 0f, alpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerLeft,
            };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(18f, 0f, 300f, Screen.height - 14f), $"HP {Mathf.CeilToInt(Health.Current)}", style);

            if (dead)
            {
                var deathStyle = new GUIStyle(style)
                {
                    fontSize = 44,
                    alignment = TextAnchor.MiddleCenter,
                };
                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "ELIMINATO", deathStyle);
            }
        }
    }
}
