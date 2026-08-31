using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BankShot
{
    /// <summary>
    /// La ribattuta (Fase 1.3, tasto destro): una "sventola" melee che rilancia i
    /// proiettili in volo verso il mirino. Finestra generosa (~0.3s) e cono largo:
    /// deve riuscire spesso — è la meccanica più divertente del gioco.
    /// </summary>
    public class MeleeParry : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] Camera aimCamera;
        [SerializeField] float range = 3f;
        [Tooltip("Semiangolo del cono davanti alla camera in cui il parry aggancia")]
        [SerializeField] float coneHalfAngle = 35f;
        [SerializeField] float windowDuration = 0.15f;
        [SerializeField] float cooldown = 0.6f;

        InputAction parryAction;
        float windowEndTime = -1f;
        float nextAllowedTime;
        readonly HashSet<BounceProjectile> parriedThisWindow = new HashSet<BounceProjectile>();

        void Awake()
        {
            parryAction = actions.FindActionMap("Player", throwIfNotFound: true)
                                 .FindAction("Parry", throwIfNotFound: true);
        }

        void OnEnable() => actions.FindActionMap("Player").Enable();
        void OnDisable() => actions.FindActionMap("Player").Disable();

        void Update()
        {
            if (parryAction.WasPressedThisFrame() && Time.time >= nextAllowedTime)
            {
                nextAllowedTime = Time.time + cooldown;
                windowEndTime = Time.time + windowDuration;
                parriedThisWindow.Clear();
                Sfx.Play2D(Sfx.Swing, pitch: Random.Range(0.9f, 1.1f), volume: 0.5f);
            }

            if (Time.time <= windowEndTime)
                TryParryInVolume();
        }

        void TryParryInVolume()
        {
            Transform cam = aimCamera.transform;
            Ray aim = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 aimPoint = aim.origin + aim.direction * 100f;

            // Iterazione su copia: Parry può modificare la lista Active
            for (int i = BounceProjectile.Active.Count - 1; i >= 0; i--)
            {
                var projectile = BounceProjectile.Active[i];
                if (parriedThisWindow.Contains(projectile))
                    continue;

                Vector3 toProjectile = projectile.transform.position - cam.position;
                if (toProjectile.magnitude > range)
                    continue;
                if (Vector3.Angle(cam.forward, toProjectile) > coneHalfAngle)
                    continue;

                projectile.Parry((aimPoint - projectile.transform.position).normalized);
                parriedThisWindow.Add(projectile);
                CombatEvents.RaiseParried();
            }
        }
    }
}
