using UnityEngine;
using UnityEngine.InputSystem;

namespace BankShot
{
    /// <summary>
    /// Uscita pulita nella build standalone: Esc libera il mouse,
    /// doppio Esc entro 1.2s chiude il gioco.
    /// </summary>
    public class QuitHotkey : MonoBehaviour
    {
        float lastEscTime = -10f;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;

            if (Time.unscaledTime - lastEscTime < 1.2f)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            lastEscTime = Time.unscaledTime;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnGUI()
        {
            if (Time.unscaledTime - lastEscTime > 1.2f)
                return;
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperCenter,
            };
            style.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(0, 12f, Screen.width, 30f), "Premi ancora ESC per uscire — click per tornare al gioco", style);
        }
    }
}
