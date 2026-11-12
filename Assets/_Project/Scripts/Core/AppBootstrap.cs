using Deskmates.Creatures;
using Deskmates.Desk;
using Deskmates.Interaction;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Deskmates.Core
{
    /// <summary>
    /// Entry point of the Main scene. Restores the known desk with its creatures, or asks for a calibration; until
    /// then the creature waits on the scanned table so something is visible right away.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        // A creature placed for the first time on a desk drops from this height above it, in metres.
        const float FirstDropHeight = 0.2f;

        [SerializeField] Transform head;
        [SerializeField] Creature mailCreaturePrefab;
        [SerializeField] DeskSession desk;

        [Tooltip("How far in front of the user the spot on a scanned table is, in metres.")]
        [SerializeField] float forwardDistance = 0.55f;

        [Tooltip("Minimum distance kept from the scanned table edges, in metres.")]
        [SerializeField] float edgeMargin = 0.08f;

        public Creature MailCreature { get; private set; }

        async void Start()
        {
            desk.DeskChanged += OnDeskChanged;
            MRUK.Instance.RegisterSceneLoadedCallback(OnRoomLoaded);
            if (!await desk.RestoreAsync())
            {
                desk.Recalibrate();
            }
        }

        void OnDestroy()
        {
            if (desk != null)
            {
                desk.DeskChanged -= OnDeskChanged;
            }
        }

        // Before any desk exists: show the creature on the scanned table, or floating in front of the user.
        // A desk restored afterwards moves it to its saved place.
        void OnRoomLoaded()
        {
            if (desk.HasDesk)
            {
                return;
            }

            Pose headPose = HeadPose();
            MRUKRoom room = MRUK.Instance.GetCurrentRoom();
            if (!DeskLocator.TryFindSpot(room, headPose, forwardDistance, edgeMargin, out Pose spot))
            {
                Debug.LogWarning("[Deskmates] No desk found in this room; placing the creature in front of the user.");
                spot = DeskLocator.FloatingSpot(headPose, forwardDistance);
            }

            Spawn().transform.SetPositionAndRotation(spot.position, spot.rotation);
        }

        void OnDeskChanged()
        {
            if (!desk.HasDesk)
            {
                return;
            }

            Creature creature = Spawn();
            GrabAndDrop grab = creature.GetComponent<GrabAndDrop>();
            if (desk.TryGetPlacement(grab.CreatureId, out Pose saved))
            {
                creature.transform.SetPositionAndRotation(saved.position, saved.rotation);
                desk.SavePlacement(grab.CreatureId, creature.transform);
                return;
            }

            // First time on this desk: drop it into its default spot.
            Pose spot = DeskLocator.DefaultSpot(desk.Plane, HeadPose(), 0, 1);
            creature.transform.SetPositionAndRotation(spot.position + Vector3.up * FirstDropHeight, spot.rotation);
            grab.DropFromHere();
        }

        Creature Spawn()
        {
            if (MailCreature == null)
            {
                MailCreature = Instantiate(mailCreaturePrefab);
            }

            return MailCreature;
        }

        Pose HeadPose() => new Pose(head.position, head.rotation);
    }
}
