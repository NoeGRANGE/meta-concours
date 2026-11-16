using System.Collections.Generic;
using Deskmates.Creatures;
using Deskmates.Desk;
using Deskmates.Interaction;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Deskmates.Core
{
    /// <summary>
    /// Entry point of the Main scene. Restores the known desk with its creatures, or asks for a calibration; until
    /// then the creatures wait on the scanned table so something is visible right away.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        // A creature placed for the first time on a desk drops from this height above it, in metres.
        const float FirstDropHeight = 0.2f;

        // Zone around the spot found on a scanned table, or in front of the user, before any calibration.
        // shortcut: a fixed zone rather than the table's real edges, take them from MRUK if creatures fall off its sides.
        static readonly Vector2 FallbackHalfSize = new Vector2(0.3f, 0.15f);

        [SerializeField] Transform head;

        [Tooltip("The family, mail creature first.")]
        [SerializeField] Creature[] creaturePrefabs;

        [SerializeField] DeskSession desk;

        [Tooltip("How far in front of the user the spot on a scanned table is, in metres.")]
        [SerializeField] float forwardDistance = 0.55f;

        [Tooltip("Minimum distance kept from the scanned table edges, in metres.")]
        [SerializeField] float edgeMargin = 0.08f;

        readonly List<Creature> creatures = new();

        public IReadOnlyList<Creature> Creatures => creatures;

        public Creature MailCreature => creatures.Count > 0 ? creatures[0] : null;

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

        // Before any desk exists: line the creatures up on the scanned table, or floating in front of the user.
        // A desk restored afterwards moves them to their saved places.
        void OnRoomLoaded()
        {
            if (desk.HasDesk)
            {
                return;
            }

            Pose headPose = HeadPose();
            MRUKRoom room = MRUK.Instance.GetCurrentRoom();
            if (DeskLocator.TryFindSpot(room, headPose, forwardDistance, edgeMargin, out Pose middle))
            {
                // Released creatures land on the table until the desk is calibrated.
                desk.FallbackPlane = new DeskPlane(middle.position, middle.rotation.eulerAngles.y, FallbackHalfSize);
            }
            else
            {
                Debug.LogWarning("[Deskmates] No desk found in this room; placing the creatures in front of the user.");
            }

            // A row across, each creature at the point of the table nearest to its place in the row, so a small
            // table keeps them all on top.
            SpawnAll();
            Vector3 right = Vector3.ProjectOnPlane(head.right, Vector3.up).normalized;
            for (int i = 0; i < creatures.Count; i++)
            {
                float across = (i - (creatures.Count - 1) * 0.5f) * DeskLocator.RowSpacing;
                Pose shifted = new Pose(headPose.position + right * across, headPose.rotation);
                if (!DeskLocator.TryFindSpot(room, shifted, forwardDistance, edgeMargin, out Pose place))
                {
                    place = DeskLocator.FloatingSpot(shifted, forwardDistance);
                }

                // Each one turns to the user, not straight ahead of its place in the row.
                Vector3 toUser = Vector3.ProjectOnPlane(headPose.position - place.position, Vector3.up);
                Quaternion facing = toUser.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(toUser, Vector3.up) : place.rotation;
                creatures[i].transform.SetPositionAndRotation(place.position, facing);
            }
        }

        void OnDeskChanged()
        {
            if (!desk.HasDesk)
            {
                return;
            }

            SpawnAll();
            for (int i = 0; i < creatures.Count; i++)
            {
                Creature creature = creatures[i];
                GrabAndDrop grab = creature.GetComponent<GrabAndDrop>();
                if (desk.TryGetPlacement(grab.CreatureId, out Pose saved))
                {
                    creature.transform.SetPositionAndRotation(saved.position, saved.rotation);
                    desk.SavePlacement(grab.CreatureId, creature.transform);
                    continue;
                }

                // First time on this desk: drop it into its default spot.
                Pose spot = DeskLocator.DefaultSpot(desk.Plane, HeadPose(), i, creatures.Count);
                creature.transform.SetPositionAndRotation(spot.position + Vector3.up * FirstDropHeight, spot.rotation);
                grab.DropFromHere();
            }
        }

        void SpawnAll()
        {
            if (creatures.Count > 0)
            {
                return;
            }

            foreach (Creature prefab in creaturePrefabs)
            {
                creatures.Add(Instantiate(prefab));
            }
        }

        Pose HeadPose() => new Pose(head.position, head.rotation);
    }
}
