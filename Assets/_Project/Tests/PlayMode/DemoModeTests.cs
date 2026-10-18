#if UNITY_EDITOR
using System.Collections;
using System.Threading.Tasks;
using Deskmates.Core;
using Deskmates.Creatures;
using Deskmates.Data;
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
    /// The S2 criterion of PRD 11 without a headset: in the Main scene, the demo mode alone takes the mail creature
    /// from calm to panic, then to a sigh of relief.
    /// </summary>
    public class DemoModeTests
    {
        const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        const string RoomPath = "Packages/com.meta.xr.mrutilitykit/Core/Rooms/Prefabs/Office/Office00.prefab";

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator DemoTakesTheMailCreatureToPanicThenRelief()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                MainScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            Task<MRUK.LoadDeviceResult> load =
                MRUK.Instance.LoadSceneFromPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath));
            yield return new WaitUntil(() => load.IsCompleted);
            yield return null;

            Creature mail = Object.FindAnyObjectByType<AppBootstrap>().MailCreature;
            Assert.IsNotNull(mail, "The mail creature was not spawned.");
            DataHub hub = DataHub.Instance;
            Assert.IsNotNull(hub, "Main has no DataHub.");

            int reliefs = 0;
            mail.Relieved += () => reliefs++;
            bool panicked = false;

            // Ten times faster demo, four times faster game time: the three-minute demo plays in about 5 s.
            hub.Speed = 10f;
            Time.timeScale = 4f;
            float limit = Time.time + 25f;
            while (Time.time < limit && reliefs == 0)
            {
                panicked |= mail.Expression == CreatureExpression.Panic;
                yield return null;
            }

            Assert.IsTrue(panicked, "The demo never took the mail creature to panic.");
            Assert.AreEqual(1, reliefs, "The mail creature should sigh once the inbox is cleared.");
        }
    }
}
#endif
