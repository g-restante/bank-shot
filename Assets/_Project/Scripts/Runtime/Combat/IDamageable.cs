using System.Collections.Generic;
using UnityEngine;

namespace BankShot
{
    /// <summary>Un punto della traiettoria registrata di un proiettile (per la killcam).</summary>
    public readonly struct TrajectoryPoint
    {
        public readonly Vector3 Position;
        public readonly float Time; // tempo di volo al passaggio

        public TrajectoryPoint(Vector3 position, float time)
        {
            Position = position;
            Time = time;
        }
    }

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

        /// <summary>Radice di chi ha sparato (null se ignoto).</summary>
        public readonly Transform Attacker;

        /// <summary>Traiettoria completa del proiettile (null se il danno non viene da un proiettile).</summary>
        public readonly IReadOnlyList<TrajectoryPoint> Trajectory;

        public DamageInfo(float amount, Vector3 point, Vector3 direction, int bounces, Transform attacker,
            IReadOnlyList<TrajectoryPoint> trajectory = null)
        {
            Amount = amount;
            Point = point;
            Direction = direction;
            Bounces = bounces;
            Attacker = attacker;
            Trajectory = trajectory;
        }
    }
}
