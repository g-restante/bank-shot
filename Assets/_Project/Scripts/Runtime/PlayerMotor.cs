using UnityEngine;
using UnityEngine.InputSystem;

namespace BankShot
{
    /// <summary>
    /// Movimento FPS su CharacterController: WASD, sprint, salto, gravità custom.
    /// Espone lo stato "a terra / in aria" che servirà al trick Airborne (Fase 1).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float sprintMultiplier = 1.5f;
        [SerializeField] float jumpHeight = 1.2f;
        [SerializeField] float gravity = -20f; // più forte del reale: salto più "arcade"

        CharacterController controller;
        InputAction moveAction;
        InputAction jumpAction;
        InputAction sprintAction;
        float verticalVelocity;
        float lastGroundedTime;

        public bool IsGrounded => controller.isGrounded;

        /// <summary>Secondi trascorsi dall'ultimo contatto col terreno (per il trick Airborne, soglia ≥0.3s).</summary>
        public float TimeSinceGrounded => Time.time - lastGroundedTime;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            var map = actions.FindActionMap("Player", throwIfNotFound: true);
            moveAction = map.FindAction("Move", throwIfNotFound: true);
            jumpAction = map.FindAction("Jump", throwIfNotFound: true);
            sprintAction = map.FindAction("Sprint", throwIfNotFound: true);
        }

        void OnEnable() => actions.FindActionMap("Player").Enable();
        void OnDisable() => actions.FindActionMap("Player").Disable();

        void Update()
        {
            if (controller.isGrounded)
            {
                lastGroundedTime = Time.time;
                // Piccola spinta verso il basso per restare incollati a terra e su per le rampe
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;

                if (jumpAction.WasPressedThisFrame())
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector2 input = moveAction.ReadValue<Vector2>();
            float speed = moveSpeed * (sprintAction.IsPressed() ? sprintMultiplier : 1f);
            Vector3 planar = (transform.right * input.x + transform.forward * input.y) * speed;

            controller.Move((planar + Vector3.up * verticalVelocity) * Time.deltaTime);
        }
    }
}
