using UnityEngine;
using UnityEngine.InputSystem;

namespace BankShot
{
    /// <summary>
    /// Mouse look: yaw sul corpo, pitch sul pivot della camera. Blocca il cursore quando attivo.
    /// </summary>
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] Transform cameraPivot;
        [SerializeField] float sensitivity = 0.12f; // gradi per unità di delta mouse
        [SerializeField] float pitchLimit = 89f;

        InputAction lookAction;
        float pitch;

        void Awake()
        {
            lookAction = actions.FindActionMap("Player", throwIfNotFound: true)
                                .FindAction("Look", throwIfNotFound: true);
        }

        void OnEnable()
        {
            actions.FindActionMap("Player").Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            // Il click riprende il controllo se il cursore è stato liberato (Esc)
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                return;
            }

            Vector2 delta = lookAction.ReadValue<Vector2>() * sensitivity;
            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
