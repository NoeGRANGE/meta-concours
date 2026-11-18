using System;
using System.IO;
using System.Threading.Tasks;
using Deskmates.Core;
using Deskmates.Creatures;
using Deskmates.Data;
using Deskmates.Data.Demo;
using Deskmates.Data.Live;
using Deskmates.Data.Stress;
using Deskmates.Desk;
using Deskmates.Interaction;
using Deskmates.Sandbox;
using Meta.XR.MRUtilityKit;
using Oculus.Interaction;
using Oculus.Interaction.Editor.QuickActions;
using Oculus.Interaction.Input;
using Oculus.Interaction.OVR.Editor.QuickActions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using Deskmates.UI;
using UnityEngine;
using UnityEngine.UI;
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
        const string AndroidPackageName = "com.fretlings.app";
        const string ModelsFolder = "Assets/_Project/Art/Models";
        const string CreaturePrefabsFolder = "Assets/_Project/Prefabs/Creatures";
        const string CreatureConfigsFolder = "Assets/_Project/Settings/Creatures";
        const string StressCalculatorsFolder = "Assets/_Project/Settings/Stress";
        const string DemoScriptPath = "Assets/_Project/Settings/Data/DemoScript_Default.asset";
        const string DataSettingsPath = "Assets/_Project/Settings/Data/DataSettings.asset";
        const string DeskCalibrationConfigPath = "Assets/_Project/Settings/Desk/DeskCalibrationConfig.asset";
        const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        const string SandboxScenePath = "Assets/_Project/Scenes/Sandbox_Creature.unity";
        const string ParticleMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";
        const string SighPuffName = "SighPuff";
        const string UiFontPath = "Assets/_Project/Art/Fonts/Lato-Regular.ttf";
        const string RoundedSpritePath = "Assets/_Project/Art/UI/Rounded.png";
        const string OutlineMaterialPath = "Assets/_Project/Art/Materials/GazeOutline.mat";
        const string AudioFolder = "Assets/_Project/Art/Audio";

        // The sounds chosen by the user from SourceArt/sounds/propositions.html, and their volumes.
        static readonly (Sound sound, string clip, float volume)[] Sounds =
        {
            (Sound.Landing, "landing", 0.30f),
            (Sound.Sigh, "sigh", 0.45f),
            (Sound.PanelOpen, "panel", 0.25f),
            (Sound.PanelClose, "panel", 0.15f),
            (Sound.ItemHandled, "item", 0.30f),
            (Sound.Ripple, "ripple", 0.35f),
            (Sound.FamilyArrives, "family", 0.35f),
            (Sound.AgentWaiting, "agent_waiting", 0.25f),
        };

        // Interface palette, taken from the creatures (STYLE.md): warm cream card, cocoa ink.
        static readonly Color UiCard = new Color32(0xFB, 0xF6, 0xEE, 0xF2);
        static readonly Color UiInk = new Color32(0x4A, 0x3B, 0x33, 0xFF);
        static readonly Color UiMuted = new Color32(0x9A, 0x89, 0x7C, 0xFF);

        // Parts of Meta's interaction rig that move the user around: a seated app has no use for them (PRD 2.2).
        static readonly string[] LocomotionParts =
        {
            "Locomotor", "LocomotionHandInteractorGroup", "LocomotionControllerInteractorGroup",
            "MicroGesturesLocomotionHandInteractorGroup",
        };
        const string CameraRigPrefabPath = "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab";
        const string MrukPrefabPath = "Packages/com.meta.xr.mrutilitykit/Core/Tools/MRUK.prefab";
        const string MailCreaturePrefab = "Creature_Mail";

        // Model file, prefab name (PRD 9), colour at rest (PRD 4.2), data source, stress formula (PRD 4.4), and the
        // accent of its panel: a deeper tone of its accessory, readable on the cream card (STYLE.md).
        static readonly (string model, string prefab, Color32 restColor, string sourceId, Type stress, Color32 accent)[] Creatures =
        {
            ("Facteur", MailCreaturePrefab, new Color32(0xEF, 0xE6, 0xD8, 0xFF), SourceIds.Mail, typeof(MailStress), new Color32(0x5B, 0x7F, 0xB8, 0xFF)),
            ("Reunions", "Creature_Calendar", new Color32(0xDC, 0xE3, 0xE6, 0xFF), SourceIds.Calendar, typeof(CalendarStress), new Color32(0x7E, 0x9A, 0xA8, 0xFF)),
            ("Taches", "Creature_Tasks", new Color32(0xE6, 0xDC, 0xEB, 0xFF), SourceIds.Tasks, typeof(TasksStress), new Color32(0xA5, 0x8B, 0xB5, 0xFF)),
            ("Messages", "Creature_Messages", new Color32(0xD7, 0xE3, 0xCF, 0xFF), SourceIds.Messages, typeof(MessagesStress), new Color32(0x8D, 0xAA, 0x7C, 0xFF)),
            ("Agents", "Creature_Agents", new Color32(0xD9, 0xE4, 0xEC, 0xFF), SourceIds.Agents, typeof(AgentsStress), new Color32(0xE0, 0xB0, 0x3A, 0xFF)),
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
            foreach ((string model, string prefab, Color32 restColor, string sourceId, Type stress, Color32 accent) in Creatures)
            {
                CreatureConfig config = EnsureCreatureConfig(prefab, restColor);
                StressCalculator calculator = EnsureStressCalculator(prefab, stress);
                CreateCreaturePrefab(model, prefab);
                UpdateCreaturePrefab(prefab, config, sourceId, calculator, accent);
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

            // Passthrough is always on: the user sees the real room, so the boundary needs not clear the creatures.
            manager.shouldBoundaryVisibilityBeSuppressed = true;

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
            SetSerialized(app, "head", property => property.objectReferenceValue = centerEye);

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
            EnsureDataSettings();
            EditScene(MainScenePath, () => EnsureDataHub(true));
            EditScene(SandboxScenePath, () =>
            {
                DataHub hub = EnsureDataHub(false);
                StressSandbox sandbox = Object.FindAnyObjectByType<StressSandbox>();
                SetSerialized(sandbox, "demo", property => property.objectReferenceValue = hub);
            });
        }

        /// <summary>
        /// Gives Main the Interaction SDK hands and the desk session (calibration, memory, editor shortcuts),
        /// and wires them to the app. Safe to run again: it only adds what is missing.
        /// </summary>
        [MenuItem("Deskmates/Setup/6. Add Hands and Desk to Main")]
        public static void AddHandsAndDeskToMain()
        {
            EnsureDeskCalibrationConfig();
            EditScene(MainScenePath, () =>
            {
                if (Object.FindAnyObjectByType<OVRCameraRigRef>() == null)
                {
                    OVRQuickActionsAPI.AddOVRInteractionRig(false);
                }

                foreach (Transform part in Object.FindAnyObjectByType<OVRCameraRigRef>().GetComponentsInChildren<Transform>(true))
                {
                    if (Array.IndexOf(LocomotionParts, part.name) >= 0)
                    {
                        part.gameObject.SetActive(false);
                    }
                }

                Transform head = Object.FindAnyObjectByType<OVRCameraRig>().transform.Find("TrackingSpace/CenterEyeAnchor");
                DeskSession session = Object.FindAnyObjectByType<DeskSession>(FindObjectsInactive.Include);
                if (session == null)
                {
                    session = CreateDeskSession();
                }

                DeskCalibration calibration = session.GetComponent<DeskCalibration>();
                DeskRipple ripple = session.GetComponentInChildren<DeskRipple>(true);
                DeskCalibrationConfig config = AssetDatabase.LoadAssetAtPath<DeskCalibrationConfig>(DeskCalibrationConfigPath);
                SetSerialized(calibration, "config", property => property.objectReferenceValue = config);
                SetSerialized(calibration, "head", property => property.objectReferenceValue = head);
                SetSerialized(calibration, "ripple", property => property.objectReferenceValue = ripple);
                SetSerialized(session, "calibration", property => property.objectReferenceValue = calibration);
                SetSerialized(session.GetComponent<DeskDebugKeys>(), "session", property => property.objectReferenceValue = session);
                AppBootstrap app = Object.FindAnyObjectByType<AppBootstrap>();
                SetSerialized(app, "desk", property => property.objectReferenceValue = session);

                // The whole family on the desk, mail creature first (PRD 4.2).
                SetSerialized(app, "creaturePrefabs", property =>
                {
                    property.arraySize = Creatures.Length;
                    for (int i = 0; i < Creatures.Length; i++)
                    {
                        property.GetArrayElementAtIndex(i).objectReferenceValue =
                            AssetDatabase.LoadAssetAtPath<Creature>(PrefabPath(Creatures[i].prefab));
                    }
                });

                // First-run walkthrough and hand prompt (PRD 6.1), on a card in front of the user.
                Onboarding onboarding = Object.FindAnyObjectByType<Onboarding>(FindObjectsInactive.Include);
                if (onboarding == null)
                {
                    CreateOnboarding(app, session, head);
                }
                else
                {
                    GetOrAddComponent<PopIn>(onboarding.GetComponentInChildren<Canvas>(true).gameObject);
                }

                // A few soft sounds (STYLE.md 6), played where they happen.
                SoundBank bank = Object.FindAnyObjectByType<SoundBank>(FindObjectsInactive.Include);
                if (bank == null)
                {
                    bank = new GameObject("Sounds").AddComponent<SoundBank>();
                }

                SetSerialized(bank, "entries", property =>
                {
                    property.arraySize = Sounds.Length;
                    for (int i = 0; i < Sounds.Length; i++)
                    {
                        SerializedProperty entry = property.GetArrayElementAtIndex(i);
                        entry.FindPropertyRelative("sound").enumValueIndex = (int)Sounds[i].sound;
                        entry.FindPropertyRelative("clip").objectReferenceValue =
                            AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/{Sounds[i].clip}.wav");
                        entry.FindPropertyRelative("volume").floatValue = Sounds[i].volume;
                    }
                });

                // Look and pinch (PRD 5): the gaze selector and the shared detail panel.
                GazeSelector gaze = Object.FindAnyObjectByType<GazeSelector>();
                if (gaze == null)
                {
                    gaze = new GameObject("Gaze").AddComponent<GazeSelector>();
                }

                SetSerialized(gaze, "head", property => property.objectReferenceValue = head);
                // A panel from before the current style (with its accent pill) is rebuilt; a current one is kept, so
                // the scene does not churn.
                DetailPanel existing = Object.FindAnyObjectByType<DetailPanel>(FindObjectsInactive.Include);
                bool current = existing != null && existing.transform.Find("Canvas/Accent") != null;
                if (existing != null && !current)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }

                if (!current)
                {
                    CreateDetailPanel(gaze);
                }
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
                AddHandsAndDeskToMain();
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
            PlayerSettings.productName = "Fretlings";

            // Live mode talks to the desktop companion over plain HTTP on the local network.
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;

            // Hands-first is an elimination criterion of the competition (PRD 2.1): never require controllers.
            OVRProjectConfig config = OVRProjectConfig.CachedProjectConfig;
            config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;
            config.anchorSupport = OVRProjectConfig.AnchorSupport.Enabled;
            config.sceneSupport = OVRProjectConfig.FeatureSupport.Required;
            config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
            config.boundaryVisibilitySupport = OVRProjectConfig.FeatureSupport.Supported;
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
            StressCalculator calculator, Color accent)
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
                EnsureGrab(root, sourceId);
                EnsurePeek(root, accent);
                Material outlineMaterial = EnsureOutlineMaterial();
                SetSerialized(GetOrAddComponent<GazeOutline>(root), "material", property => property.objectReferenceValue = outlineMaterial);
                if (sourceId == SourceIds.Agents)
                {
                    GetOrAddComponent<AgentsLamp>(root);
                }

                if (sourceId == SourceIds.Mail)
                {
                    AudioClip flames = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/flame_loop.wav");
                    SetSerialized(creature, "panicLoop", property => property.objectReferenceValue = flames);
                }

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

        // Hand grab and distance grab from the Interaction SDK (PRD 5), then our own fall onto the desk (PRD 5.1).
        static void EnsureGrab(GameObject root, string creatureId)
        {
            if (!root.TryGetComponent(out BoxCollider collider))
            {
                Bounds body = FindPart(root.transform.GetChild(0), "Body").bounds;
                collider = root.AddComponent<BoxCollider>();
                collider.center = root.transform.InverseTransformPoint(body.center);
                collider.size = body.size;
            }

            if (!root.TryGetComponent(out Grabbable grabbable))
            {
                QuickActionsAPI.AddGrabInteraction(root);
                grabbable = root.GetComponent<Grabbable>();
            }

            // Released creatures fall back onto the desk by themselves, not by physics.
            SetSerialized(grabbable, "_throwWhenUnselected", property => property.boolValue = false);
            GrabAndDrop drop = GetOrAddComponent<GrabAndDrop>(root);
            SetSerialized(drop, "grabbable", property => property.objectReferenceValue = grabbable);
            SetSerialized(drop, "creatureId", property => property.stringValue = creatureId);
        }

        // Pinching at a distance selects (opens the panel), so a creature is only grabbed by a hand next to it.
        // Prefabs built before this keep a distance grab child: remove it.
        static void EnsurePeek(GameObject root, Color accent)
        {
            Transform distanceGrab = root.transform.Find("ISDK_DistanceHandGrabInteraction");
            if (distanceGrab != null)
            {
                Object.DestroyImmediate(distanceGrab.gameObject);
            }

            CreatureInfo info = GetOrAddComponent<CreatureInfo>(root);
            SetSerialized(info, "accent", property => property.colorValue = accent);

            // A bubble from before the current style (with its tail) is rebuilt; a current one is kept, so prefabs
            // do not churn.
            Transform oldPeek = root.transform.Find("Peek");
            if (oldPeek != null && oldPeek.Find("Tail") != null)
            {
                return;
            }

            if (oldPeek != null)
            {
                Object.DestroyImmediate(oldPeek.gameObject);
            }

            Canvas peek = CreateWorldCanvas("Peek", root.transform, new Vector2(440f, 72f));
            peek.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            peek.gameObject.AddComponent<PopIn>();

            // A small diamond under the bubble, pointing at its creature.
            GameObject tail = new GameObject("Tail", typeof(RectTransform));
            tail.transform.SetParent(peek.transform, false);
            Place((RectTransform)tail.transform, 0f, -36f, 22f, 22f);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.AddComponent<Image>().color = UiCard;

            Text text = CreateText(peek.transform, "Text", 28, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(18f, 0f);
            text.rectTransform.offsetMax = new Vector2(-18f, 0f);
            SetSerialized(info, "peek", property => property.objectReferenceValue = peek);
            SetSerialized(info, "peekText", property => property.objectReferenceValue = text);
        }

        static void CreateDetailPanel(GazeSelector gaze)
        {
            const float width = 440f;
            const float rowHeight = 70f;
            DetailPanel panel = new GameObject("Detail Panel").AddComponent<DetailPanel>();
            Canvas canvas = CreateWorldCanvas("Canvas", panel.transform, new Vector2(width, 330f));
            canvas.gameObject.AddComponent<PopIn>();

            // A pill at the top in the colour of the creature the panel belongs to (set when it opens).
            GameObject pill = new GameObject("Accent", typeof(RectTransform));
            pill.transform.SetParent(canvas.transform, false);
            Place((RectTransform)pill.transform, 0f, 152f, 72f, 10f);
            Image accent = pill.AddComponent<Image>();
            Round(accent, 5f);

            Text title = CreateText(canvas.transform, "Title", 26, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, 0f, 135f, width - 32f, 50f);
            Text hint = CreateText(canvas.transform, "Hint", 18, TextAnchor.MiddleCenter);
            hint.color = UiMuted;
            Place(hint.rectTransform, 0f, -138f, width - 32f, 44f);

            PanelRow[] rows = new PanelRow[3];
            for (int i = 0; i < rows.Length; i++)
            {
                GameObject row = new GameObject($"Row {i}", typeof(RectTransform));
                row.transform.SetParent(canvas.transform, false);
                Place((RectTransform)row.transform, 0f, 70f - i * (rowHeight + 6f), width - 32f, rowHeight);
                Image background = row.AddComponent<Image>();
                Round(background, 2f);
                background.color = PanelRow.Idle;
                BoxCollider collider = row.AddComponent<BoxCollider>();
                collider.size = new Vector3(width - 32f, rowHeight, 10f);
                Text label = CreateText(row.transform, "Label", 22, TextAnchor.MiddleLeft);
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(16f, 0f);
                label.rectTransform.offsetMax = new Vector2(-12f, 0f);
                rows[i] = row.AddComponent<PanelRow>();
                SetSerialized(rows[i], "label", property => property.objectReferenceValue = label);
                SetSerialized(rows[i], "background", property => property.objectReferenceValue = background);
            }

            SetSerialized(panel, "gaze", property => property.objectReferenceValue = gaze);
            SetSerialized(panel, "canvas", property => property.objectReferenceValue = canvas);
            SetSerialized(panel, "title", property => property.objectReferenceValue = title);
            SetSerialized(panel, "hint", property => property.objectReferenceValue = hint);
            SetSerialized(panel, "accent", property => property.objectReferenceValue = accent);
            SetSerialized(panel, "rows", property =>
            {
                property.arraySize = rows.Length;
                for (int i = 0; i < rows.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
                }
            });
        }

        static void CreateOnboarding(AppBootstrap app, DeskSession session, Transform head)
        {
            Onboarding onboarding = new GameObject("Onboarding").AddComponent<Onboarding>();
            Canvas card = CreateWorldCanvas("Card", onboarding.transform, new Vector2(620f, 130f));
            card.gameObject.AddComponent<PopIn>();
            Text text = CreateText(card.transform, "Text", 30, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(24f, 10f);
            text.rectTransform.offsetMax = new Vector2(-24f, -10f);
            text.lineSpacing = 1.1f;
            card.gameObject.SetActive(false);
            SetSerialized(onboarding, "app", property => property.objectReferenceValue = app);
            SetSerialized(onboarding, "desk", property => property.objectReferenceValue = session);
            SetSerialized(onboarding, "head", property => property.objectReferenceValue = head);
            SetSerialized(onboarding, "card", property => property.objectReferenceValue = card);
            SetSerialized(onboarding, "cardText", property => property.objectReferenceValue = text);
        }

        // 0.6 mm per canvas unit: a 28-unit line is about 1.7 cm tall, over 0.9 degrees at arm's length (PRD 5).
        static Canvas CreateWorldCanvas(string name, Transform parent, Vector2 size)
        {
            GameObject canvasObject = new GameObject(name, typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * 0.0006f;
            Image background = canvasObject.AddComponent<Image>();
            Round(background, 1f);
            background.color = UiCard;
            return canvas;
        }

        // A soft rounded card, like the creatures' vinyl: radius 24 units at multiplier 1, half that at 2.
        static void Round(Image image, float multiplier)
        {
            image.sprite = EnsureRoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = multiplier;
        }

        // Generated once rather than drawn by hand, so the corners stay exactly circular.
        static Sprite EnsureRoundedSprite()
        {
            const int size = 64;
            const int radius = 24;
            if (AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath) == null)
            {
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        Vector2 pixel = new Vector2(x + 0.5f, y + 0.5f);
                        Vector2 corner = new Vector2(Mathf.Clamp(pixel.x, radius, size - radius), Mathf.Clamp(pixel.y, radius, size - radius));
                        float alpha = Mathf.Clamp01(radius - Vector2.Distance(pixel, corner) + 0.5f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(RoundedSpritePath));
                File.WriteAllBytes(RoundedSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(RoundedSpritePath);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(RoundedSpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(radius, radius, radius, radius);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        }

        // A material asset, not one made at run time, so the outline shader is kept in the build.
        static Material EnsureOutlineMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (material == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutlineMaterialPath));
                material = new Material(Shader.Find("Fretlings/GazeOutline"));
                AssetDatabase.CreateAsset(material, OutlineMaterialPath);
                Debug.Log($"[Deskmates] Created {OutlineMaterialPath}.");
            }

            return material;
        }

        static Text CreateText(Transform parent, string name, int size, TextAnchor anchor)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath);
            text.fontSize = size;
            text.alignment = anchor;
            text.color = UiInk;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static DeskSession CreateDeskSession()
        {
            GameObject sessionObject = new GameObject("Desk Session");
            sessionObject.AddComponent<DeskCalibration>();
            sessionObject.AddComponent<DeskDebugKeys>();
            DeskSession session = sessionObject.AddComponent<DeskSession>();

            GameObject rippleObject = new GameObject("Desk Ripple");
            rippleObject.transform.SetParent(sessionObject.transform, false);
            LineRenderer line = rippleObject.AddComponent<LineRenderer>();
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            rippleObject.AddComponent<DeskRipple>();
            return session;
        }

        static void EnsureDeskCalibrationConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<DeskCalibrationConfig>(DeskCalibrationConfigPath) != null)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(DeskCalibrationConfigPath));
            AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DeskCalibrationConfig>(), DeskCalibrationConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Deskmates] Created {DeskCalibrationConfigPath}.");
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
        // Demo by default; switching its mode to Live connects the creatures to the desktop companion.
        static void EnsureDataSettings()
        {
            if (AssetDatabase.LoadAssetAtPath<DataSettings>(DataSettingsPath) != null)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(DataSettingsPath));
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DataSettings>(), DataSettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Deskmates] Created {DataSettingsPath}.");
        }

        static DemoScript EnsureDemoScript()
        {
            DemoScript script = AssetDatabase.LoadAssetAtPath<DemoScript>(DemoScriptPath);
            if (script != null)
            {
                // Scripts made before the agents creature get its sessions once.
                if (!script.events.Exists(e => e.sourceId == SourceIds.Agents))
                {
                    script.events.AddRange(AgentsDemoEvents());
                    EditorUtility.SetDirty(script);
                    AssetDatabase.SaveAssets();
                }

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
            script.events.AddRange(AgentsDemoEvents());

            Directory.CreateDirectory(Path.GetDirectoryName(DemoScriptPath));
            AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(script, DemoScriptPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Deskmates] Created {DemoScriptPath}.");
            return script;
        }

        // Agents: coding sessions run, one asks for permission, one fails, one finishes; the user answers and looks
        // at them, and the last one finishes calmly. Session names stand for project folders; no tool is named.
        static DemoEvent[] AgentsDemoEvents() => new[]
        {
            DemoSession(5f, "refactor-auth", "Refactor the login flow", AgentStates.Running),
            DemoSession(12f, "fix-flaky-tests", "Fix the flaky checkout tests", AgentStates.Running),
            DemoSession(28f, "refactor-auth", "Asks to run the database migration", AgentStates.Waiting),
            DemoSession(45f, "docs-site", "Update the API docs", AgentStates.Running),
            DemoSession(60f, "fix-flaky-tests", "Two tests still time out", AgentStates.Failed),
            DemoSession(85f, "docs-site", "12 pages updated", AgentStates.Done),
            DemoSession(110f, "refactor-auth", "Migrating the sessions table", AgentStates.Running),
            DemoResolve(125f, SourceIds.Agents, "fix-flaky-tests"),
            DemoResolve(132f, SourceIds.Agents, "docs-site"),
            DemoSession(150f, "refactor-auth", "Login flow refactored, all tests green", AgentStates.Done),
            DemoResolve(170f, SourceIds.Agents, "refactor-auth"),
        };

        static DemoEvent DemoSession(float at, string session, string detail, string state)
        {
            DemoEvent demoEvent = DemoAdd(at, SourceIds.Agents, session, session, detail, 0f);
            demoEvent.state = state;
            return demoEvent;
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
            DataSettings settings = AssetDatabase.LoadAssetAtPath<DataSettings>(DataSettingsPath);
            SetSerialized(hub, "settings", property => property.objectReferenceValue = settings);
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
