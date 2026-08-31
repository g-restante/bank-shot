using System;
using UnityEngine;

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

        /// <summary>Qualcuno è morto: colpo letale + radice della vittima (per killcam e scoreboard).</summary>
        public static event Action<DamageInfo, Transform> Kill;

        public static void RaiseDamageDealt(in DamageInfo info) => DamageDealt?.Invoke(info);

        public static void RaiseTrickShot(Tricks tricks) => TrickShot?.Invoke(tricks);

        public static void RaiseParried() => Parried?.Invoke();

        public static void RaiseKill(in DamageInfo info, Transform victim) => Kill?.Invoke(info, victim);
    }
}
