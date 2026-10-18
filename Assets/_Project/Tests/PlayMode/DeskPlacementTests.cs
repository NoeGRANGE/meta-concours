#if UNITY_EDITOR
using System.Collections;
using System.Threading.Tasks;
using Deskmates.Core;
using Deskmates.Creatures;
using Meta.XR.MRUtilityKit;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Deskmates.Tests
{
    /// <summary>
    /// Runs the Main scene against MRUK's bundled office rooms, which stand in for a real room scan.
    /// </summary>
    public class DeskPlacementTests
    {
        const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        const string RoomsFolder = "Packages/com.meta.xr.mrutilitykit/Core/Rooms/Prefabs/Office";
        const float Tolerance = 0.002f;

        static readonly string[] Rooms =
        {
            "Office00", "Office01", "Office02", "Office03", "Office04",
            "Office05", "Office06", "Office07", "Office08", "Office09",
        };

        [UnityTest]
        public IEnumerator MailCreatureSitsCalmOnADesk([ValueSource(nameof(Rooms))] string roomName)
        {
            // There is no headset in a test run, so OVR and MRUK log errors about the missing device.
            LogAssert.ignoreFailingMessages = true;

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                MainScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            GameObject roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{RoomsFolder}/{roomName}.prefab");
            Assert.IsNotNull(roomPrefab, $"Room prefab {roomName} not found.");
            Task<MRUK.LoadDeviceResult> load = MRUK.Instance.LoadSceneFromPrefab(roomPrefab);
            yield return new WaitUntil(() => load.IsCompleted);
            Assert.AreEqual(MRUK.LoadDeviceResult.Success, load.Result);
            yield return null;

            AppBootstrap app = Object.FindAnyObjectByType<AppBootstrap>();
            Assert.IsNotNull(app, "Main scene has no AppBootstrap.");
            Creature creature = app.MailCreature;
            Assert.IsNotNull(creature, "The mail creature was not spawned.");

            AssertRestsOnATable(creature);
            AssertFacesUser(creature);
            AssertIsCalm(creature);
        }

        static void AssertRestsOnATable(Creature creature)
        {
            Vector3 position = creature.transform.position;
            MRUKAnchor desk = null;
            foreach (MRUKAnchor anchor in MRUK.Instance.GetCurrentRoom().Anchors)
            {
                if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE) &&
                    anchor.IsPositionInVolume(position, false) &&
                    Mathf.Abs(TopHeight(anchor) - position.y) < Tolerance)
                {
                    desk = anchor;
                    break;
                }
            }

            Assert.IsNotNull(desk, $"Creature at {position} is not on top of any table.");

            Bounds bounds = VisibleBounds(creature);
            Assert.AreEqual(position.y, bounds.min.y, Tolerance, "The creature's base should rest on the desk.");
            Assert.That(bounds.size.y, Is.InRange(0.05f, 0.12f), "The creature should be about 7 cm tall.");
        }

        static void AssertFacesUser(Creature creature)
        {
            Vector3 head = Camera.main.transform.position;
            Vector3 toUser = Vector3.ProjectOnPlane(head - creature.transform.position, Vector3.up);
            if (toUser.sqrMagnitude < 1e-4f)
            {
                return;
            }

            Assert.Greater(Vector3.Dot(creature.transform.forward, toUser.normalized), 0.99f,
                "The creature should face the user.");

            // The face itself, not just the prefab root, must point at the user.
            Vector3 body = default;
            Vector3 mouth = default;
            foreach (Renderer renderer in creature.GetComponentsInChildren<Renderer>(true))
            {
                switch (CreatureFace.PartKey(renderer.name))
                {
                    case "Body":
                        body = renderer.bounds.center;
                        break;
                    case "MouthSmile":
                        mouth = renderer.bounds.center;
                        break;
                }
            }

            Vector3 face = Vector3.ProjectOnPlane(mouth - body, Vector3.up).normalized;
            Assert.Greater(Vector3.Dot(face, toUser.normalized), 0.99f, "The creature's face should point at the user.");
        }

        static void AssertIsCalm(Creature creature)
        {
            foreach (Transform part in creature.GetComponentsInChildren<Transform>(true))
            {
                switch (CreatureFace.PartKey(part.name))
                {
                    case "EyeClosed":
                    case "Cheek":
                    case "MouthSmile":
                    case "Body":
                        Assert.IsTrue(part.gameObject.activeInHierarchy, $"{part.name} should be visible when calm.");
                        break;
                    case "EyeOpen":
                    case "EyeShine":
                    case "EyeWide":
                    case "EyePupil":
                    case "Brow":
                    case "MouthFlat":
                    case "MouthWorried":
                    case "MouthOpen":
                    case "Sweat":
                    case "FlameMain":
                    case "FlameLeft":
                    case "FlameRight":
                    case "FlameCore":
                        Assert.IsFalse(part.gameObject.activeInHierarchy, $"{part.name} should be hidden when calm.");
                        break;
                }
            }
        }

        static float TopHeight(MRUKAnchor anchor)
        {
            Bounds volume = anchor.VolumeBounds.Value;
            float top = float.MinValue;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = new Vector3(
                    (corner & 1) == 0 ? volume.min.x : volume.max.x,
                    (corner & 2) == 0 ? volume.min.y : volume.max.y,
                    (corner & 4) == 0 ? volume.min.z : volume.max.z);
                top = Mathf.Max(top, anchor.transform.TransformPoint(local).y);
            }

            return top;
        }

        static Bounds VisibleBounds(Creature creature)
        {
            Renderer[] renderers = creature.GetComponentsInChildren<Renderer>(false);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }
    }
}
#endif
