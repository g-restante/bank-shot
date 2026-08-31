using System;

namespace BankShot
{
    /// <summary>Trickshot riconosciuti allo sparo (combinabili).</summary>
    [Flags]
    public enum Tricks
    {
        None = 0,
        Airborne = 1 << 0,      // in aria da >= 0.3s
        Flick = 1 << 1,         // rotazione camera >= 150° in 0.5s
        BehindTheBack = 1 << 2, // tiro opposto alla direzione di corsa
        NoScope = 1 << 3,       // riservato alle armi con mirino (Fase 3)
    }

    /// <summary>Esito della valutazione trick al momento dello sparo.</summary>
    public readonly struct TrickShotInfo
    {
        public readonly Tricks Tricks;

        /// <summary>1 = danno pieno; 0.5 = penalità anti-spam (stesso trick ripetuto).</summary>
        public readonly float DamagePenalty;

        public TrickShotInfo(Tricks tricks, float damagePenalty)
        {
            Tricks = tricks;
            DamagePenalty = damagePenalty;
        }

        /// <summary>Il colpo nasce già armato?</summary>
        public bool BornArmed => Tricks != Tricks.None;
    }
}
