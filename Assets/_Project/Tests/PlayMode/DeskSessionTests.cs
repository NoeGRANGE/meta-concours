#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deskmates.Desk;
using Deskmates.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Deskmates.Tests
{
    /// <summary>
    /// The desk memory of PRD 5.2: calibrate, find the desk again after a restart, move it, change rooms, forget it,
    /// and creatures that fall onto it and keep their place. Anchors live in memory, standing in for the headset.
    /// </summary>
    public class DeskSessionTests
    {
        const string MailPrefab = "Assets/_Project/Prefabs/Creatures/Creature_Mail.prefab";

        Dictionary<Guid, Pose> anchorsInThisRoom;
        string storePath;
        GameObject sessionObject;
        DeskSession session;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // An empty scene, so nothing left by other tests (a Main scene, a scanned room) gets in the way.
            Scene fresh = SceneManager.CreateScene($"DeskSessionTest-{Guid.NewGuid()}");
            SceneManager.SetActiveScene(fresh);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded != fresh)
                {
                    yield return SceneManager.UnloadSceneAsync(loaded);
                }
            }

            anchorsInThisRoom = new Dictionary<Guid, Pose>();
            storePath = Path.Combine(Path.GetTempPath(), $"desks-{Guid.NewGuid()}.json");
            DeskSession.ServicesOverride = () =>
                (new InMemoryAnchorService(anchorsInThisRoom, isPersistent: true), new DeskStore(storePath));
        }

        [TearDown]
        public void TearDown()
        {
            DeskSession.ServicesOverride = null;
            File.Delete(storePath);
        }

        [UnityTest]
        public IEnumerator ACalibratedDeskIsFoundAgainWithItsCreature()
        {
            StartSession();
            yield return Await(session.RestoreAsync(), restored => Assert.IsFalse(restored, "Nothing saved yet."));

            yield return Calibrate(new Vector3(0.1f, 0.74f, 0.45f));
            Assert.IsTrue(session.HasDesk);
            Assert.AreEqual(0.74f, session.Plane.Height, 1e-4);

            GrabAndDrop mail = SpawnMail(session.Plane.Center + new Vector3(0.05f, 0.2f, 0.05f));
            mail.DropFromHere();
            yield return WaitUntilLanded(mail);
            Assert.AreEqual(0.74f, mail.transform.position.y, 1e-3, "It landed on the desk.");
            Assert.AreSame(session.Root, mail.transform.parent, "It belongs to the desk now.");
            Vector3 landedAt = mail.transform.position;
            yield return new WaitUntil(() => session.Persisting.IsCompleted);
            yield return WaitForStoreContaining("mail");

            // Restart: a new session finds the same desk and the creature's place.
            RestartSession();
            yield return Await(session.RestoreAsync(), restored => Assert.IsTrue(restored));
            Assert.AreEqual(0.74f, session.Plane.Height, 1e-4);
            Assert.IsTrue(session.TryGetPlacement("mail", out Pose saved));
            Assert.That(Vector3.Distance(landedAt, saved.position), Is.LessThan(1e-3f));
        }

        [UnityTest]
        public IEnumerator RecalibratingAMovedDeskKeepsTheLayout()
        {
            StartSession();
            yield return Calibrate(new Vector3(0f, 0.74f, 0.45f));
            GrabAndDrop mail = SpawnMail(session.Plane.ToWorld(new Vector3(0.2f, 0.15f, 0.1f)));
            mail.DropFromHere();
            yield return WaitUntilLanded(mail);

            // The user raised the desk by 6 cm and pushed it 30 cm to the right.
            yield return Calibrate(new Vector3(0.3f, 0.80f, 0.45f));
            Assert.AreEqual(0.80f, session.Plane.Height, 1e-4);
            Assert.AreEqual(0.80f, mail.transform.position.y, 1e-3, "The creature moved with the desk.");
            Assert.AreEqual(0.5f, mail.transform.position.x, 2e-2, "It kept its place relative to the desk.");

            RestartSession();
            yield return Await(session.RestoreAsync(), restored => Assert.IsTrue(restored));
            Assert.AreEqual(0.80f, session.Plane.Height, 1e-4, "Only the new desk is remembered.");
            Assert.AreEqual(1, StoredDeskCount());
        }

        [UnityTest]
        public IEnumerator AnotherRoomGetsANewDeskAndTheOldOneIsKept()
        {
            StartSession();
            yield return Calibrate(new Vector3(0f, 0.74f, 0.45f));

            // Elsewhere, the first desk's anchor cannot be found.
            anchorsInThisRoom.Clear();
            RestartSession();
            yield return Await(session.RestoreAsync(), restored => Assert.IsFalse(restored));
            yield return Calibrate(new Vector3(0f, 0.70f, 0.4f));
            Assert.AreEqual(2, StoredDeskCount(), "Both desks are remembered.");

            yield return Await(session.ForgetCurrentDeskAsync(), () => { });
            Assert.IsFalse(session.HasDesk);
            Assert.AreEqual(1, StoredDeskCount());
        }

        [UnityTest]
        public IEnumerator ACreatureLetGoOffTheDeskLandsBackInTheZone()
        {
            StartSession();
            yield return Calibrate(new Vector3(0f, 0.74f, 0.45f));
            GrabAndDrop mail = SpawnMail(session.Plane.ToWorld(new Vector3(1.5f, 0.3f, 0f)));
            mail.DropFromHere();
            yield return WaitUntilLanded(mail);
            Assert.IsTrue(session.Plane.Contains(mail.transform.position));
            Assert.AreEqual(0.74f, mail.transform.position.y, 1e-3);
        }

        void StartSession()
        {
            GameObject head = new GameObject("Head");
            head.transform.SetPositionAndRotation(new Vector3(0f, 1.15f, 0f), Quaternion.identity);
            sessionObject = new GameObject("Desk Session");
            sessionObject.SetActive(false);
            DeskCalibration calibration = sessionObject.AddComponent<DeskCalibration>();
            Inject(calibration, "head", head.transform);
            session = sessionObject.AddComponent<DeskSession>();
            Inject(session, "calibration", calibration);
            sessionObject.SetActive(true);
        }

        void RestartSession()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            StartSession();
        }

        IEnumerator Calibrate(Vector3 handOnDesk)
        {
            bool changed = false;
            void OnChanged() => changed = true;
            session.DeskChanged += OnChanged;
            session.Recalibrate();
            session.Calibration.Complete(DeskCalibration.MeasurementAt(handOnDesk));
            yield return new WaitUntil(() => changed);
            session.DeskChanged -= OnChanged;
            yield return new WaitUntil(() => session.Persisting.IsCompleted);
        }

        static GrabAndDrop SpawnMail(Vector3 position)
        {
            GameObject mail = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MailPrefab), position, Quaternion.identity);
            return mail.GetComponent<GrabAndDrop>();
        }

        static IEnumerator WaitUntilLanded(GrabAndDrop creature)
        {
            float limit = Time.time + 5f;
            yield return null;
            while (creature.IsFalling && Time.time < limit)
            {
                yield return null;
            }

            Assert.IsFalse(creature.IsFalling, "The creature never came to rest.");
        }

        // Read straight from the file: waiting on the async loader from the main thread would deadlock.
        int StoredDeskCount() => Regex.Matches(File.ReadAllText(storePath), "\"anchorId\"").Count;

        // Placements are saved in the background; wait until the file has caught up.
        IEnumerator WaitForStoreContaining(string text)
        {
            float limit = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < limit && !(File.Exists(storePath) && File.ReadAllText(storePath).Contains(text)))
            {
                yield return null;
            }

            StringAssert.Contains(text, File.ReadAllText(storePath));
        }

        static IEnumerator Await<T>(Task<T> task, Action<T> check)
        {
            yield return new WaitUntil(() => task.IsCompleted);
            Assert.IsNull(task.Exception);
            check(task.Result);
        }

        static IEnumerator Await(Task task, Action check)
        {
            yield return new WaitUntil(() => task.IsCompleted);
            Assert.IsNull(task.Exception);
            check();
        }

        static void Inject(Object target, string field, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
