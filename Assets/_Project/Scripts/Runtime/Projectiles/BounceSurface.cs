using UnityEngine;

namespace BankShot
{
    public enum SurfaceType
    {
        Metal,  // rimbalzo perfetto
        Wood,   // smorzato: -20% velocità
        Rubber, // accelerato: +25% velocità
    }

    /// <summary>
    /// Materiale di rimbalzo di una superficie. Le superfici senza componente
    /// si comportano come metallo (rimbalzo perfetto).
    /// </summary>
    public class BounceSurface : MonoBehaviour
    {
        /// <summary>Costo per le superfici senza componente (si comportano come metallo).</summary>
        public const float DefaultEnergyCost = 0.15f;

        [SerializeField] SurfaceType type = SurfaceType.Metal;

        public SurfaceType Type => type;

        public float SpeedMultiplier => type switch
        {
            SurfaceType.Wood => 0.8f,
            SurfaceType.Rubber => 1.25f,
            _ => 1f,
        };

        /// <summary>
        /// Quanta energia di rimbalzo consuma questa superficie: il legno assorbe
        /// (2 sponde e muore), il metallo restituisce (~6), la gomma quasi tutto (~12).
        /// </summary>
        public float EnergyCost => type switch
        {
            SurfaceType.Wood => 0.45f,
            SurfaceType.Rubber => 0.08f,
            _ => DefaultEnergyCost,
        };
    }
}
