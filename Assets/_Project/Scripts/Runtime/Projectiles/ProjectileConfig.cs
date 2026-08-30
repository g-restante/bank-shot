using UnityEngine;

namespace BankShot
{
    /// <summary>
    /// Parametri del proiettile a rimbalzo — da tarare nei playtest (Fase 1).
    /// Valori iniziali dal piano: lento e visibile, 3 colpi per kill, +25% danno e +10% velocità a rimbalzo.
    /// </summary>
    [CreateAssetMenu(menuName = "BankShot/Projectile Config", fileName = "ProjectileConfig")]
    public class ProjectileConfig : ScriptableObject
    {
        [Header("Moto")]
        public float baseSpeed = 60f;          // m/s — "tracciante veloce": più realistico ma ancora leggibile
        public float radius = 0.08f;           // raggio dello spherecast e della mesh
        public float lifetime = 10f;           // secondi: la mappa resta un flipper

        [Header("Danno e potenziamento")]
        public float baseDamage = 34f;         // 3 colpi per kill (100 hp)
        [Tooltip("Guadagno di danno per ogni rimbalzo oltre il primo (0.25 = +25%)")]
        public float damageGainPerBounce = 0.25f;
        [Tooltip("Guadagno di velocità per ogni rimbalzo (0.10 = +10%)")]
        public float speedGainPerBounce = 0.10f;
        [Tooltip("I rimbalzi oltre questo numero non potenziano più (il proiettile continua a rimbalzare)")]
        public int maxPowerBounces = 6;

        [Header("Leggibilità (colore = stato, non solo: anche la scia cambia)")]
        public Color disarmedColor = new Color(0.55f, 0.55f, 0.55f);
        public Color armedColor = new Color(1f, 0.15f, 0.1f);
        public Color maxPowerColor = new Color(1f, 0.8f, 0.15f);

        /// <summary>Danno corrente dato il numero di rimbalzi di potenziamento (0 se disarmato).</summary>
        public float DamageAt(int powerBounces)
        {
            if (powerBounces <= 0)
                return 0f;
            return baseDamage * (1f + damageGainPerBounce * (powerBounces - 1));
        }
    }
}
