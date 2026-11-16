using System;
using Deskmates.Core;
using Deskmates.Creatures;
using Deskmates.Desk;
using Oculus.Interaction;
using UnityEngine;

namespace Deskmates.Interaction
{
    /// <summary>
    /// Lets a creature be picked up by hand and, once released, fall onto the desk, bounce and stand back up facing
    /// the user (PRD 5.1). Where it lands is remembered by the desk session (PRD 5.2).
    /// </summary>
    [RequireComponent(typeof(Creature))]
    public sealed class GrabAndDrop : MonoBehaviour
    {
        [SerializeField] Grabbable grabbable;

        [Tooltip("Identifies this creature in the saved desk layout.")]
        [SerializeField] string creatureId;

        [Tooltip("Fraction of real gravity: a little lower reads better at this small scale.")]
        [SerializeField] float gravityScale = 0.7f;

        [SerializeField] float restitution = 0.35f;

        [Tooltip("Distance kept from the edges of the desk zone, in metres.")]
        [SerializeField] float edgeMargin = 0.06f;

        [Tooltip("How fast the creature turns back to face the user while it falls, in degrees per second.")]
        [SerializeField] float turnSpeed = 540f;

        CreatureMotion motion;
        DropMotion drop;
        Vector3 landing;

        /// <summary>Raised once the creature has come to rest on the desk.</summary>
        public event Action Landed;

        public string CreatureId => creatureId;

        public bool IsHeld { get; private set; }

        public bool IsFalling => drop != null;

        void Awake()
        {
            motion = GetComponent<CreatureMotion>();
        }

        void OnEnable()
        {
            if (grabbable != null)
            {
                grabbable.WhenPointerEventRaised += OnPointerEvent;
            }
        }

        void OnDisable()
        {
            if (grabbable != null)
            {
                grabbable.WhenPointerEventRaised -= OnPointerEvent;
            }
        }

        /// <summary>Lets the creature fall onto the desk from where it is. Used on release and for a first placement.</summary>
        public void DropFromHere()
        {
            DeskSession desk = DeskSession.Instance;
            if (desk == null || !desk.TryGetLandingPlane(out DeskPlane plane))
            {
                // No surface known yet: it stays where it was let go.
                return;
            }

            landing = plane.ClampToZone(transform.position, edgeMargin);
            drop = new DropMotion(transform.position.y - plane.Height, 9.81f * gravityScale, restitution);
            drop.Bounced += motion.PlayLanding;

            // The landing sound already holds the small second bounce, so it plays on the first impact only.
            bool landed = false;
            drop.Bounced += _ =>
            {
                if (!landed)
                {
                    landed = true;
                    SoundBank.Play(Sound.Landing, transform.position);
                }
            };
        }

        void OnPointerEvent(PointerEvent pointerEvent)
        {
            switch (pointerEvent.Type)
            {
                case PointerEventType.Select:
                    IsHeld = true;
                    drop = null;
                    break;
                case PointerEventType.Unselect when grabbable.SelectingPointsCount == 0:
                    IsHeld = false;
                    DropFromHere();
                    break;
            }
        }

        void Update()
        {
            if (drop == null || IsHeld)
            {
                return;
            }

            DeskSession desk = DeskSession.Instance;
            if (desk == null || !desk.TryGetLandingPlane(out DeskPlane plane))
            {
                drop = null;
                return;
            }

            drop.Step(Time.deltaTime);

            // Glide back over the zone while falling, and turn upright to face the user.
            float follow = 1f - Mathf.Exp(-10f * Time.deltaTime);
            Vector3 position = transform.position;
            position.x = Mathf.Lerp(position.x, landing.x, follow);
            position.z = Mathf.Lerp(position.z, landing.z, follow);
            position.y = plane.Height + drop.Height;
            transform.SetPositionAndRotation(position,
                Quaternion.RotateTowards(transform.rotation, FacingUser(position), turnSpeed * Time.deltaTime));

            Vector3 offset = position - landing;
            offset.y = 0f;
            if (drop.Settled && offset.sqrMagnitude < 1e-6f)
            {
                transform.SetPositionAndRotation(new Vector3(landing.x, plane.Height, landing.z), FacingUser(position));
                drop = null;
                desk.SavePlacement(creatureId, transform);
                Landed?.Invoke();
            }
        }

        static Quaternion FacingUser(Vector3 position)
        {
            Camera eyes = Camera.main;
            Vector3 toUser = eyes != null ? Vector3.ProjectOnPlane(eyes.transform.position - position, Vector3.up) : Vector3.back;
            return toUser.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(toUser, Vector3.up) : Quaternion.identity;
        }
    }
}
