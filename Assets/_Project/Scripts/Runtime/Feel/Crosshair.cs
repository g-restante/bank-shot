using UnityEngine;

namespace BankShot
{
    /// <summary>
    /// Crosshair IMGUI: croce a 4 bracci con gap che si apre e colore giallo
    /// quando un nostro proiettile va a segno (hitmarker). Placeholder greybox.
    /// </summary>
    public class Crosshair : MonoBehaviour
    {
        [SerializeField] float armLength = 8f;
        [SerializeField] float thickness = 2f;
        [SerializeField] float gap = 5f;
        [SerializeField] Color color = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] Color hitColor = new Color(1f, 0.85f, 0.15f, 1f);
        [SerializeField] float hitFlashDuration = 0.18f;

        static Texture2D pixel;
        float lastHitTime = -10f;

        void OnEnable() => CombatEvents.DamageDealt += OnDamageDealt;
        void OnDisable() => CombatEvents.DamageDealt -= OnDamageDealt;

        void OnDamageDealt(DamageInfo info)
        {
            if (info.Attacker != transform.root)
                return; // hitmarker solo per i colpi del giocatore locale
            lastHitTime = Time.time;
            Sfx.Play2D(Sfx.Hit, pitch: 1f + Mathf.Min(0.5f, info.Bounces * 0.08f), volume: 0.5f);
        }

        void OnGUI()
        {
            if (pixel == null)
            {
                pixel = new Texture2D(1, 1);
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
            }

            float sinceHit = Time.time - lastHitTime;
            bool flashing = sinceHit < hitFlashDuration;
            float g = gap + (flashing ? Mathf.Lerp(6f, 0f, sinceHit / hitFlashDuration) : 0f);

            GUI.color = flashing ? hitColor : color;
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            GUI.DrawTexture(new Rect(cx - g - armLength, cy - thickness * 0.5f, armLength, thickness), pixel); // sinistra
            GUI.DrawTexture(new Rect(cx + g, cy - thickness * 0.5f, armLength, thickness), pixel);             // destra
            GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - g - armLength, thickness, armLength), pixel); // sopra
            GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy + g, thickness, armLength), pixel);             // sotto
            GUI.color = Color.white;
        }
    }
}
