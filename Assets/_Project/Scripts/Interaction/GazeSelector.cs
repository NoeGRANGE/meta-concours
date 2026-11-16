using Deskmates.Desk;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace Deskmates.Interaction
{
    /// <summary>Something the user can look at and pinch (PRD 5).</summary>
    public interface IGazeTarget
    {
        /// <summary>Looked at long enough to count as intent.</summary>
        void OnGazeDwell();

        void OnGazeExit();

        /// <summary>Pinched while being looked at.</summary>
        void OnPinch();
    }

    /// <summary>
    /// Look to target, pinch to select (PRD 5). Targets with the head direction, which every device has: Quest 3
    /// and 3S have no eye tracking, and it is the PRD's fallback.
    /// shortcut: head gaze on Meta VR Glasses too, switch to their eye gaze once one is available to test.
    /// In the editor, P stands in for a pinch (Game view focused).
    /// </summary>
    public sealed class GazeSelector : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] float dwellSeconds = 0.4f;
        [SerializeField] float maxDistance = 2f;

        [Tooltip("Radius of the gaze ray, in metres: precise enough to tell the panel's lines apart.")]
        [SerializeField] float radius = 0.03f;

        [Tooltip("Wider radius tried when the precise ray hits nothing: the head points only roughly where the eyes look.")]
        [SerializeField] float wideRadius = 0.1f;

        readonly IsdkHandSampler hands = new IsdkHandSampler();
        IGazeTarget hovered;
        float hoveredFor;
        bool dwelled;
        bool wasPinching;

        public IGazeTarget Hovered => dwelled ? hovered : null;

        void Update()
        {
            IGazeTarget target = null;
            if (Physics.SphereCast(head.position, radius, head.forward, out RaycastHit hit, maxDistance)
                || Physics.SphereCast(head.position, wideRadius, head.forward, out hit, maxDistance))
            {
                target = hit.collider.GetComponentInParent<IGazeTarget>();
            }

            // A destroyed target still compares unequal to null through the interface.
            if (hovered is Object destroyed && destroyed == null)
            {
                hovered = null;
                dwelled = false;
            }

            if (target != hovered)
            {
                if (dwelled)
                {
                    hovered.OnGazeExit();
                }

                hovered = target;
                hoveredFor = 0f;
                dwelled = false;
            }

            bool pinching = hands.IsPinching();
#if UNITY_EDITOR
            pinching |= Keyboard.current != null && Keyboard.current.pKey.isPressed;
#endif
            bool pinchedNow = pinching && !wasPinching;
            wasPinching = pinching;

            if (hovered == null)
            {
                return;
            }

            hoveredFor += Time.deltaTime;
            if (!dwelled && hoveredFor >= dwellSeconds)
            {
                dwelled = true;
                hovered.OnGazeDwell();
            }

            if (dwelled && pinchedNow)
            {
                hovered.OnPinch();
            }
        }
    }
}
