using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Deskmates.Desk
{
    /// <summary>
    /// The desk service of PRD 5.2. Restores a known desk at launch, runs calibrations whenever the rest of the app
    /// asks (onboarding, menu), replaces a desk that moved or adds a new one, and remembers where each creature sits.
    /// Creatures placed on the desk become children of its anchored root, so they follow the anchor.
    /// </summary>
    public sealed class DeskSession : MonoBehaviour
    {
        // Creatures moved onto a smaller zone after a recalibration stay this far from its edges.
        const float RelayoutMargin = 0.06f;

        [SerializeField] DeskCalibration calibration;

        IAnchorService anchors;
        DeskStore store;
        DeskRecord current;
        Transform root;

        /// <summary>Test hook: supplies the anchor service and store instead of the defaults, before Awake.</summary>
        public static Func<(IAnchorService anchors, DeskStore store)> ServicesOverride;

        public static DeskSession Instance { get; private set; }

        /// <summary>Raised when a desk is restored, calibrated, replaced or forgotten.</summary>
        public event Action DeskChanged;

        public bool HasDesk => root != null;

        public bool IsCalibrating => calibration.IsCalibrating;

        public bool IsRestoring { get; private set; }

        /// <summary>
        /// The anchoring and saving that follow a change. The desk is usable before it completes; await it to be sure
        /// the change survives a restart.
        /// </summary>
        public Task Persisting { get; private set; } = Task.CompletedTask;

        /// <summary>The anchored desk frame. Creatures on the desk are its children.</summary>
        public Transform Root => root;

        public DeskPlane Plane => new DeskPlane(root.position, root.eulerAngles.y, current.halfSize);

        /// <summary>Where creatures land until a desk is calibrated: the scanned table, if there is one (PRD 5.1).</summary>
        public DeskPlane? FallbackPlane { get; set; }

        /// <summary>The surface a released creature falls onto: the desk, or the fallback before any calibration.</summary>
        public bool TryGetLandingPlane(out DeskPlane plane)
        {
            plane = HasDesk ? Plane : FallbackPlane.GetValueOrDefault();
            return HasDesk || FallbackPlane.HasValue;
        }

        public DeskCalibration Calibration => calibration;

        void Awake()
        {
            Instance = this;
            if (ServicesOverride != null)
            {
                (anchors, store) = ServicesOverride();
            }
            else
            {
                // On a headset, real anchors always. isHmdPresent can still be false this early on the device, which
                // used to fall back to session-only anchors and never save the desk.
                bool realAnchors = !Application.isEditor || OVRManager.isHmdPresent;
                anchors = realAnchors ? new OvrAnchorService() : new InMemoryAnchorService();
                store = new DeskStore(DeskStore.DefaultPath);
            }

            calibration.Completed += OnCalibrated;
        }

        void OnDestroy()
        {
            calibration.Completed -= OnCalibrated;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Looks for the known desks in this space and restores the most recently used one found.</summary>
        public async Task<bool> RestoreAsync()
        {
            IsRestoring = true;
            try
            {
                await store.LoadAsync();
                foreach (DeskRecord record in store.Desks.OrderByDescending(d => d.lastUsedUtcTicks).ToList())
                {
                    GameObject anchored = await anchors.LoadAsync(record.AnchorId);
                    if (anchored == null)
                    {
                        continue;
                    }

                    current = record;
                    root = anchored.transform;
                    record.lastUsedUtcTicks = DateTime.UtcNow.Ticks;
                    Debug.Log($"[Deskmates] Desk restored at {Plane.Height:F3} m with {record.creatures.Count} creature(s).");
                    Persist(SaveAsync());
                    DeskChanged?.Invoke();
                    return true;
                }

                return false;
            }
            finally
            {
                IsRestoring = false;
            }
        }

        /// <summary>
        /// Asks the user to lay a hand on the desk. The known desk is replaced if there is one (it moved),
        /// otherwise a new desk is created.
        /// </summary>
        public void Recalibrate()
        {
            calibration.Begin();
        }

        public void CancelCalibration()
        {
            calibration.Cancel();
        }

        /// <summary>Erases the current desk and its saved data. Creatures stay where they are.</summary>
        public async Task ForgetCurrentDeskAsync()
        {
            if (!HasDesk)
            {
                return;
            }

            DeskRecord record = current;
            Transform oldRoot = root;
            foreach (Transform creature in Children(oldRoot))
            {
                creature.SetParent(null, true);
            }

            current = null;
            root = null;
            store.Desks.Remove(record);
            await anchors.EraseAsync(record.AnchorId, oldRoot.gameObject);
            Destroy(oldRoot.gameObject);
            await SaveAsync();
            DeskChanged?.Invoke();
        }

        /// <summary>Puts the creature on the desk frame and remembers where it sits.</summary>
        public void SavePlacement(string creatureId, Transform creature)
        {
            if (!HasDesk)
            {
                return;
            }

            creature.SetParent(root, true);
            current.SetPlacement(creatureId, creature.localPosition, creature.localEulerAngles.y);
            Persist(SaveAsync());
        }

        public bool TryGetPlacement(string creatureId, out Pose world)
        {
            CreaturePlacement placement = HasDesk ? current.Find(creatureId) : null;
            if (placement == null)
            {
                world = default;
                return false;
            }

            world = new Pose(root.TransformPoint(placement.localPosition),
                root.rotation * Quaternion.Euler(0f, placement.localYaw, 0f));
            return true;
        }

        /// <summary>The closest point of the desk zone, or the point itself when there is no desk.</summary>
        public Vector3 ClampToDesk(Vector3 world, float margin) => HasDesk ? Plane.ClampToZone(world, margin) : world;

        // The new desk is usable at once; anchoring it and saving it follow in the background.
        void OnCalibrated(DeskPlane plane)
        {
            GameObject newRoot = new GameObject("Desk");
            newRoot.transform.SetPositionAndRotation(plane.Center, plane.Rotation);
            DeskRecord record = new DeskRecord { halfSize = plane.HalfSize };
            DeskRecord previous = current;
            Transform previousRoot = root;

            if (HasDesk)
            {
                // The known desk moved, or the user redefined it: creatures keep their layout, inside the new zone.
                foreach (Transform creature in Children(root))
                {
                    Vector3 local = DeskPlane.ClampLocal(creature.localPosition, plane.HalfSize, RelayoutMargin);
                    creature.SetParent(newRoot.transform, false);
                    creature.localPosition = local;
                }

                foreach (CreaturePlacement placement in current.creatures)
                {
                    record.SetPlacement(placement.creatureId,
                        DeskPlane.ClampLocal(placement.localPosition, plane.HalfSize, RelayoutMargin), placement.localYaw);
                }

                store.Desks.Remove(current);
            }

            current = record;
            root = newRoot.transform;
            record.lastUsedUtcTicks = DateTime.UtcNow.Ticks;
            Debug.Log($"[Deskmates] Desk calibrated at {plane.Height:F3} m, zone {plane.HalfSize.x * 2f:F2} x {plane.HalfSize.y * 2f:F2} m.");
            DeskChanged?.Invoke();
            Persist(AnchorAndSaveAsync(record, newRoot, previous, previousRoot));
        }

        async Task AnchorAndSaveAsync(DeskRecord record, GameObject newRoot, DeskRecord previous, Transform previousRoot)
        {
            if (previousRoot != null)
            {
                await anchors.EraseAsync(previous.AnchorId, previousRoot.gameObject);
                Destroy(previousRoot.gameObject);
            }

            Guid anchorId = await anchors.AnchorAsync(newRoot);
            if (anchorId != Guid.Empty && current == record)
            {
                record.anchorId = anchorId.ToString();
                store.Desks.Add(record);
                Debug.Log("[Deskmates] Desk anchored; it will be found again next time.");
            }

            await SaveAsync();
        }

        void Persist(Task work)
        {
            Persisting = Task.WhenAll(Persisting, LogFailures(work));
        }

        async Task LogFailures(Task work)
        {
            try
            {
                await work;
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        async Task SaveAsync()
        {
            if (!anchors.IsPersistent)
            {
                return;
            }

            try
            {
                await store.SaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Deskmates] Could not save the desks: {e.Message}");
            }
        }

        static List<Transform> Children(Transform parent) => parent.Cast<Transform>().ToList();
    }
}
