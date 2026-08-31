using System.Collections;
using UnityEngine;

namespace BankShot
{
    /// <summary>
    /// Bot stupido della sandbox (gate Fase 1): vaga per l'arena, se ti vede ti
    /// spara addosso (colpi diretti disarmati che rimbalzano sul corpo — regola
    /// del gioco). Tre colpi e cade con una capriola ragdoll, poi respawna.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(CharacterController))]
    public class SandboxBot : MonoBehaviour
    {
        [SerializeField] ProjectileConfig projectileConfig;
        [SerializeField] float moveSpeed = 4f;
        [SerializeField] float wanderRadius = 15f;
        [SerializeField] float sightRange = 28f;
        [SerializeField] float fireInterval = 2f;
        [SerializeField] float fireSpreadDegrees = 4f;
        [SerializeField] float respawnDelay = 2.5f;

        Health health;
        CharacterController controller;
        Renderer bodyRenderer;
        Vector3 wanderTarget;
        float nextWanderTime;
        float nextFireTime;
        bool dead;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            health = GetComponent<Health>();
            controller = GetComponent<CharacterController>();
            bodyRenderer = GetComponent<Renderer>();
        }

        void OnEnable()
        {
            health.Died += OnDied;
            PickWanderTarget();
        }

        void OnDisable() => health.Died -= OnDied;

        void Update()
        {
            if (dead)
                return;

            Wander();
            TryShootPlayer();
        }

        void Wander()
        {
            if (Time.time >= nextWanderTime || Arrived())
                PickWanderTarget();

            Vector3 toTarget = wanderTarget - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.04f)
            {
                Vector3 dir = toTarget.normalized;
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(dir), 240f * Time.deltaTime);
                controller.Move((dir * moveSpeed + Vector3.down * 10f) * Time.deltaTime);
            }
        }

        bool Arrived()
        {
            Vector3 d = wanderTarget - transform.position;
            d.y = 0f;
            return d.sqrMagnitude < 1f;
        }

        void PickWanderTarget()
        {
            Vector2 p = Random.insideUnitCircle * wanderRadius;
            wanderTarget = new Vector3(p.x, transform.position.y, p.y);
            nextWanderTime = Time.time + Random.Range(2.5f, 5f);
        }

        void TryShootPlayer()
        {
            var player = PlayerAvatar.Local;
            if (player == null || player.Health.IsDead || Time.time < nextFireTime)
                return;

            Vector3 eye = transform.position + Vector3.up * 0.6f;
            Vector3 targetPoint = player.transform.position + Vector3.up * 0.6f;
            Vector3 toPlayer = targetPoint - eye;
            if (toPlayer.magnitude > sightRange)
                return;

            // Linea di vista: niente muri in mezzo
            int mask = ~LayerMask.GetMask("Projectile", "Ignore Raycast");
            if (!Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, sightRange, mask)
                || hit.collider.transform.root != player.transform.root)
                return;

            nextFireTime = Time.time + fireInterval * Random.Range(0.8f, 1.3f);

            // Faccia al bersaglio e fuoco, con un po' di rosa (è un bot scarso)
            Vector3 flat = toPlayer;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(flat.normalized);

            Vector2 spread = Random.insideUnitCircle * fireSpreadDegrees;
            Vector3 dir = Quaternion.AngleAxis(spread.x, Vector3.up)
                        * Quaternion.AngleAxis(spread.y, transform.right)
                        * toPlayer.normalized;
            Vector3 muzzle = eye + dir * 0.7f;

            BounceProjectile.Spawn(projectileConfig, muzzle, dir, shooter: transform);
            Sfx.PlayAt(Sfx.Shoot, muzzle, pitch: Random.Range(0.95f, 1.05f), volume: 0.7f);
        }

        void OnDied(DamageInfo info)
        {
            if (dead)
                return;
            dead = true;
            SpawnCorpse(info);
            StartCoroutine(RespawnRoutine());
        }

        /// <summary>Capriola ragdoll: qui sì rigidbody (dal piano, Fase 1.4).</summary>
        void SpawnCorpse(DamageInfo info)
        {
            var corpse = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            corpse.name = "BotCorpse";
            corpse.transform.SetPositionAndRotation(transform.position, transform.rotation);
            corpse.GetComponent<Renderer>().sharedMaterial = bodyRenderer.sharedMaterial;

            var body = corpse.AddComponent<Rigidbody>();
            body.mass = 70f;
            Vector3 impulse = info.Direction.normalized * 420f + Vector3.up * 240f;
            body.AddForceAtPosition(impulse, info.Point, ForceMode.Impulse);
            Destroy(corpse, 4f);
        }

        IEnumerator RespawnRoutine()
        {
            bodyRenderer.enabled = false;
            controller.enabled = false;

            yield return new WaitForSeconds(respawnDelay);

            Vector2 p = Random.insideUnitCircle * wanderRadius;
            transform.position = new Vector3(p.x, 1.1f, p.y);
            controller.enabled = true;
            bodyRenderer.enabled = true;
            health.Revive();
            PickWanderTarget();
            dead = false;
        }
    }
}
