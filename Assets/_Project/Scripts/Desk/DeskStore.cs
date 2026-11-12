using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deskmates.Desk
{
    /// <summary>Where a creature sits on a desk, in the desk's frame.</summary>
    [Serializable]
    public sealed class CreaturePlacement
    {
        public string creatureId;
        public Vector3 localPosition;
        public float localYaw;
    }

    /// <summary>One calibrated desk: its anchor, its zone and the creatures on it (PRD 5.2).</summary>
    [Serializable]
    public sealed class DeskRecord
    {
        public string anchorId;
        public Vector2 halfSize;
        public long lastUsedUtcTicks;
        public List<CreaturePlacement> creatures = new();

        public Guid AnchorId => Guid.TryParse(anchorId, out Guid id) ? id : Guid.Empty;

        public CreaturePlacement Find(string creatureId) => creatures.Find(p => p.creatureId == creatureId);

        public void SetPlacement(string creatureId, Vector3 localPosition, float localYaw)
        {
            CreaturePlacement placement = Find(creatureId);
            if (placement == null)
            {
                placement = new CreaturePlacement { creatureId = creatureId };
                creatures.Add(placement);
            }

            placement.localPosition = localPosition;
            placement.localYaw = localYaw;
        }
    }

    /// <summary>
    /// The desks the user has calibrated, kept as JSON on the device. Nothing leaves it (PRD 5.2).
    /// </summary>
    public sealed class DeskStore
    {
        readonly string path;
        readonly SemaphoreSlim writing = new SemaphoreSlim(1, 1);

        public DeskStore(string path)
        {
            this.path = path;
        }

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "desks.json");

        public List<DeskRecord> Desks { get; } = new();

        public async Task LoadAsync()
        {
            Desks.Clear();
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                string json = await File.ReadAllTextAsync(path);
                StoreData data = JsonUtility.FromJson<StoreData>(json);
                if (data?.desks != null)
                {
                    Desks.AddRange(data.desks);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Deskmates] Could not read {path}, starting without saved desks: {e.Message}");
            }
        }

        public async Task SaveAsync()
        {
            // Serialised on the calling thread; only the file write happens in the background.
            string json = JsonUtility.ToJson(new StoreData { desks = Desks }, true);
            await writing.WaitAsync();
            try
            {
                string temporary = path + ".tmp";
                await File.WriteAllTextAsync(temporary, json);
                File.Copy(temporary, path, true);
                File.Delete(temporary);
            }
            finally
            {
                writing.Release();
            }
        }

        [Serializable]
        sealed class StoreData
        {
            public List<DeskRecord> desks = new();
        }
    }
}
