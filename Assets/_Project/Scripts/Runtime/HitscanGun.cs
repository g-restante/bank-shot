using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BankShot
{
    /// <summary>
    /// Arma hitscan della Fase 0: raycast dal centro camera con tracer visivo.
    /// In Fase 1 verrà sostituita dal proiettile deterministico a rimbalzo.
    /// </summary>
    public class HitscanGun : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] Camera aimCamera;
        [SerializeField] float range = 200f;
        [SerializeField] float tracerDuration = 0.06f;
        [SerializeField] Vector3 muzzleOffset = new Vector3(0.25f, -0.2f, 0.1f);

        InputAction attackAction;
        Material tracerMaterial;
        int hitMask;

        void Awake()
        {
            attackAction = actions.FindActionMap("Player", throwIfNotFound: true)
                                  .FindAction("Attack", throwIfNotFound: true);
            tracerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                color = new Color(1f, 0.85f, 0.2f)
            };
            hitMask = ~LayerMask.GetMask("Player"); // il raycast ignora chi spara
        }

        void OnEnable() => actions.FindActionMap("Player").Enable();
        void OnDisable() => actions.FindActionMap("Player").Disable();

        void Update()
        {
            if (attackAction.WasPressedThisFrame())
                Fire();
        }

        void Fire()
        {
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 end = ray.origin + ray.direction * range;

            if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                if (hit.collider.TryGetComponent(out ShootableTarget target))
                    target.OnHit(hit.point);
            }

            Vector3 muzzle = aimCamera.transform.position + aimCamera.transform.TransformVector(muzzleOffset);
            StartCoroutine(TracerRoutine(muzzle, end));
        }

        IEnumerator TracerRoutine(Vector3 from, Vector3 to)
        {
            var go = new GameObject("Tracer");
            var line = go.AddComponent<LineRenderer>();
            line.material = tracerMaterial;
            line.startWidth = 0.02f;
            line.endWidth = 0.02f;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);

            yield return new WaitForSeconds(tracerDuration);
            Destroy(go);
        }
    }
}
