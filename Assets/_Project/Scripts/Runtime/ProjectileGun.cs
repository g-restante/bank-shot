using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BankShot
{
    /// <summary>
    /// La pistola base: spara il proiettile a rimbalzo dal centro del mirino.
    /// Il colpo nasce DISARMATO — l'aggancio bornArmed è pronto per il
    /// riconoscimento trickshot della Fase 1.2.
    /// </summary>
    public class ProjectileGun : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] Camera aimCamera;
        [SerializeField] ProjectileConfig config;
        [SerializeField] float fireCooldown = 0.2f;
        [SerializeField] Vector3 muzzleOffset = new Vector3(0.25f, -0.2f, 0.3f);

        InputAction attackAction;
        float nextFireTime;

        /// <summary>Scatta a ogni colpo sparato: aggancio per viewmodel, audio, HUD.</summary>
        public event Action Fired;

        void Awake()
        {
            attackAction = actions.FindActionMap("Player", throwIfNotFound: true)
                                  .FindAction("Attack", throwIfNotFound: true);
        }

        void OnEnable() => actions.FindActionMap("Player").Enable();
        void OnDisable() => actions.FindActionMap("Player").Disable();

        void Update()
        {
            if (attackAction.WasPressedThisFrame() && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireCooldown;
                Fire();
            }
        }

        void Fire()
        {
            Ray aim = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 muzzle = aimCamera.transform.position + aimCamera.transform.TransformVector(muzzleOffset);
            // Direzione dal muzzle verso il punto mirato: il tiro converge sul mirino
            Vector3 targetPoint = aim.origin + aim.direction * 100f;
            Vector3 direction = (targetPoint - muzzle).normalized;

            BounceProjectile.Spawn(config, muzzle, direction, bornArmed: false);
            Fired?.Invoke();
        }
    }
}
