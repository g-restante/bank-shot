using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BankShot.EditorTools
{
    /// <summary>
    /// Costruisce la scena Sandbox della Fase 0 in modo riproducibile:
    /// pavimento, muri, bersagli e player (capsula + camera + arma hitscan).
    /// Eseguibile dal menu o in batchmode con -executeMethod.
    /// </summary>
    public static class SandboxSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        const string MaterialsDir = "Assets/_Project/Materials";

        [MenuItem("BankShot/Build Sandbox Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
                throw new System.InvalidOperationException($"InputActionAsset non trovato: {InputActionsPath}");

            Material ground = GetOrCreateMaterial("Ground", new Color(0.45f, 0.45f, 0.48f));
            Material wall = GetOrCreateMaterial("Wall", new Color(0.65f, 0.62f, 0.55f));
            Material cover = GetOrCreateMaterial("Cover", new Color(0.35f, 0.5f, 0.65f));
            Material target = GetOrCreateMaterial("Target", new Color(1f, 0.5f, 0.1f));

            BuildLight();
            BuildArena(ground, wall, cover);
            BuildTargets(target);
            BuildPlayer(inputActions);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[SandboxSceneBuilder] Scena salvata in {ScenePath}");
        }

        static void BuildLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = 1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static void BuildArena(Material ground, Material wall, Material cover)
        {
            var arena = new GameObject("Arena");

            var floor = Primitive(PrimitiveType.Plane, "Floor", arena.transform, ground,
                position: Vector3.zero, scale: new Vector3(4f, 1f, 4f)); // 40x40 m

            // Muri perimetrali
            Primitive(PrimitiveType.Cube, "Wall_N", arena.transform, wall, new Vector3(0f, 1.5f, 20f), new Vector3(40f, 3f, 0.5f));
            Primitive(PrimitiveType.Cube, "Wall_S", arena.transform, wall, new Vector3(0f, 1.5f, -20f), new Vector3(40f, 3f, 0.5f));
            Primitive(PrimitiveType.Cube, "Wall_E", arena.transform, wall, new Vector3(20f, 1.5f, 0f), new Vector3(0.5f, 3f, 40f));
            Primitive(PrimitiveType.Cube, "Wall_W", arena.transform, wall, new Vector3(-20f, 1.5f, 0f), new Vector3(0.5f, 3f, 40f));

            // Coperture sparse
            Primitive(PrimitiveType.Cube, "Cover_A", arena.transform, cover, new Vector3(-6f, 1f, 2f), new Vector3(2f, 2f, 2f));
            Primitive(PrimitiveType.Cube, "Cover_B", arena.transform, cover, new Vector3(5f, 1f, -4f), new Vector3(2f, 2f, 2f));
            Primitive(PrimitiveType.Cube, "Cover_C", arena.transform, cover, new Vector3(2f, 1f, 8f), new Vector3(3f, 2f, 1f));

            // Pannello a 45°: assaggio delle sponde della Fase 1
            var panel = Primitive(PrimitiveType.Cube, "Panel_45", arena.transform, cover, new Vector3(-10f, 1.5f, 10f), new Vector3(3f, 3f, 0.3f));
            panel.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        }

        static void BuildTargets(Material material)
        {
            var parent = new GameObject("Targets");
            var positions = new[]
            {
                new Vector3(0f, 1f, 12f),
                new Vector3(-8f, 3f, 15f),
                new Vector3(9f, 1.5f, 10f),
                new Vector3(14f, 0.75f, -2f),
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var t = Primitive(PrimitiveType.Sphere, $"Target_{i}", parent.transform, material,
                    positions[i], Vector3.one * 1.5f);
                t.AddComponent<ShootableTarget>();
            }
        }

        static void BuildPlayer(InputActionAsset inputActions)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.layer = LayerMask.NameToLayer("Player");
            player.transform.position = new Vector3(0f, 1.1f, -15f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());

            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.SetParent(player.transform, worldPositionStays: false);
            cameraGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            cameraGo.tag = "MainCamera";
            cameraGo.layer = LayerMask.NameToLayer("Player");
            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.05f;
            cameraGo.AddComponent<AudioListener>();

            var motor = player.AddComponent<PlayerMotor>();
            Assign(motor, "actions", inputActions);

            var look = player.AddComponent<PlayerLook>();
            Assign(look, "actions", inputActions);
            Assign(look, "cameraPivot", cameraGo.transform);

            var gun = cameraGo.AddComponent<HitscanGun>();
            Assign(gun, "actions", inputActions);
            Assign(gun, "aimCamera", camera);
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Material material,
            Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            return mat;
        }

        /// <summary>Assegna un campo privato [SerializeField] via SerializedObject.</summary>
        static void Assign(Component component, string field, Object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(field);
            if (prop == null)
                throw new System.InvalidOperationException($"Campo '{field}' non trovato su {component.GetType().Name}");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
