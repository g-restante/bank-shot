using System;
using UnityEngine;

namespace BankShot
{
    public enum ProjectileState
    {
        Disarmed, // grigio, 0 danni
        Armed,    // rosso, dopo il 1° rimbalzo (o nato da trick)
    }

    /// <summary>
    /// Il cuore del gioco: proiettile SENZA rigidbody, traiettoria custom con
    /// spherecast per-step in FixedUpdate + Vector3.Reflect sui rimbalzi.
    /// Deterministico a parità di (posizione, direzione, tick): è la proprietà
    /// su cui si appoggerà il netcode in Fase 2 — tenere la simulazione qui,
    /// senza dipendenze da Update/rendering.
    ///
    /// Regole (dal piano):
    /// - nasce DISARMATO: il colpo diretto fa 0 danni, rimbalza sul corpo e SI ARMA
    /// - si ARMA al primo rimbalzo (o se nato da trickshot)
    /// - ogni rimbalzo (fino al cap) potenzia: +danno, +velocità, scia più intensa
    /// - superfici: metallo = perfetto, legno = smorzato, gomma = accelerato
    /// </summary>
    public class BounceProjectile : MonoBehaviour
    {
        const int MaxImpactsPerStep = 4;   // angoli stretti: più impatti nello stesso step
        const float SkinOffset = 0.001f;   // distacco dalla superficie dopo il rimbalzo

        ProjectileConfig config;
        Vector3 direction;
        float speed;
        float lifeRemaining;
        int bounces;       // rimbalzi totali (statistiche, killcam)
        int powerBounces;  // rimbalzi che contano per il potenziamento (cap)
        ProjectileState state;
        int hitMask;
        bool bornFromTrick;
        float trickPenalty = 1f;
        float bounceEnergy;

        MeshRenderer meshRenderer;
        TrailRenderer trail;
        MaterialPropertyBlock mpb;
        AudioSource whistleSource;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static Material bodyMaterial;
        static Material trailMaterial;

        /// <summary>Scatta a ogni rimbalzo: aggancio per audio (pitch crescente) e FX di Fase 1.4.</summary>
        public event Action<BounceProjectile, RaycastHit> Bounced;

        public ProjectileState State => state;
        public int Bounces => bounces;
        public float Speed => speed;
        public Vector3 Direction => direction;
        public float Damage
        {
            get
            {
                if (state == ProjectileState.Disarmed)
                    return 0f;
                float damage = bornFromTrick && bounces == 0
                    ? config.baseDamage * config.trickDamageFactor              // trick puro: veloce ma debole
                    : config.DamageAt(powerBounces) * (bornFromTrick ? config.trickBounceMultiplier : 1f); // trick+sponde: leggendario
                return damage * trickPenalty;
            }
        }

        public static BounceProjectile Spawn(ProjectileConfig config, Vector3 position, Vector3 direction, TrickShotInfo trick = default)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            go.layer = LayerMask.NameToLayer("Projectile");
            // Niente collider: la simulazione è tutta spherecast. (Il parry della
            // Fase 1.3 userà una query dedicata, non la fisica dei collider.)
            Destroy(go.GetComponent<SphereCollider>());
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (config.radius * 2f);

            var projectile = go.AddComponent<BounceProjectile>();
            projectile.Init(config, direction, trick);
            return projectile;
        }

        void Init(ProjectileConfig config, Vector3 direction, TrickShotInfo trick)
        {
            this.config = config;
            this.direction = direction.normalized;
            speed = config.baseSpeed;
            lifeRemaining = config.lifetime;
            bornFromTrick = trick.BornArmed;
            trickPenalty = trick.BornArmed ? trick.DamagePenalty : 1f;
            bounceEnergy = config.bounceEnergy;
            state = trick.BornArmed ? ProjectileState.Armed : ProjectileState.Disarmed;
            if (trick.BornArmed)
                powerBounces = 1; // il trick vale come primo rimbalzo per il potenziamento

            hitMask = ~LayerMask.GetMask("Projectile", "Player", "Ignore Raycast");

            SetupVisuals();
            UpdateVisuals();
        }

        void FixedUpdate()
        {
            lifeRemaining -= Time.fixedDeltaTime;
            if (lifeRemaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float distance = speed * Time.fixedDeltaTime;
            for (int i = 0; i < MaxImpactsPerStep && distance > 1e-4f; i++)
            {
                if (Physics.SphereCast(transform.position, config.radius, direction, out RaycastHit hit,
                        distance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    transform.position += direction * hit.distance;
                    distance -= hit.distance;
                    if (!HandleImpact(hit))
                        return; // il proiettile è morto sull'impatto
                }
                else
                {
                    transform.position += direction * distance;
                    break;
                }
            }
        }

        /// <summary>Gestisce un impatto. Ritorna false se il proiettile si distrugge.</summary>
        bool HandleImpact(RaycastHit hit)
        {
            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                if (state == ProjectileState.Armed)
                {
                    var info = new DamageInfo(Damage, hit.point, direction, bounces);
                    damageable.TakeDamage(info);
                    CombatEvents.RaiseDamageDealt(info);
                    Destroy(gameObject);
                    return false;
                }
                // Punizione comica: colpo diretto disarmato = 0 danni,
                // il proiettile rimbalza sul corpo e da lì in poi è armato.
                return TryBounce(hit, surfaceMultiplier: 1f, energyCost: BounceSurface.DefaultEnergyCost);
            }

            float multiplier = 1f;
            float cost = BounceSurface.DefaultEnergyCost;
            if (hit.collider.TryGetComponent(out BounceSurface surface))
            {
                multiplier = surface.SpeedMultiplier;
                cost = surface.EnergyCost;
            }
            return TryBounce(hit, multiplier, cost);
        }

        /// <summary>Rimbalza se c'è energia; altrimenti il colpo muore sull'impatto. Ritorna false se distrutto.</summary>
        bool TryBounce(RaycastHit hit, float surfaceMultiplier, float energyCost)
        {
            if (bounceEnergy < energyCost)
            {
                // Tonfo sordo: la superficie ha assorbito il colpo
                Sfx.PlayAt(Sfx.Bounce, hit.point, pitch: 0.5f, volume: 0.4f);
                Destroy(gameObject);
                return false;
            }
            bounceEnergy -= energyCost;
            Bounce(hit, surfaceMultiplier);
            return true;
        }

        void Bounce(RaycastHit hit, float surfaceMultiplier)
        {
            direction = Vector3.Reflect(direction, hit.normal).normalized;
            transform.position += hit.normal * SkinOffset;

            bounces++;
            if (powerBounces < config.maxPowerBounces)
            {
                powerBounces++;
                speed *= 1f + config.speedGainPerBounce;
            }
            speed *= surfaceMultiplier;
            state = ProjectileState.Armed;

            UpdateVisuals();
            // Ping metallico che sale di tono a ogni rimbalzo: si sente la carica
            Sfx.PlayAt(Sfx.Bounce, hit.point, pitch: 1f + 0.12f * powerBounces, volume: 0.5f);
            Bounced?.Invoke(this, hit);
        }

        // ---- Visuale: il colore È l'informazione (grigio→rosso→giallo col potenziamento) ----

        void SetupVisuals()
        {
            if (bodyMaterial == null)
                bodyMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (trailMaterial == null)
                trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = bodyMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mpb = new MaterialPropertyBlock();

            trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMaterial;
            trail.time = 0.35f;
            trail.minVertexDistance = 0.05f;
            trail.startWidth = config.radius * 1.6f;
            trail.endWidth = 0f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Il fischio del proiettile: sommesso da disarmato, "urla" col potenziamento
            whistleSource = gameObject.AddComponent<AudioSource>();
            whistleSource.clip = Sfx.WhistleLoop;
            whistleSource.loop = true;
            whistleSource.spatialBlend = 1f;
            whistleSource.dopplerLevel = 1f;
            whistleSource.rolloffMode = AudioRolloffMode.Linear;
            whistleSource.maxDistance = 50f;
            whistleSource.Play();
        }

        void UpdateVisuals()
        {
            Color color = state == ProjectileState.Disarmed
                ? config.disarmedColor
                : Color.Lerp(config.armedColor, config.maxPowerColor,
                    config.maxPowerBounces <= 1 ? 1f : (powerBounces - 1f) / (config.maxPowerBounces - 1f));

            mpb.SetColor(BaseColorId, color);
            meshRenderer.SetPropertyBlock(mpb);

            // Scia più lunga e larga a ogni potenziamento: si legge da lontano
            float power = Mathf.Clamp01((float)powerBounces / config.maxPowerBounces);
            trail.time = 0.35f + 0.45f * power;
            trail.startWidth = config.radius * (1.6f + 2.4f * power);
            trail.startColor = color;
            Color end = color;
            end.a = 0f;
            trail.endColor = end;

            // Sommesso: deve leggersi, non coprire gli spari (tono realistico)
            whistleSource.volume = state == ProjectileState.Disarmed ? 0.03f : 0.06f + 0.14f * power;
            whistleSource.pitch = 0.85f + 0.6f * power;
        }
    }
}
