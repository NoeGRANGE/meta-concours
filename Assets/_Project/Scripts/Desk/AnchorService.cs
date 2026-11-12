using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deskmates.Desk
{
    /// <summary>
    /// Creates, finds and erases the spatial anchor a desk hangs from (PRD 5.2).
    /// </summary>
    public interface IAnchorService
    {
        /// <summary>False when anchors only last for the session, so there is no point saving desks.</summary>
        bool IsPersistent { get; }

        /// <summary>Anchors the object where it stands and saves the anchor. Returns its id, or Guid.Empty.</summary>
        Task<Guid> AnchorAsync(GameObject target);

        /// <summary>Finds a saved anchor in this space and returns a new object that follows it, or null.</summary>
        Task<GameObject> LoadAsync(Guid id);

        Task EraseAsync(Guid id, GameObject anchored);
    }

    /// <summary>Meta spatial anchors, saved on the device.</summary>
    public sealed class OvrAnchorService : IAnchorService
    {
        const int LocalizeTimeoutMs = 5000;

        public bool IsPersistent => true;

        public async Task<Guid> AnchorAsync(GameObject target)
        {
            OVRSpatialAnchor anchor = target.AddComponent<OVRSpatialAnchor>();
            if (!await anchor.WhenLocalizedAsync())
            {
                Debug.LogWarning("[Deskmates] Could not create the desk anchor; the desk will not be remembered.");
                Object.Destroy(anchor);
                return Guid.Empty;
            }

            OVRResult<OVRAnchor.SaveResult> saved = await anchor.SaveAnchorAsync();
            if (!saved.Success)
            {
                Debug.LogWarning($"[Deskmates] Could not save the desk anchor ({saved.Status}); it lasts for this session only.");
                return Guid.Empty;
            }

            return anchor.Uuid;
        }

        public async Task<GameObject> LoadAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            List<OVRSpatialAnchor.UnboundAnchor> found = new();
            var loaded = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(new[] { id }, found);
            if (!loaded.Success || found.Count == 0)
            {
                return null;
            }

            OVRSpatialAnchor.UnboundAnchor unbound = found[0];
            if (!unbound.Localized && !await Localize(unbound))
            {
                return null;
            }

            // Bind before the component's Start, or it would create a brand new anchor instead.
            GameObject anchored = new GameObject("Desk");
            unbound.BindTo(anchored.AddComponent<OVRSpatialAnchor>());
            return anchored;
        }

        public async Task EraseAsync(Guid id, GameObject anchored)
        {
            if (anchored != null && anchored.TryGetComponent(out OVRSpatialAnchor anchor))
            {
                await anchor.EraseAnchorAsync();
            }
            else if (id != Guid.Empty)
            {
                await OVRSpatialAnchor.EraseAnchorsAsync(Array.Empty<OVRSpatialAnchor>(), new[] { id });
            }
        }

        static async Task<bool> Localize(OVRSpatialAnchor.UnboundAnchor unbound)
        {
            Task<bool> localize = LocalizeTask(unbound);
            Task finished = await Task.WhenAny(localize, Task.Delay(LocalizeTimeoutMs));
            return finished == localize && localize.Result;
        }

        static async Task<bool> LocalizeTask(OVRSpatialAnchor.UnboundAnchor unbound) => await unbound.LocalizeAsync();
    }

    /// <summary>
    /// Anchors that only exist in memory: used in the editor without a headset and in tests. Desks found again
    /// within the same run, never after a restart.
    /// </summary>
    public sealed class InMemoryAnchorService : IAnchorService
    {
        readonly Dictionary<Guid, Pose> anchors;

        public InMemoryAnchorService(Dictionary<Guid, Pose> sharedAnchors = null, bool isPersistent = false)
        {
            anchors = sharedAnchors ?? new Dictionary<Guid, Pose>();
            IsPersistent = isPersistent;
        }

        public bool IsPersistent { get; }

        public Task<Guid> AnchorAsync(GameObject target)
        {
            Guid id = Guid.NewGuid();
            anchors[id] = new Pose(target.transform.position, target.transform.rotation);
            return Task.FromResult(id);
        }

        public Task<GameObject> LoadAsync(Guid id)
        {
            if (!anchors.TryGetValue(id, out Pose pose))
            {
                return Task.FromResult<GameObject>(null);
            }

            GameObject anchored = new GameObject("Desk");
            anchored.transform.SetPositionAndRotation(pose.position, pose.rotation);
            return Task.FromResult(anchored);
        }

        public Task EraseAsync(Guid id, GameObject anchored)
        {
            anchors.Remove(id);
            return Task.CompletedTask;
        }
    }
}
