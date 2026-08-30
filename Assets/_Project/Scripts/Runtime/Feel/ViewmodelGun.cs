using UnityEngine;

namespace BankShot
{
    /// <summary>
    /// Viewmodel dell'arma sotto la camera, con rinculo a molla sullo sparo.
    /// Usa il modello assegnato (Blaster Kit di Kenney, CC0); se manca ripiega
    /// su una pistola greybox di primitive.
    /// </summary>
    [RequireComponent(typeof(ProjectileGun))]
    public class ViewmodelGun : MonoBehaviour
    {
        [SerializeField] GameObject modelPrefab;
        [SerializeField] Vector3 restPosition = new Vector3(0.28f, -0.28f, 0.45f);
        [SerializeField] Vector3 modelEulerOffset = new Vector3(0f, 90f, 0f); // gli FBX Quaternius puntano a sinistra
        [Tooltip("Lunghezza a cui normalizzare il modello (auto-fit sui bounds): gli FBX arrivano con scale arbitrarie")]
        [SerializeField] float targetLength = 0.35f;
        [SerializeField] float recoilKick = 0.09f;
        [SerializeField] float recoilAngle = 7f;
        [SerializeField] float returnSpeed = 12f;

        ProjectileGun gun;
        Transform root;
        float recoil; // 0..1, decade verso 0

        void Awake()
        {
            gun = GetComponent<ProjectileGun>();

            root = new GameObject("Viewmodel").transform;
            root.SetParent(transform, worldPositionStays: false);
            root.localPosition = restPosition;

            if (modelPrefab != null)
            {
                var model = Instantiate(modelPrefab, root);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(modelEulerOffset);
                foreach (var col in model.GetComponentsInChildren<Collider>())
                    Destroy(col);
                SetLayerRecursive(model.transform, gameObject.layer);
                FitModel(model);
            }
            else
            {
                BuildGreybox();
            }
        }

        /// <summary>Normalizza scala e centro del modello sui suoi bounds renderizzati.</summary>
        void FitModel(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            float maxDim = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxDim > 1e-4f)
                model.transform.localScale *= targetLength / maxDim;

            // Ricentra: il centro dei bounds va sull'origine del viewmodel
            bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            model.transform.localPosition -= root.InverseTransformPoint(bounds.center);
        }

        static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t)
                SetLayerRecursive(child, layer);
        }

        void BuildGreybox()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.18f, 0.18f, 0.22f));

            Part(new Vector3(0f, 0f, 0.1f), new Vector3(0.055f, 0.055f, 0.42f), material);    // canna
            Part(new Vector3(0f, -0.04f, -0.12f), new Vector3(0.08f, 0.11f, 0.16f), material); // corpo
            Part(new Vector3(0f, -0.13f, -0.16f), new Vector3(0.06f, 0.12f, 0.07f), material); // impugnatura
        }

        void Part(Vector3 localPos, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "VM_Part";
            go.layer = gameObject.layer;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root, worldPositionStays: false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = material;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void OnEnable() => gun.Fired += OnFired;
        void OnDisable() => gun.Fired -= OnFired;

        void OnFired()
        {
            recoil = 1f;
            Sfx.Play2D(Sfx.Shoot, pitch: Random.Range(0.95f, 1.05f), volume: 0.6f);
        }

        void LateUpdate()
        {
            recoil = Mathf.MoveTowards(recoil, 0f, returnSpeed * Time.deltaTime * Mathf.Max(0.25f, recoil));
            root.localPosition = restPosition + new Vector3(0f, 0f, -recoilKick * recoil);
            root.localRotation = Quaternion.Euler(-recoilAngle * recoil, 0f, 0f);
        }
    }
}
