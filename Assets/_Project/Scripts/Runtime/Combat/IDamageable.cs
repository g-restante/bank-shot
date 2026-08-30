using UnityEngine;

namespace BankShot
{
    /// <summary>Chi può ricevere danno da un proiettile armato.</summary>
    public interface IDamageable
    {
        void TakeDamage(in DamageInfo info);
    }

    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly int Bounces;

        public DamageInfo(float amount, Vector3 point, Vector3 direction, int bounces)
        {
            Amount = amount;
            Point = point;
            Direction = direction;
            Bounces = bounces;
        }
    }
}
