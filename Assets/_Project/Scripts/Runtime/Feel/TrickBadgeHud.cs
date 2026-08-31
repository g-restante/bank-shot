using System.Collections.Generic;
using UnityEngine;

namespace BankShot
{
    /// <summary>
    /// Badge a schermo quando parte un trickshot ("AIRBORNE!", "FLICK!"...).
    /// Feedback immediato e leggibilità nei clip (dal piano, Fase 1.2).
    /// </summary>
    public class TrickBadgeHud : MonoBehaviour
    {
        [SerializeField] float duration = 0.9f;
        [SerializeField] int fontSize = 34;
        [SerializeField] Color color = new Color(1f, 0.85f, 0.15f);

        string text;
        float shownAt = -10f;
        GUIStyle style;

        void OnEnable() => CombatEvents.TrickShot += OnTrickShot;
        void OnDisable() => CombatEvents.TrickShot -= OnTrickShot;

        void OnTrickShot(Tricks tricks)
        {
            var parts = new List<string>(3);
            if ((tricks & Tricks.Airborne) != 0) parts.Add("AIRBORNE!");
            if ((tricks & Tricks.Flick) != 0) parts.Add("FLICK!");
            if ((tricks & Tricks.BehindTheBack) != 0) parts.Add("BEHIND THE BACK!");
            if ((tricks & Tricks.NoScope) != 0) parts.Add("NO-SCOPE!");
            text = string.Join(" + ", parts);
            shownAt = Time.time;
        }

        void OnGUI()
        {
            float elapsed = Time.time - shownAt;
            if (elapsed > duration || string.IsNullOrEmpty(text))
                return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
            }
            style.fontSize = fontSize;

            float t = elapsed / duration;
            float alpha = 1f - t * t;              // resta pieno, poi svanisce
            float rise = 24f * t;                  // sale leggermente

            var rect = new Rect(0f, Screen.height * 0.32f - rise, Screen.width, 60f);
            // Ombra per staccare dal fondo chiaro/scuro
            style.normal.textColor = new Color(0f, 0f, 0f, 0.7f * alpha);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
            style.normal.textColor = new Color(color.r, color.g, color.b, alpha);
            GUI.Label(rect, text, style);
        }
    }
}
