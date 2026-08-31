using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BankShot.EditorTools
{
    /// <summary>
    /// Costruisce la scena Sandbox in modo riproducibile: arena con superfici di
    /// rimbalzo (metallo/legno/gomma, colori che le telegrafano), bersagli e
    /// player con la pistola a proiettili. Eseguibile dal menu o in batchmode.
    /// </summary>
    public static class SandboxSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        const string MaterialsDir = "Assets/_Project/Materials";
        const string ConfigsDir = "Assets/_Project/Configs";

        [MenuItem("BankShot/Build Sandbox Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
                throw new System.InvalidOperationException($"InputActionAsset non trovato: {InputActionsPath}");

            var projectileConfig = GetOrCreateProjectileConfig();

            // Palette industriale (tono Doom/CS:GO), ma le superfici restano leggibili:
            // acciaio freddo = metallo, marrone scuro = legno (smorza), verde militare = gomma (accelera).
            Material ground = GetOrCreateMaterial("Ground", new Color(0.16f, 0.16f, 0.17f)); // asfalto
            Material wall = GetOrCreateMaterial("Wall", new Color(0.35f, 0.36f, 0.4f));      // acciaio
            Material wood = GetOrCreateMaterial("Wood", new Color(0.32f, 0.22f, 0.13f));     // casse scure
            Material rubber = GetOrCreateMaterial("Rubber", new Color(0.2f, 0.38f, 0.2f));   // gomma militare
            Material target = GetOrCreateMaterial("Target", new Color(0.8f, 0.25f, 0.1f));   // ruggine/arancio
            Material bot = GetOrCreateMaterial("Bot", new Color(0.55f, 0.12f, 0.1f));        // rosso scuro

            BuildLight();
            BuildArena(ground, wall, wood, rubber);
            BuildTargets(target);
            BuildPlayer(inputActions, projectileConfig);
            BuildBots(bot, projectileConfig);

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

        static void BuildArena(Material ground, Material wall, Material wood, Material rubber)
        {
            var arena = new GameObject("Arena");

            Surface(Primitive(PrimitiveType.Plane, "Floor", arena.transform, ground,
                Vector3.zero, new Vector3(4f, 1f, 4f)), SurfaceType.Metal); // 40x40 m

            // Muri perimetrali: metallo, rimbalzo perfetto
            Surface(Primitive(PrimitiveType.Cube, "Wall_N", arena.transform, wall, new Vector3(0f, 1.5f, 20f), new Vector3(40f, 3f, 0.5f)), SurfaceType.Metal);
            Surface(Primitive(PrimitiveType.Cube, "Wall_S", arena.transform, wall, new Vector3(0f, 1.5f, -20f), new Vector3(40f, 3f, 0.5f)), SurfaceType.Metal);
            Surface(Primitive(PrimitiveType.Cube, "Wall_E", arena.transform, wall, new Vector3(20f, 1.5f, 0f), new Vector3(0.5f, 3f, 40f)), SurfaceType.Metal);
            Surface(Primitive(PrimitiveType.Cube, "Wall_W", arena.transform, wall, new Vector3(-20f, 1.5f, 0f), new Vector3(0.5f, 3f, 40f)), SurfaceType.Metal);

            // Coperture in legno: smorzano il rimbalzo
            Surface(Primitive(PrimitiveType.Cube, "Cover_A", arena.transform, wood, new Vector3(-6f, 1f, 2f), new Vector3(2f, 2f, 2f)), SurfaceType.Wood);
            Surface(Primitive(PrimitiveType.Cube, "Cover_B", arena.transform, wood, new Vector3(5f, 1f, -4f), new Vector3(2f, 2f, 2f)), SurfaceType.Wood);
            Surface(Primitive(PrimitiveType.Cube, "Cover_C", arena.transform, wood, new Vector3(2f, 1f, 8f), new Vector3(3f, 2f, 1f)), SurfaceType.Wood);

            // Pannelli a 45° in gomma: accelerano — le sponde "buone"
            var panelA = Primitive(PrimitiveType.Cube, "Panel_45_A", arena.transform, rubber, new Vector3(-10f, 1.5f, 10f), new Vector3(3f, 3f, 0.3f));
            panelA.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            Surface(panelA, SurfaceType.Rubber);

            var panelB = Primitive(PrimitiveType.Cube, "Panel_45_B", arena.transform, rubber, new Vector3(10f, 1.5f, 4f), new Vector3(3f, 3f, 0.3f));
            panelB.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
            Surface(panelB, SurfaceType.Rubber);

            // Angolo stretto per rimbalzi a catena
            var corner = Primitive(PrimitiveType.Cube, "CornerPanel", arena.transform, rubber, new Vector3(-14f, 1.5f, -12f), new Vector3(4f, 3f, 0.3f));
            corner.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            Surface(corner, SurfaceType.Rubber);
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
                new Vector3(-12f, 1f, 6f),   // dietro copertura: si colpisce solo di sponda
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var t = Primitive(PrimitiveType.Sphere, $"Target_{i}", parent.transform, material,
                    positions[i], Vector3.one * 1.5f);
                t.AddComponent<ShootableTarget>();
            }
        }

        static void BuildPlayer(InputActionAsset inputActions, ProjectileConfig projectileConfig)
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

            var trickshot = player.AddComponent<TrickshotDetector>();
            Assign(trickshot, "motor", motor);
            Assign(trickshot, "yawSource", player.transform);

            var gun = cameraGo.AddComponent<ProjectileGun>();
            Assign(gun, "actions", inputActions);
            Assign(gun, "aimCamera", camera);
            Assign(gun, "config", projectileConfig);
            Assign(gun, "trickshot", trickshot);

            var parry = cameraGo.AddComponent<MeleeParry>();
            Assign(parry, "actions", inputActions);
            Assign(parry, "aimCamera", camera);

            var viewmodel = cameraGo.AddComponent<ViewmodelGun>(); // viewmodel + suono sparo
            var pistol = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Weapons/Pistol.fbx");
            if (pistol != null)
                Assign(viewmodel, "modelPrefab", pistol);

            cameraGo.AddComponent<Crosshair>();     // mirino + hitmarker
            cameraGo.AddComponent<TrickBadgeHud>(); // badge "AIRBORNE!" ecc.

            player.AddComponent<Health>();
            player.AddComponent<PlayerAvatar>();    // HP, vignetta danno, respawn
        }

        static void BuildBots(Material material, ProjectileConfig projectileConfig)
        {
            var parent = new GameObject("Bots");
            var positions = new[]
            {
                new Vector3(-10f, 1.1f, 14f),
                new Vector3(12f, 1.1f, 8f),
                new Vector3(0f, 1.1f, 16f),
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var bot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                bot.name = $"Bot_{i}";
                bot.transform.SetParent(parent.transform, worldPositionStays: false);
                bot.transform.position = positions[i];
                bot.GetComponent<Renderer>().sharedMaterial = material;
                Object.DestroyImmediate(bot.GetComponent<CapsuleCollider>());

                var controller = bot.AddComponent<CharacterController>();
                controller.height = 2f;
                controller.radius = 0.4f;

                bot.AddComponent<Health>();
                var ai = bot.AddComponent<SandboxBot>();
                Assign(ai, "projectileConfig", projectileConfig);
            }
        }

        static ProjectileConfig GetOrCreateProjectileConfig()
        {
            string path = $"{ConfigsDir}/StandardProjectile.asset";
            var config = AssetDatabase.LoadAssetAtPath<ProjectileConfig>(path);
            if (config == null)
            {
                if (!AssetDatabase.IsValidFolder(ConfigsDir))
                    AssetDatabase.CreateFolder("Assets/_Project", "Configs");
                config = ScriptableObject.CreateInstance<ProjectileConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            return config;
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

        static void Surface(GameObject go, SurfaceType type)
        {
            var surface = go.AddComponent<BounceSurface>();
            var so = new SerializedObject(surface);
            so.FindProperty("type").enumValueIndex = (int)type;
            so.ApplyModifiedPropertiesWithoutUndo();
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
