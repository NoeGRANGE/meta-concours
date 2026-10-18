using System;
using System.IO;
using System.Threading.Tasks;
using Deskmates.Core;
using Deskmates.Creatures;
using Deskmates.Data;
using Deskmates.Data.Demo;
using Deskmates.Data.Stress;
using Deskmates.Sandbox;
using Meta.XR.MRUtilityKit;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Deskmates.Editor
{
    /// <summary>
    /// Project configuration and generation of the creature prefabs, their configs and the scenes.
    /// Scenes and configs are only created when missing; creature prefabs are created when missing and
    /// then have their components and references brought up to date.
    /// Headless: Unity -batchmode -projectPath . -executeMethod Deskmates.Editor.ProjectSetup.RunBatch
    /// </summary>
    public static class ProjectSetup
    {
        const string AndroidPackageName = "com.deskmates.app";
        const string ModelsFolder = "Assets/_Project/Art/Models";
        const string CreaturePrefabsFolder = "Assets/_Project/Prefabs/Creatures";
        const string CreatureConfigsFolder = "Assets/_Project/Settings/Creatures";
        const string StressCalculatorsFolder = "Assets/_Project/Settings/Stress";
        const string DemoScriptPath = "Assets/_Project/Settings/Data/DemoScript_Default.asset";
        const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        const string SandboxScenePath = "Assets/_Project/Scenes/Sandbox_Creature.unity";
        const string ParticleMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";
        const string SighPuffName = "SighPuff";
        const string CameraRigPrefabPath = "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab";
        const string MrukPrefabPath = "Packages/com.meta.xr.mrutilitykit/Core/Tools/MRUK.prefab";
        const string MailCreaturePrefab = "Creature_Mail";

        // Model file, prefab name (PRD 9), colour at rest (PRD 4.2), data source and stress formula (PRD 4.4).
        static readonly (string model, string prefab, Color32 restColor, string sourceId, Type stress)[] Creatures =
        {
            ("Facteur", MailCreaturePrefab, new Color32(0xEF, 0xE6, 0xD8, 0xFF), SourceIds.Mail, typeof(MailStress)),
            ("Reunions", "Creature_Calendar", new Color32(0xDC, 0xE3, 0xE6, 0xFF), SourceIds.Calendar, typeof(CalendarStress)),
            ("Taches", "Creature_Tasks", new Color32(0xE6, 0xDC, 0xEB, 0xFF), SourceIds.Tasks, typeof(TasksStress)),
            ("Messages", "Creature_Messages", new Color32(0xD7, 0xE3, 0xCF, 0xFF), SourceIds.Messages, typeof(MessagesStress)),
        };

        [MenuItem("Deskmates/Setup/1. Configure Project")]
        static async void ConfigureProjectMenu()
        {
            try
            {
                await ConfigureProjectAsync();
                Debug.Log("[Deskmates] Project configured.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MenuItem("Deskmates/Setup/2. Create or Update Creature Prefabs")]
        public static void CreateCreaturePrefabs()
        {
            Directory.CreateDirectory(CreaturePrefabsFolder);
            Directory.CreateDirectory(CreatureConfigsFolder);
            Directory.CreateDirectory(StressCalculatorsFolder);
            AssetDatabase.Refresh();
            foreach ((string model, string prefab, Color32 restColor, string sourceId, Type stress) in Creatures)
            {
                CreatureConfig config = EnsureCreatureConfig(prefab, restColor);
                StressCalculator calculator = EnsureStressCalculator(prefab, stress);
                CreateCreaturePrefab(model, prefab);
                UpdateCreaturePrefab(prefab, config, sourceId, calculator);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Deskmates/Setup/3. Create Main Scene")]
        public static void CreateMainScene()
        {
            if (File.Exists(MainScenePath))
            {
                Debug.Log($"[Deskmates] {MainScenePath} already exists, left untouched.");
                return;
            }

            CreateCreaturePrefabs();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Light light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject rig = InstantiatePackagePrefab(CameraRigPrefabPath);
            OVRManager manager = rig.GetComponent<OVRManager>();
            manager.isInsightPassthroughEnabled = true;

            // Passthrough is composited behind the app, so the eye buffer must clear to transparent.
            Transform centerEye = rig.transform.Find("TrackingSpace/CenterEyeAnchor");
            Camera camera = centerEye.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);

            // OVRPassthroughLayer always renders as the background underlay.
            new GameObject("Passthrough").AddComponent<OVRPassthroughLayer>();

            MRUK mruk = InstantiatePackagePrefab(MrukPrefabPath).GetComponent<MRUK>();
            mruk.SceneSettings.DataSource = MRUK.SceneDataSource.Device;
            PrefabUtility.RecordPrefabInstancePropertyModifications(mruk);

            AppBootstrap app = new GameObject("App").AddComponent<AppBootstrap>();
            Creature mailCreature = AssetDatabase.LoadAssetAtPath<Creature>(PrefabPath(MailCreaturePrefab));
            SetSerialized(app, "head", property => property.objectReferenceValue = centerEye);
            SetSerialized(app, "mailCreaturePrefab", property => property.objectReferenceValue = mailCreature);

            Directory.CreateDirectory(Path.GetDirectoryName(MainScenePath));
            AssetDatabase.Refresh();
            EditorSceneManager.SaveScene(scene, MainScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) };
            Debug.Log($"[Deskmates] Created {MainScenePath}.");
        }

        [MenuItem("Deskmates/Setup/4. Create Sandbox Scene")]
        public static void CreateSandboxScene()
        {
            if (File.Exists(SandboxScenePath))
            {
                Debug.Log($"[Deskmates] {SandboxScenePath} already exists, left untouched.");
                return;
            }

            CreateCreaturePrefabs();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Light light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // A desk top at seated height, the four creatures on it, and a camera where a seated user's eyes would be.
            const float deskHeight = 0.75f;
            GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Desk";
            desk.transform.localScale = new Vector3(1.2f, 0.03f, 0.7f);
            desk.transform.position = new Vector3(0f, deskHeight - 0.015f, 0.15f);

            Creature[] creatures = new Creature[Creatures.Length];
            for (int i = 0; i < Creatures.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(Creatures[i].prefab));
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetPositionAndRotation(
                    new Vector3((i - 1.5f) * 0.13f, deskHeight, 0f), Quaternion.Euler(0f, 180f, 0f));
                creatures[i] = instance.GetComponent<Creature>();
            }

            Camera camera = new GameObject("Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.01f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.24f, 0.24f, 0.26f);
            camera.transform.position = new Vector3(0f, 1.05f, -0.55f);
            camera.transform.LookAt(new Vector3(0f, deskHeight + 0.04f, 0f));

            StressSandbox sandbox = new GameObject("Stress Sandbox").AddComponent<StressSandbox>();
            SetSerialized(sandbox, "creatures", property =>
            {
                property.arraySize = creatures.Length;
                for (int i = 0; i < creatures.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = creatures[i];
                }
            });

            EditorSceneManager.SaveScene(scene, SandboxScenePath);
            Debug.Log($"[Deskmates] Created {SandboxScenePath}.");
        }

        /// <summary>Gives Main a playing DataHub, and the sandbox one its Use Demo toggle switches on.</summary>
        [MenuItem("Deskmates/Setup/5. Add Demo Data to Scenes")]
        public static void AddDemoDataToScenes()
        {
            EnsureDemoScript();
            EditScene(MainScenePath, () => EnsureDataHub(true));
            EditScene(SandboxScenePath, () =>
            {
                DataHub hub = EnsureDataHub(false);
                StressSandbox sandbox = Object.FindAnyObjectByType<StressSandbox>();
                SetSerialized(sandbox, "demo", property => property.objectReferenceValue = hub);
            });
        }

        /// <summary>Command-line entry point: configures the project, generates missing assets, then quits.</summary>
        public static async void RunBatch()
        {
            int exitCode = 0;
            try
            {
                await ConfigureProjectAsync();
                CreateCreaturePrefabs();
                CreateMainScene();
                CreateSandboxScene();
                AddDemoDataToScenes();
                AssetDatabase.SaveAssets();
                Debug.Log("[Deskmates] Batch setup finished.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                exitCode = 1;
            }

            EditorApplication.Exit(exitCode);
        }

        static async Task ConfigureProjectAsync()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackageName);

            // Hands-first is an elimination criterion of the competition (PRD 2.1): never require controllers.
            OVRProjectConfig config = OVRProjectConfig.CachedProjectConfig;
            config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;
            config.anchorSupport = OVRProjectConfig.AnchorSupport.Enabled;
            config.sceneSupport = OVRProjectConfig.FeatureSupport.Required;
            config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
            OVRProjectConfig.CommitProjectConfig(config);

            // Meta's Project Setup Tool: XR loader, OpenXR features, player and quality settings.
            // Standalone is what the XR Simulator runs against in the Editor.
            await OVRProjectSetup.FixAllAsync(BuildTargetGroup.Standalone);
            await OVRProjectSetup.FixAllAsync(BuildTargetGroup.Android);
            AssetDatabase.SaveAssets();
        }

        static void CreateCreaturePrefab(string modelName, string prefabName)
        {
            string prefabPath = PrefabPath(prefabName);
            if (File.Exists(prefabPath))
            {
                return;
            }

            string modelPath = $"{ModelsFolder}/{modelName}.glb";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                throw new FileNotFoundException($"Creature model not imported: {modelPath}");
            }

            // The wrapper keeps a unit scale so stress scaling never fights the model's own 0.07 export scale.
            GameObject root = new GameObject(prefabName);
            try
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                AlignModel(instance.transform, modelPath);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[Deskmates] Created {prefabPath}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Adds whatever components or references a creature prefab is missing. The model is the root's first child.
        static void UpdateCreaturePrefab(string prefabName, CreatureConfig config, string sourceId,
            StressCalculator calculator)
        {
            string prefabPath = PrefabPath(prefabName);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform model = root.transform.GetChild(0);
                Creature creature = GetOrAddComponent<Creature>(root);
                CreatureMotion motion = GetOrAddComponent<CreatureMotion>(root);
                CreatureVfx vfx = GetOrAddComponent<CreatureVfx>(root);
                CreatureDataLink link = GetOrAddComponent<CreatureDataLink>(root);
                GetOrAddComponent<CreatureFace>(root);

                Transform puffTransform = root.transform.Find(SighPuffName);
                ParticleSystem puff = puffTransform != null
                    ? puffTransform.GetComponent<ParticleSystem>()
                    : CreateSighPuff(root.transform, model);

                SetSerialized(creature, "config", property => property.objectReferenceValue = config);
                SetSerialized(motion, "visual", property => property.objectReferenceValue = model);
                SetSerialized(vfx, "sighPuff", property => property.objectReferenceValue = puff);
                SetSerialized(link, "sourceId", property => property.stringValue = sourceId);
                SetSerialized(link, "calculator", property => property.objectReferenceValue = calculator);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static CreatureConfig EnsureCreatureConfig(string prefabName, Color restColor)
        {
            string path = $"{CreatureConfigsFolder}/{prefabName.Replace("Creature_", "CreatureConfig_")}.asset";
            CreatureConfig config = AssetDatabase.LoadAssetAtPath<CreatureConfig>(path);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<CreatureConfig>();
            config.restColor = restColor;
            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"[Deskmates] Created {path}.");
            return config;
        }

        static StressCalculator EnsureStressCalculator(string prefabName, Type type)
        {
            string path = $"{StressCalculatorsFolder}/{prefabName.Replace("Creature_", "Stress_")}.asset";
            StressCalculator calculator = AssetDatabase.LoadAssetAtPath<StressCalculator>(path);
            if (calculator != null)
            {
                return calculator;
            }

            calculator = (StressCalculator)ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(calculator, path);
            Debug.Log($"[Deskmates] Created {path}.");
            return calculator;
        }

        // The default scenario (PRD 7.1): about three minutes in which each creature goes from calm to panic and
        // back, at different moments. Names are invented; nothing refers to a real brand or person.
        static DemoScript EnsureDemoScript()
        {
            DemoScript script = AssetDatabase.LoadAssetAtPath<DemoScript>(DemoScriptPath);
            if (script != null)
            {
                return script;
            }

            script = ScriptableObject.CreateInstance<DemoScript>();
            script.events.AddRange(new[]
            {
                // Mail: important mails pile up, then the user goes through the inbox.
                DemoAdd(0f, SourceIds.Mail, "mail-lunch", "Lunch menu for the week", "Office team", -30f),
                DemoAdd(8f, SourceIds.Mail, "mail-contract", "Contract draft for your review", "Maya Lindqvist", -2f, true),
                DemoAdd(25f, SourceIds.Mail, "mail-client-call", "Client call moved to 3 pm", "Tomás Rivera", -1f, true),
                DemoAdd(45f, SourceIds.Mail, "mail-budget", "Budget figures needed today", "Priya Nair", -1f, true),
                DemoAdd(62f, SourceIds.Mail, "mail-contract-followup", "Re: Contract draft, any update?", "Maya Lindqvist", 0f, true),
                DemoAdd(80f, SourceIds.Mail, "mail-invoice", "Invoice 2291 is overdue", "Accounts team", 0f, true),
                DemoResolve(115f, SourceIds.Mail),

                // Calendar: the design review comes closer, then starts. The next meeting is far away.
                DemoAdd(0f, SourceIds.Calendar, "meeting-design-review", "Design review", "with Jonah Park", 70f),
                DemoAdd(0f, SourceIds.Calendar, "meeting-weekly-planning", "Weekly planning", "with the product team", 190f),

                // Tasks: two due today, one tomorrow; two go overdue, then the user ticks them off.
                DemoAdd(0f, SourceIds.Tasks, "task-slides", "Send the slide deck to Priya", "Work", 45f),
                DemoAdd(0f, SourceIds.Tasks, "task-onboarding", "Review the onboarding copy", "Work", 100f),
                DemoAdd(0f, SourceIds.Tasks, "task-offsite", "Book the team offsite venue", "Work", 24f * 60f),
                DemoAdd(50f, SourceIds.Tasks, "task-report", "Update the quarterly report", "Work", 30f),
                DemoResolve(150f, SourceIds.Tasks, "task-slides"),
                DemoResolve(152f, SourceIds.Tasks, "task-report"),
                DemoResolve(154f, SourceIds.Tasks, "task-onboarding"),

                // Messages: two direct messages wait longer and longer, then the user answers.
                DemoAdd(30f, SourceIds.Messages, "dm-sam", "Got a minute to talk about the demo?", "Sam Okafor", 0f),
                DemoAdd(70f, SourceIds.Messages, "dm-lena", "Can you check my numbers before 11?", "Lena Ruiz", 0f),
                DemoResolve(160f, SourceIds.Messages),
            });

            Directory.CreateDirectory(Path.GetDirectoryName(DemoScriptPath));
            AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(script, DemoScriptPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Deskmates] Created {DemoScriptPath}.");
            return script;
        }

        static DemoEvent DemoAdd(float at, string sourceId, string itemId, string title, string detail, float minutesFromNow,
            bool important = false)
        {
            return new DemoEvent
            {
                atSecond = at,
                sourceId = sourceId,
                kind = DemoEventKind.AddItem,
                itemId = itemId,
                title = title,
                detail = detail,
                minutesFromNow = minutesFromNow,
                important = important,
            };
        }

        static DemoEvent DemoResolve(float at, string sourceId, string itemId = "")
        {
            return new DemoEvent { atSecond = at, sourceId = sourceId, kind = DemoEventKind.Resolve, itemId = itemId };
        }

        static DataHub EnsureDataHub(bool enabled)
        {
            // Loaded here rather than passed in: opening a scene unloads assets that nothing references yet.
            DemoScript script = AssetDatabase.LoadAssetAtPath<DemoScript>(DemoScriptPath);
            DataHub hub = Object.FindAnyObjectByType<DataHub>(FindObjectsInactive.Include);
            if (hub == null)
            {
                hub = new GameObject("Data").AddComponent<DataHub>();
                hub.enabled = enabled;
            }

            SetSerialized(hub, "demoScript", property => property.objectReferenceValue = script);
            return hub;
        }

        static void EditScene(string path, Action edit)
        {
            if (!File.Exists(path))
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            edit();
            EditorSceneManager.SaveScene(scene);
        }

        // A small burst of soft white puffs leaving the mouth, forward and slightly up (PRD 4.5).
        static ParticleSystem CreateSighPuff(Transform root, Transform model)
        {
            Renderer mouth = FindPart(model, "MouthSmile");
            GameObject puffObject = new GameObject(SighPuffName);
            puffObject.transform.SetParent(root, false);
            puffObject.transform.localPosition = root.InverseTransformPoint(mouth.bounds.center) + new Vector3(0f, 0f, 0.01f);
            puffObject.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);

            ParticleSystem puff = puffObject.AddComponent<ParticleSystem>();
            puff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = puff.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.02f);
            main.startColor = new Color(1f, 1f, 1f, 0.85f);
            main.gravityModifier = -0.01f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 16;

            ParticleSystem.EmissionModule emission = puff.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

            ParticleSystem.ShapeModule shape = puff.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.004f;

            ParticleSystem.SizeOverLifetimeModule size = puff.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 2.5f));

            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = puff.colorOverLifetime;
            color.enabled = true;
            color.color = fade;

            puffObject.GetComponent<ParticleSystemRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            return puff;
        }

        static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T component) ? component : target.AddComponent<T>();
        }

        // Turns the model so its face looks along the prefab's +Z and rests its base on y = 0.
        static void AlignModel(Transform model, string modelPath)
        {
            Renderer body = FindPart(model, "Body");
            Renderer mouth = FindPart(model, "MouthSmile");
            if (body == null || mouth == null)
            {
                throw new InvalidOperationException($"{modelPath} has no Body or MouthSmile part (PRD 10.1).");
            }

            Vector3 face = Vector3.ProjectOnPlane(mouth.bounds.center - body.bounds.center, Vector3.up);
            float yaw = Vector3.SignedAngle(face, Vector3.forward, Vector3.up);
            model.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * model.rotation;
            model.position += Vector3.down * body.bounds.min.y;
        }

        static Renderer FindPart(Transform model, string partKey)
        {
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (CreatureFace.PartKey(renderer.name) == partKey)
                {
                    return renderer;
                }
            }

            return null;
        }

        static GameObject InstantiatePackagePrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new FileNotFoundException($"Prefab not found: {path}");
            }

            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        static void SetSerialized(Object target, string propertyName, Action<SerializedProperty> assign)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name} has no serialized property '{propertyName}'.");
            }

            assign(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static string PrefabPath(string prefabName) => $"{CreaturePrefabsFolder}/{prefabName}.prefab";
    }
}
