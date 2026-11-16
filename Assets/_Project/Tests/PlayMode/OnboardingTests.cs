#if UNITY_EDITOR
using System.Collections;
using System.Threading.Tasks;
using Deskmates.Core;
using Deskmates.Creatures;
using Deskmates.Data;
using Deskmates.Desk;
using Deskmates.UI;
using Meta.XR.MRUtilityKit;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Deskmates.Tests
{
    /// <summary>Every other PlayMode test starts with the walkthrough already done.</summary>
    [SetUpFixture]
    public class SkipOnboardingByDefault
    {
        [OneTimeSetUp]
        public void SetUp() => Onboarding.DoneOverride = true;

        [OneTimeTearDown]
        public void TearDown() => Onboarding.DoneOverride = null;
    }

    /// <summary>
    /// PRD 6.1 in the Main scene: nothing until the hand is on the desk, then the mail creature alone, and the whole
    /// family once it has sighed.
    /// </summary>
    public class OnboardingTests
    {
        const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        const string RoomPath = "Packages/com.meta.xr.mrutilitykit/Core/Rooms/Prefabs/Office/Office00.prefab";

        string storePath;

        [SetUp]
        public void SetUp()
        {
            Onboarding.DoneOverride = false;
            storePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"desks-{System.Guid.NewGuid()}.json");
            DeskSession.ServicesOverride = () => (new InMemoryAnchorService(isPersistent: false), new DeskStore(storePath));
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Onboarding.DoneOverride = true;
            DeskSession.ServicesOverride = null;
            System.IO.File.Delete(storePath);
        }

        [UnityTest]
        public IEnumerator FirstRunIntroducesTheMailCreatureThenTheFamily()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(MainScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Task<MRUK.LoadDeviceResult> load =
                MRUK.Instance.LoadSceneFromPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath));
            yield return new WaitUntil(() => load.IsCompleted);
            yield return null;

            AppBootstrap app = Object.FindAnyObjectByType<AppBootstrap>();
            Onboarding onboarding = Object.FindAnyObjectByType<Onboarding>();
            Text card = onboarding.GetComponentInChildren<Text>(true);
            Assert.IsTrue(onboarding.IsRunning);
            Assert.AreEqual(4, app.Creatures.Count);
            foreach (Creature creature in app.Creatures)
            {
                Assert.IsFalse(creature.gameObject.activeSelf, "No creature before the desk is known.");
            }

            Assert.AreEqual(0f, DataHub.Instance.Speed, "The demo waits for the desk.");
            StringAssert.Contains("hand flat", card.text);

            DeskSession desk = DeskSession.Instance;
            desk.Calibration.Complete(DeskCalibration.MeasurementAt(app.MailCreature.transform.position));
            yield return null;
            yield return null;
            Assert.IsTrue(app.MailCreature.gameObject.activeSelf, "The mail creature drops in first.");
            for (int i = 1; i < app.Creatures.Count; i++)
            {
                Assert.IsFalse(app.Creatures[i].gameObject.activeSelf, "The others wait for the first sigh.");
            }

            Assert.Greater(DataHub.Instance.Speed, 0f, "The demo starts once the desk is known.");
            StringAssert.Contains("Mail Fretling", card.text);

            app.MailCreature.PlayRelief();
            yield return null;
            foreach (Creature creature in app.Creatures)
            {
                Assert.IsTrue(creature.gameObject.activeSelf, "After the sigh the whole family is on the desk.");
            }

            StringAssert.Contains("Meet your Fretlings", card.text);
        }

        [UnityTest]
        public IEnumerator ClearingTheMailCreaturesInboxBringsTheFamilyEvenWithoutASigh()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(MainScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Task<MRUK.LoadDeviceResult> load =
                MRUK.Instance.LoadSceneFromPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath));
            yield return new WaitUntil(() => load.IsCompleted);
            yield return null;

            AppBootstrap app = Object.FindAnyObjectByType<AppBootstrap>();
            DeskSession.Instance.Calibration.Complete(DeskCalibration.MeasurementAt(app.MailCreature.transform.position));
            yield return null;

            // Past the introduction, the user pinches the creature and handles every mail, worried or not.
            Time.timeScale = 10f;
            yield return new WaitForSeconds(6f);
            Time.timeScale = 1f;
            Assert.IsFalse(app.Creatures[1].gameObject.activeSelf, "The family is still waiting.");

            app.MailCreature.GetComponent<CreatureInfo>().OnPinch();
            DetailPanel panel = DetailPanel.Instance;
            for (int guard = 0; guard < 10 && panel.Shown.Count > 0; guard++)
            {
                panel.Resolve(0);
                yield return null;
            }

            yield return null;
            yield return null;
            foreach (Creature creature in app.Creatures)
            {
                Assert.IsTrue(creature.gameObject.activeSelf, "An empty inbox brings the whole family.");
            }
        }
    }
}
#endif
