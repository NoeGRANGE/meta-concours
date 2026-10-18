using Deskmates.Creatures;
using Deskmates.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deskmates.Sandbox
{
    /// <summary>
    /// Drives the creatures of the Sandbox_Creature scene. With Use Demo on (key D), the demo script plays.
    /// Otherwise they follow the sliders: with Same Stress For All on, the Stress slider or keys 1 to 4 (calm, alert,
    /// stressed, panic) set every creature; with it off, each creature follows its own slider in Individual Stress.
    /// Space plays the relief on all of them.
    /// </summary>
    public sealed class StressSandbox : MonoBehaviour
    {
        static readonly Key[] StateKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
        static readonly float[] StateStress = { 0f, 0.3f, 0.6f, 1f };

        [SerializeField] Creature[] creatures;

        [Tooltip("Plays the demo script instead of the sliders. Key D toggles it.")]
        [SerializeField] bool useDemo;

        [SerializeField] DataHub demo;
        [SerializeField] bool sameStressForAll = true;
        [Range(0f, 1f)] [SerializeField] float stress;

        [Tooltip("One slider per creature, in the order of Creatures. Used when Same Stress For All is off.")]
        [Range(0f, 1f)] [SerializeField] float[] individualStress = new float[4];

        void OnValidate()
        {
            if (creatures != null && individualStress.Length != creatures.Length)
            {
                System.Array.Resize(ref individualStress, creatures.Length);
            }
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                for (int i = 0; i < StateKeys.Length; i++)
                {
                    if (keyboard[StateKeys[i]].wasPressedThisFrame)
                    {
                        stress = StateStress[i];
                        sameStressForAll = true;
                        useDemo = false;
                    }
                }

                if (keyboard.dKey.wasPressedThisFrame)
                {
                    useDemo = !useDemo;
                }

                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    foreach (Creature creature in creatures)
                    {
                        creature.PlayRelief();
                    }
                }
            }

            if (demo != null)
            {
                demo.enabled = useDemo;
            }

            if (useDemo)
            {
                return;
            }

            for (int i = 0; i < creatures.Length; i++)
            {
                bool hasOwnSlider = i < individualStress.Length;
                creatures[i].SetTargetStress(sameStressForAll || !hasOwnSlider ? stress : individualStress[i]);
            }
        }
    }
}
