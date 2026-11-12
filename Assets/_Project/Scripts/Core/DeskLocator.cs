using Deskmates.Desk;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Deskmates.Core
{
    /// <summary>
    /// Proposes where creatures sit: on the calibrated desk, or before any calibration on the scanned table.
    /// </summary>
    public static class DeskLocator
    {
        // Used when the room has no table: roughly desk height for a seated user.
        const float FloatingSpotDrop = 0.35f;

        // Default row of creatures on the calibrated desk: spacing between them, how far behind the hand they sit,
        // and the distance they keep from the zone edges, in metres.
        const float RowSpacing = 0.14f;
        const float RowDepth = 0.1f;
        const float RowMargin = 0.06f;

        /// <summary>
        /// Spot <paramref name="index"/> of <paramref name="count"/> in a row across the calibrated desk, a little
        /// behind where the hand rested, facing the user.
        /// </summary>
        public static Pose DefaultSpot(DeskPlane plane, Pose head, int index, int count)
        {
            float across = (index - (count - 1) * 0.5f) * RowSpacing;
            Vector3 position = plane.ClampToZone(plane.ToWorld(new Vector3(across, 0f, RowDepth)), RowMargin);
            return new Pose(position, FacingUser(position, head, HorizontalForward(head)));
        }

        /// <summary>
        /// Returns a pose on top of the table nearest to the point <paramref name="forwardDistance"/> metres
        /// in front of the user, facing the user. Returns false when the room has no table.
        /// </summary>
        public static bool TryFindSpot(MRUKRoom room, Pose head, float forwardDistance, float edgeMargin, out Pose spot)
        {
            spot = default;
            if (room == null)
            {
                return false;
            }

            Vector3 forward = HorizontalForward(head);
            Vector3 target = head.position + forward * forwardDistance;

            bool found = false;
            Vector3 best = default;
            float bestDistance = float.MaxValue;
            foreach (MRUKAnchor anchor in room.Anchors)
            {
                if (!anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE) ||
                    !TryClosestPointOnTop(anchor, target, edgeMargin, out Vector3 candidate))
                {
                    continue;
                }

                float distance = Vector3.ProjectOnPlane(candidate - target, Vector3.up).sqrMagnitude;
                if (distance < bestDistance)
                {
                    found = true;
                    best = candidate;
                    bestDistance = distance;
                }
            }

            if (found)
            {
                spot = new Pose(best, FacingUser(best, head, forward));
            }

            return found;
        }

        /// <summary>Fallback spot in mid-air, in front of and below the user's eyes.</summary>
        public static Pose FloatingSpot(Pose head, float forwardDistance)
        {
            Vector3 forward = HorizontalForward(head);
            Vector3 position = head.position + forward * forwardDistance + Vector3.down * FloatingSpotDrop;
            return new Pose(position, FacingUser(position, head, forward));
        }

        // MRUK anchors have their surface in the local XY plane with +Z as the normal;
        // a table's volume hangs below its top face.
        static bool TryClosestPointOnTop(MRUKAnchor anchor, Vector3 worldTarget, float edgeMargin, out Vector3 worldPoint)
        {
            Rect top;
            float topZ;
            if (anchor.PlaneRect.HasValue)
            {
                top = anchor.PlaneRect.Value;
                topZ = 0f;
            }
            else if (anchor.VolumeBounds.HasValue)
            {
                Bounds volume = anchor.VolumeBounds.Value;
                top = new Rect(volume.min.x, volume.min.y, volume.size.x, volume.size.y);
                topZ = volume.max.z;
            }
            else
            {
                worldPoint = default;
                return false;
            }

            // On a table narrower than twice the margin, settle for its centre line.
            float marginX = Mathf.Min(edgeMargin, top.width * 0.5f);
            float marginY = Mathf.Min(edgeMargin, top.height * 0.5f);
            Vector3 local = anchor.transform.InverseTransformPoint(worldTarget);
            local.x = Mathf.Clamp(local.x, top.xMin + marginX, top.xMax - marginX);
            local.y = Mathf.Clamp(local.y, top.yMin + marginY, top.yMax - marginY);
            local.z = topZ;
            worldPoint = anchor.transform.TransformPoint(local);
            return true;
        }

        static Vector3 HorizontalForward(Pose head)
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
            {
                // Looking straight up or down: the top of the head still points somewhere horizontal.
                forward = Vector3.ProjectOnPlane(head.up, Vector3.up);
            }

            return forward.sqrMagnitude < 1e-4f ? Vector3.forward : forward.normalized;
        }

        static Quaternion FacingUser(Vector3 position, Pose head, Vector3 userForward)
        {
            Vector3 toUser = Vector3.ProjectOnPlane(head.position - position, Vector3.up);
            Vector3 facing = toUser.sqrMagnitude < 1e-4f ? -userForward : toUser.normalized;
            return Quaternion.LookRotation(facing, Vector3.up);
        }
    }
}
