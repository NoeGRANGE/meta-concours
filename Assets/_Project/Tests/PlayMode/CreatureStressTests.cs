#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Deskmates.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deskmates.Tests
{
    /// <summary>
    /// Drives real creature prefabs through the stress states and checks what is on screen (PRD 4.3–4.5).
    /// </summary>
    public class CreatureStressTests
    {
        const string PrefabsFolder = "Assets/_Project/Prefabs/Creatures";
        static readonly int BaseColorId = Shader.PropertyToID("baseColorFactor");

        static readonly string[] Prefabs = { "Creature_Mail", "Creature_Calendar", "Creature_Tasks", "Creature_Messages" };

        Creature creature;
        int reliefs;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (creature != null)
            {
                Object.Destroy(creature.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator PanicThenCalmEndsWithOneRelief([ValueSource(nameof(Prefabs))] string prefabName)
        {
            yield return Spawn(prefabName);
            Vector3 calmScale = Visual().localScale;

            creature.SetTargetStress(1f);
            yield return Wait(3f);
            Assert.AreEqual(CreatureExpression.Panic, creature.Expression);
            AssertVisible(true, "EyeWide", "EyePupil", "Brow", "MouthOpen");
            AssertVisible(false, "EyeClosed", "Cheek", "MouthSmile", "EyeOpen");
            Assert.AreEqual(1.4f, Visual().localScale.x / calmScale.x, 0.08f, "Panic should make the creature 40% bigger.");
            Color panicColor = BodyColor();
            Assert.Greater(panicColor.r - panicColor.g, 0.3f, "The body should turn red in panic.");

            creature.SetTargetStress(0f);
            yield return Wait(3f);
            Assert.AreEqual(CreatureExpression.Calm, creature.Expression);
            AssertVisible(true, "EyeClosed", "Cheek", "MouthSmile");
            AssertVisible(false, "EyeWide", "EyePupil", "Brow", "MouthOpen", "Sweat");
            Assert.AreEqual(1f, Visual().localScale.x / calmScale.x, 0.05f);
            Assert.AreEqual(1, reliefs, "The relief should play exactly once.");
        }

        [UnityTest]
        public IEnumerator MailFlamesGrowWithStress()
        {
            yield return Spawn("Creature_Mail");
            AssertVisible(false, "FlameMain", "FlameLeft", "FlameRight", "FlameCore");

            creature.SetTargetStress(0.3f);
            yield return Wait(3f);
            AssertVisible(true, "FlameMain");
            AssertVisible(false, "FlameLeft", "FlameRight", "FlameCore");

            creature.SetTargetStress(1f);
            yield return Wait(3f);
            AssertVisible(true, "FlameMain", "FlameLeft", "FlameRight", "FlameCore");
        }

        [UnityTest]
        public IEnumerator TaskPostitsPileUpWithStress()
        {
            yield return Spawn("Creature_Tasks");
            AssertVisible(false, "PostitBlue", "PostitPink");

            creature.SetTargetStress(0.6f);
            yield return Wait(3f);
            AssertVisible(true, "PostitBlue");
            AssertVisible(false, "PostitPink");

            creature.SetTargetStress(1f);
            yield return Wait(3f);
            AssertVisible(true, "PostitBlue", "PostitPink");
        }

        IEnumerator Spawn(string prefabName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/{prefabName}.prefab");
            Assert.IsNotNull(prefab, $"{prefabName} not found.");
            creature = Object.Instantiate(prefab).GetComponent<Creature>();
            reliefs = 0;
            creature.Relieved += () => reliefs++;
            yield return null;
        }

        // Runs game time faster so each wait only takes a fraction of a second.
        static IEnumerator Wait(float gameSeconds)
        {
            Time.timeScale = 4f;
            yield return new WaitForSeconds(gameSeconds);
            Time.timeScale = 1f;
        }

        Transform Visual() => creature.transform.GetChild(0);

        Color BodyColor()
        {
            Renderer body = Parts("Body")[0].GetComponent<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            body.GetPropertyBlock(block);
            return block.GetColor(BaseColorId);
        }

        void AssertVisible(bool visible, params string[] partKeys)
        {
            foreach (string key in partKeys)
            {
                List<Transform> parts = Parts(key);
                Assert.IsNotEmpty(parts, $"{creature.name} has no {key} part.");
                foreach (Transform part in parts)
                {
                    Assert.AreEqual(visible, part.gameObject.activeInHierarchy,
                        $"{part.name} should be {(visible ? "visible" : "hidden")} at {creature.Expression}.");
                }
            }
        }

        List<Transform> Parts(string key)
        {
            List<Transform> parts = new List<Transform>();
            foreach (Transform part in creature.GetComponentsInChildren<Transform>(true))
            {
                if (CreatureFace.PartKey(part.name) == key)
                {
                    parts.Add(part);
                }
            }

            return parts;
        }
    }
}
#endif
