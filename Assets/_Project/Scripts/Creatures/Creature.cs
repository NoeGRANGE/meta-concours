using System;
using UnityEngine;

namespace Deskmates.Creatures
{
    /// <summary>
    /// Root component of a desk creature. For now it only puts the model in its calm state at startup;
    /// stress, motion and VFX are layered on top of it.
    /// </summary>
    [RequireComponent(typeof(CreatureFace))]
    public sealed class Creature : MonoBehaviour
    {
        // Accessories that only appear once the creature is stressed (PRD 10.1).
        static readonly string[] StressOnlyParts =
        {
            "FlameMain", "FlameLeft", "FlameRight", "FlameCore",
            "PostitBlue", "PostitPink",
        };

        CreatureFace face;

        void Awake()
        {
            face = GetComponent<CreatureFace>();
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (Array.IndexOf(StressOnlyParts, CreatureFace.PartKey(child.name)) >= 0)
                {
                    child.gameObject.SetActive(false);
                }
            }

            face.Apply(CreatureExpression.Calm);
        }
    }
}
