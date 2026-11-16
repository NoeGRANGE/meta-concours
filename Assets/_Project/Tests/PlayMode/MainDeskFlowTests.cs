#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Deskmates.Core;
using Deskmates.Creatures;
using Deskmates.Desk;
using Deskmates.Interaction;
using Meta.XR.MRUtilityKit;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Deskmates.Tests
{
    /// <summary>
    /// The first-run desk flow in the Main scene: no known desk, so a calibration starts; once the hand is on the desk,
    /// the mail creature drops onto it and its place is remembered (PRD 5.1 and 5.2).
    /// </summary>
    public class MainDeskFlowTests
    {
        const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        const string RoomPath = "Packages/com.meta.xr.mrutilitykit/Core/Rooms/Prefabs/Office/Office00.prefab";

        string storePath;

        [SetUp]
        public void SetUp()
        {
            storePath = Path.Combine(Path.GetTempPath(), $"desks-{Guid.NewGuid()}.json");
            DeskSession.ServicesOverride = () => (new InMemoryAnchorService(isPersistent: true), new DeskStore(storePath));
        }

        [TearDown]
        public void TearDown()
        {
            DeskSession.ServicesOverride = null;
            File.Delete(storePath);
        }

        [UnityTest]
        public IEnumerator CalibratingDropsTheMailCreatureOntoTheDesk()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                MainScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            DeskSession desk = DeskSession.Instance;
            Assert.IsNotNull(desk, "Main has no DeskSession.");
            Assert.IsTrue(desk.IsCalibrating, "Without a known desk, a calibration starts.");

            Task<MRUK.LoadDeviceResult> load =
                MRUK.Instance.LoadSceneFromPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath));
            yield return new WaitUntil(() => load.IsCompleted);
            yield return null;

            Creature mail = Object.FindAnyObjectByType<AppBootstrap>().MailCreature;
            Assert.IsNotNull(mail, "Before calibration the creature waits on the scanned table.");

            // Released above the table before any calibration, it falls back onto the table.
            GrabAndDrop grab = mail.GetComponent<GrabAndDrop>();
            float tableHeight = mail.transform.position.y;
            mail.transform.position += Vector3.up * 0.2f;
            grab.DropFromHere();
            Assert.IsTrue(grab.IsFalling, "Without a desk, a released creature still falls onto the scanned table.");
            float limit = Time.time + 5f;
            while (Time.time < limit && grab.IsFalling)
            {
                yield return null;
            }

            Assert.AreEqual(tableHeight, mail.transform.position.y, 1e-3, "It rests on the table.");

            desk.Calibration.Complete(DeskCalibration.MeasurementAt(mail.transform.position));
            limit = Time.time + 5f;
            while (Time.time < limit && (mail.transform.parent != desk.Root || grab.IsFalling))
            {
                yield return null;
            }

            Assert.IsTrue(desk.HasDesk);
            Assert.AreSame(desk.Root, mail.transform.parent, "The creature belongs to the new desk.");
            Assert.AreEqual(desk.Plane.Height, mail.transform.position.y, 1e-3, "It rests on the desk.");
            Assert.IsTrue(desk.TryGetPlacement("mail", out _), "Its place is remembered.");
        }
    }
}
#endif
