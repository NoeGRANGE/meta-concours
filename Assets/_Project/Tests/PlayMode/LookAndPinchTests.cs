#if UNITY_EDITOR
using System.Collections;
using System.Threading.Tasks;
using Deskmates.Core;
using Deskmates.Creatures;
using Deskmates.Data;
using Deskmates.Interaction;
using Deskmates.UI;
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
    /// PRD 5 in the Main scene: looking at the mail creature shows its peek bubble; pinching opens the panel;
    /// pinching every line clears the inbox and the creature sighs.
    /// </summary>
    public class LookAndPinchTests
    {
        const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        const string RoomPath = "Packages/com.meta.xr.mrutilitykit/Core/Rooms/Prefabs/Office/Office00.prefab";

        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator ClearingTheInboxFromThePanelCalmsTheMailCreature()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(MainScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Task<MRUK.LoadDeviceResult> load =
                MRUK.Instance.LoadSceneFromPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath));
            yield return new WaitUntil(() => load.IsCompleted);
            yield return null;

            Creature mail = Object.FindAnyObjectByType<AppBootstrap>().MailCreature;
            CreatureInfo info = mail.GetComponent<CreatureInfo>();
            Assert.IsNotNull(info, "The mail creature cannot be looked at.");

            // Let the demo pile up important mails, with the user's own actions only.
            DataHub.Instance.Speed = 10f;
            Time.timeScale = 4f;
            yield return new WaitUntil(() => mail.Expression == CreatureExpression.Panic);
            Time.timeScale = 1f;
            DataHub.Instance.Speed = 0f;

            info.OnGazeDwell();
            yield return null;
            Assert.IsTrue(info.transform.Find("Peek").gameObject.activeSelf, "Looking shows the peek bubble.");
            yield return null;
            Assert.IsTrue(info.GetComponent<GazeOutline>().IsShown, "Looking outlines the creature.");

            int reliefs = 0;
            mail.Relieved += () => reliefs++;
            info.OnPinch();
            DetailPanel panel = DetailPanel.Instance;
            Assert.AreSame(info, panel.Creature, "Pinching opens the panel.");
            yield return null;
            Assert.AreEqual(3, panel.Shown.Count, "The three most urgent mails are listed.");

            for (int guard = 0; guard < 10 && panel.Shown.Count > 0; guard++)
            {
                panel.Resolve(0);
                yield return null;
            }

            IDataSource source = DataHub.Instance.GetSource(SourceIds.Mail);
            Assert.IsEmpty(source.Current.Items, "Every mail was marked read.");
            float limit = Time.time + 5f;
            yield return new WaitUntil(() => reliefs > 0 || Time.time > limit);
            Assert.AreEqual(1, reliefs, "The mail creature sighs once the inbox is clear.");

            panel.Close();
            info.OnPinch();
            Assert.AreSame(info, panel.Creature, "Pinching reopens the panel.");
            info.OnPinch();
            Assert.IsNull(panel.Creature, "Pinching the creature again closes its panel.");
        }

    }
}
#endif
