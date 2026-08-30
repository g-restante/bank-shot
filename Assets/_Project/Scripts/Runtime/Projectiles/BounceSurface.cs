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
        [SerializeField] SurfaceType type = SurfaceType.Metal;

        public SurfaceType Type => type;

        public float SpeedMultiplier => type switch
        {
            SurfaceType.Wood => 0.8f,
            SurfaceType.Rubber => 1.25f,
            _ => 1f,
        };
    }
}
