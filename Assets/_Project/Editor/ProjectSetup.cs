using System;
using System.IO;
using System.Threading.Tasks;
using Deskmates.Core;
using Deskmates.Creatures;
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
    /// Project configuration and first-time generation of the creature prefabs and the Main scene.
    /// Generated assets are never overwritten: delete one to have it rebuilt.
    /// Headless: Unity -batchmode -projectPath . -executeMethod Deskmates.Editor.ProjectSetup.RunBatch
    /// </summary>
    public static class ProjectSetup
    {
        const string AndroidPackageName = "com.deskmates.app";
        const string ModelsFolder = "Assets/_Project/Art/Models";
        const string CreaturePrefabsFolder = "Assets/_Project/Prefabs/Creatures";
        const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        const string CameraRigPrefabPath = "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab";
        const string MrukPrefabPath = "Packages/com.meta.xr.mrutilitykit/Core/Tools/MRUK.prefab";
        const string MailCreaturePrefab = "Creature_Mail";

        // Model file name -> prefab name (PRD 9).
        static readonly (string model, string prefab)[] Creatures =
        {
            ("Facteur", MailCreaturePrefab),
            ("Reunions", "Creature_Calendar"),
            ("Taches", "Creature_Tasks"),
            ("Messages", "Creature_Messages"),
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

        [MenuItem("Deskmates/Setup/2. Create Creature Prefabs")]
        public static void CreateCreaturePrefabs()
        {
            Directory.CreateDirectory(CreaturePrefabsFolder);
            AssetDatabase.Refresh();
            foreach ((string model, string prefab) in Creatures)
            {
                CreateCreaturePrefab(model, prefab);
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

        /// <summary>Command-line entry point: configures the project, generates missing assets, then quits.</summary>
        public static async void RunBatch()
        {
            int exitCode = 0;
            try
            {
                await ConfigureProjectAsync();
                CreateMainScene();
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
                root.AddComponent<Creature>();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[Deskmates] Created {prefabPath}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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
