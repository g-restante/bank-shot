using System.Collections;
using UnityEngine;

namespace BankShot
{
    /// <summary>
    /// Bersaglio della sandbox: lampeggia quando colpito da un proiettile ARMATO
    /// e logga il danno ricevuto. (Via MaterialPropertyBlock, nessuna istanza di materiale.)
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class ShootableTarget : MonoBehaviour, IDamageable
    {
        [SerializeField] Color flashColor = Color.white;
        [SerializeField] float flashDuration = 0.25f;

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

        public void TakeDamage(in DamageInfo info)
        {
            Debug.Log($"[{name}] {info.Amount:F0} danni ({info.Bounces} rimbalzi)");
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
