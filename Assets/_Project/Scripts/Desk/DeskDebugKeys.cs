using Deskmates.Core;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deskmates.Desk
{
    /// <summary>
    /// Editor-only stand-ins for the hand on the desk, which a keyboard cannot fake (Game view focused):
    /// C completes the calibration on the scanned table in front of the user, or 40 cm below the eyes;
    /// R starts a recalibration; F forgets the desk. Does nothing in a build.
    /// </summary>
    public sealed class DeskDebugKeys : MonoBehaviour
    {
        const float ReachDistance = 0.45f;
        const float DropBelowEyes = 0.4f;

        [SerializeField] DeskSession session;

#if UNITY_EDITOR
        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                if (!session.IsCalibrating)
                {
                    session.Recalibrate();
                }

                Vector3 point = PointInFront();
                Debug.Log($"[Deskmates] Key C: calibrating with a hand at {point}.");
                session.Calibration.Complete(DeskCalibration.MeasurementAt(point));
            }
            else if (keyboard.rKey.wasPressedThisFrame)
            {
                Debug.Log("[Deskmates] Key R: waiting for a hand on the desk (or press C).");
                session.Recalibrate();
            }
            else if (keyboard.fKey.wasPressedThisFrame)
            {
                Debug.Log("[Deskmates] Key F: forgetting the desk.");
                _ = session.ForgetCurrentDeskAsync();
            }
        }

        Vector3 PointInFront()
        {
            Transform head = session.Calibration.Head;
            Pose headPose = new Pose(head.position, head.rotation);
            MRUKRoom room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
            if (DeskLocator.TryFindSpot(room, headPose, ReachDistance, 0.1f, out Pose spot))
            {
                return spot.position;
            }

            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            return head.position + forward * ReachDistance + Vector3.down * DropBelowEyes;
        }
#endif
    }
}
