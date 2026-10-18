using Deskmates.Creatures;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Deskmates.Core
{
    /// <summary>
    /// Entry point of the Main scene: waits for the room data, then puts the creatures on the desk.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] Creature mailCreaturePrefab;

        [Tooltip("How far in front of the user the default spot is, in metres.")]
        [SerializeField] float forwardDistance = 0.55f;

        [Tooltip("Minimum distance kept from the desk edges, in metres.")]
        [SerializeField] float edgeMargin = 0.08f;

        bool placedOnDesk;

        public Creature MailCreature { get; private set; }

        void Start()
        {
            MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
        }

        void OnSceneLoaded()
        {
            // MRUK raises this again whenever the scene is reloaded, e.g. once the user has finished Space Setup.
            if (placedOnDesk)
            {
                return;
            }

            Pose headPose = new Pose(head.position, head.rotation);
            MRUKRoom room = MRUK.Instance.GetCurrentRoom();
            placedOnDesk = DeskLocator.TryFindSpot(room, headPose, forwardDistance, edgeMargin, out Pose spot);
            if (!placedOnDesk)
            {
                Debug.LogWarning("[Deskmates] No desk found in this room; placing the creature in front of the user.");
                spot = DeskLocator.FloatingSpot(headPose, forwardDistance);
            }

            if (MailCreature == null)
            {
                MailCreature = Instantiate(mailCreaturePrefab, spot.position, spot.rotation);
            }
            else
            {
                MailCreature.transform.SetPositionAndRotation(spot.position, spot.rotation);
            }
        }
    }
}
