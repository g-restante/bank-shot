using System;

namespace BankShot
{
    /// <summary>Eventi di combattimento globali — agganci per HUD e feedback.</summary>
    public static class CombatEvents
    {
        /// <summary>Un proiettile armato ha inflitto danno a qualcuno.</summary>
        public static event Action<DamageInfo> DamageDealt;

        /// <summary>Il giocatore locale ha sparato un trickshot riconosciuto (per i badge HUD).</summary>
        public static event Action<Tricks> TrickShot;

        /// <summary>Il giocatore locale ha ribattuto un proiettile.</summary>
        public static event Action Parried;

        public static void RaiseDamageDealt(in DamageInfo info) => DamageDealt?.Invoke(info);

        public static void RaiseTrickShot(Tricks tricks) => TrickShot?.Invoke(tricks);

        public static void RaiseParried() => Parried?.Invoke();
    }
}
