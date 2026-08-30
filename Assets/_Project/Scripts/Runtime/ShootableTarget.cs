using System.Collections;
using UnityEngine;

namespace BankShot
{
    /// <summary>
    /// Bersaglio che lampeggia quando colpito (via MaterialPropertyBlock, nessuna istanza di materiale).
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class ShootableTarget : MonoBehaviour
    {
        [SerializeField] Color flashColor = Color.white;
        [SerializeField] float flashDuration = 0.15f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Renderer rend;
        MaterialPropertyBlock mpb;
        Color baseColor;
        Coroutine flashRoutine;

        void Awake()
        {
            rend = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
            baseColor = rend.sharedMaterial.GetColor(BaseColorId);
        }

        public void OnHit(Vector3 point)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            float t = 0f;
            while (t < flashDuration)
            {
                t += Time.deltaTime;
                mpb.SetColor(BaseColorId, Color.Lerp(flashColor, baseColor, t / flashDuration));
                rend.SetPropertyBlock(mpb);
                yield return null;
            }
            rend.SetPropertyBlock(null);
            flashRoutine = null;
        }
    }
}
