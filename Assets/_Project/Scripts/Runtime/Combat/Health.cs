using System;
using UnityEngine;

namespace BankShot
{
    /// <summary>Punti vita generici: player, bot, e in futuro i giocatori remoti.</summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        /// <summary>Danno subito (già applicato).</summary>
        public event Action<DamageInfo> Damaged;

        /// <summary>Vita arrivata a zero (il DamageInfo è il colpo letale).</summary>
        public event Action<DamageInfo> Died;

        void Awake() => Current = maxHealth;

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead)
                return;

            Current = Mathf.Max(0f, Current - info.Amount);
            Damaged?.Invoke(info);
            if (IsDead)
            {
                Died?.Invoke(info);
                CombatEvents.RaiseKill(info, transform); // il transform del morto, non il root (i bot stanno sotto un parent)
            }
        }

        public void Revive()
        {
            Current = maxHealth;
        }
    }
}
