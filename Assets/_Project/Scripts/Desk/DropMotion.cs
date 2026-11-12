using System;

namespace Deskmates.Desk
{
    /// <summary>
    /// Falls onto a surface and bounces to rest, without the physics engine (PRD 5.1). Works on the height above
    /// the surface only, so the surface may move while the creature falls.
    /// </summary>
    public sealed class DropMotion
    {
        readonly float gravity;
        readonly float restitution;
        readonly float settleSpeed;
        float velocity;

        public DropMotion(float heightAboveSurface, float gravity = 9.81f, float restitution = 0.35f, float settleSpeed = 0.15f)
        {
            this.gravity = gravity;
            this.restitution = restitution;
            this.settleSpeed = settleSpeed;
            Height = Math.Max(0f, heightAboveSurface);
            Settled = Height <= 0f;
        }

        /// <summary>Raised on each impact with the impact speed, in metres per second.</summary>
        public event Action<float> Bounced;

        public float Height { get; private set; }

        public bool Settled { get; private set; }

        public void Step(float deltaTime)
        {
            if (Settled)
            {
                return;
            }

            velocity -= gravity * deltaTime;
            Height += velocity * deltaTime;
            if (Height > 0f)
            {
                return;
            }

            Height = 0f;
            float impact = -velocity;
            Bounced?.Invoke(impact);
            velocity = impact * restitution;
            if (velocity < settleSpeed)
            {
                velocity = 0f;
                Settled = true;
            }
        }
    }
}
