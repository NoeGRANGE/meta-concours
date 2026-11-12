using UnityEngine;

namespace Deskmates.Desk
{
    /// <summary>
    /// The calibrated desk: a horizontal surface and the rectangle on it where creatures live (PRD 5.1).
    /// Its frame sits at the centre of the zone, on the surface, with +Z pointing away from the user.
    /// </summary>
    public readonly struct DeskPlane
    {
        public DeskPlane(Vector3 center, float yawDegrees, Vector2 halfSize)
        {
            Center = center;
            Yaw = yawDegrees;
            HalfSize = halfSize;
        }

        public Vector3 Center { get; }

        public float Yaw { get; }

        /// <summary>Half the width (X) and half the depth (Z) of the zone, in metres.</summary>
        public Vector2 HalfSize { get; }

        public float Height => Center.y;

        public Quaternion Rotation => Quaternion.Euler(0f, Yaw, 0f);

        public Vector3 ToLocal(Vector3 world) => Quaternion.Inverse(Rotation) * (world - Center);

        public Vector3 ToWorld(Vector3 local) => Center + Rotation * local;

        public bool Contains(Vector3 world)
        {
            Vector3 local = ToLocal(world);
            return Mathf.Abs(local.x) <= HalfSize.x && Mathf.Abs(local.z) <= HalfSize.y;
        }

        /// <summary>The point of the zone closest to <paramref name="world"/>, on the surface, kept away from the edges.</summary>
        public Vector3 ClampToZone(Vector3 world, float margin = 0f)
        {
            return ToWorld(ClampLocal(ToLocal(world), HalfSize, margin));
        }

        /// <summary>Clamps a desk-local point into a zone of the given half size and puts it on the surface.</summary>
        public static Vector3 ClampLocal(Vector3 local, Vector2 halfSize, float margin)
        {
            float marginX = Mathf.Min(margin, halfSize.x);
            float marginZ = Mathf.Min(margin, halfSize.y);
            local.x = Mathf.Clamp(local.x, -halfSize.x + marginX, halfSize.x - marginX);
            local.z = Mathf.Clamp(local.z, -halfSize.y + marginZ, halfSize.y - marginZ);
            local.y = 0f;
            return local;
        }
    }
}
