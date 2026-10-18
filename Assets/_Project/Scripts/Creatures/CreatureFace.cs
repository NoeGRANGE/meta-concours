using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskmates.Creatures
{
    public enum CreatureExpression
    {
        Calm,
        Alert,
        Stressed,
        Panic,
    }

    /// <summary>
    /// Shows the face parts that belong to an expression and hides every other variant, and blinks the open eyes.
    /// The models ship with all variants as named children (PRD 10.1).
    /// </summary>
    public sealed class CreatureFace : MonoBehaviour
    {
        // Model nodes are named "<Creature>_<Part>" with an optional "_L" / "_R" side suffix.
        static readonly string[] FaceParts =
        {
            "EyeClosed", "EyeOpen", "EyeShine", "EyeWide", "EyePupil",
            "Brow", "Cheek",
            "MouthSmile", "MouthFlat", "MouthWorried", "MouthOpen",
            "Sweat",
        };

        // PRD 4.3, column "Parties visibles".
        static readonly Dictionary<CreatureExpression, string[]> VisibleParts = new()
        {
            { CreatureExpression.Calm, new[] { "EyeClosed", "Cheek", "MouthSmile" } },
            { CreatureExpression.Alert, new[] { "EyeOpen", "EyeShine", "MouthFlat" } },
            { CreatureExpression.Stressed, new[] { "EyeOpen", "Brow", "MouthWorried", "Sweat" } },
            { CreatureExpression.Panic, new[] { "EyeWide", "EyePupil", "Brow", "MouthOpen" } },
        };

        readonly Dictionary<string, List<GameObject>> parts = new();
        bool collected;
        CreatureExpression blinkingFor;
        float nextBlinkIn;
        float blinkTimeLeft;

        public CreatureExpression Expression { get; private set; }

        public void Apply(CreatureExpression expression)
        {
            CollectParts();
            Expression = expression;
            blinkTimeLeft = 0f;
            string[] visible = VisibleParts[expression];
            foreach (KeyValuePair<string, List<GameObject>> entry in parts)
            {
                SetVisible(entry.Key, Array.IndexOf(visible, entry.Key) >= 0);
            }
        }

        /// <summary>Blinks at random intervals while the eyes are open (alert and stressed).</summary>
        public void Tick(float deltaTime, CreatureConfig config)
        {
            if (Expression != CreatureExpression.Alert && Expression != CreatureExpression.Stressed)
            {
                return;
            }

            // Never blink right as the eyes open.
            if (blinkingFor != Expression)
            {
                blinkingFor = Expression;
                nextBlinkIn = UnityEngine.Random.Range(config.blinkInterval.x, config.blinkInterval.y);
            }

            if (blinkTimeLeft > 0f)
            {
                blinkTimeLeft -= deltaTime;
                if (blinkTimeLeft <= 0f)
                {
                    Apply(Expression);
                }

                return;
            }

            nextBlinkIn -= deltaTime;
            if (nextBlinkIn <= 0f)
            {
                nextBlinkIn = UnityEngine.Random.Range(config.blinkInterval.x, config.blinkInterval.y);
                blinkTimeLeft = config.blinkDuration;
                SetVisible("EyeOpen", false);
                SetVisible("EyeShine", false);
                SetVisible("EyeClosed", true);
            }
        }

        /// <summary>Returns "EyeClosed" for "Facteur_EyeClosed_L", or null when the name has no part.</summary>
        public static string PartKey(string nodeName)
        {
            int separator = nodeName.IndexOf('_');
            if (separator < 0)
            {
                return null;
            }

            string key = nodeName.Substring(separator + 1);
            if (key.EndsWith("_L", StringComparison.Ordinal) || key.EndsWith("_R", StringComparison.Ordinal))
            {
                key = key.Substring(0, key.Length - 2);
            }

            return key;
        }

        void SetVisible(string key, bool visible)
        {
            if (!parts.TryGetValue(key, out List<GameObject> list))
            {
                return;
            }

            foreach (GameObject part in list)
            {
                part.SetActive(visible);
            }
        }

        void CollectParts()
        {
            if (collected)
            {
                return;
            }

            collected = true;
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                string key = PartKey(child.name);
                if (key == null || Array.IndexOf(FaceParts, key) < 0)
                {
                    continue;
                }

                if (!parts.TryGetValue(key, out List<GameObject> list))
                {
                    list = new List<GameObject>();
                    parts.Add(key, list);
                }

                list.Add(child.gameObject);
            }
        }
    }
}
