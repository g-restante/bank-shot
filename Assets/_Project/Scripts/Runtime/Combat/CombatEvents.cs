using System;

namespace BankShot
{
    /// <summary>Eventi di combattimento globali — agganci per HUD e feedback.</summary>
    public static class CombatEvents
    {
        /// <summary>Un proiettile armato ha inflitto danno a qualcuno.</summary>
        public static event Action<DamageInfo> DamageDealt;

        public static void RaiseDamageDealt(in DamageInfo info) => DamageDealt?.Invoke(info);
    }
}
