using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deskmates.Data.Live
{
    /// <summary>
    /// One creature's source in Live mode: the items the companion holds for it (unread important mail, upcoming
    /// meetings, open tasks, coding-agent sessions), refreshed on a timer. Actions are forwarded to the companion,
    /// which applies them to the real account.
    /// </summary>
    public sealed class CompanionDataSource : IDataSource
    {
        readonly CompanionClient client;

        public CompanionDataSource(string id, CompanionClient client)
        {
            Id = id;
            this.client = client;
            Current = SourceSnapshot.Empty(id);
        }

        public event Action<SourceSnapshot> Changed;

        public string Id { get; }

        public bool IsConnected => client.IsConnected;

        public SourceSnapshot Current { get; private set; }

        public async Task RefreshAsync(CancellationToken ct)
        {
            string json = await client.GetAsync($"/v1/sources/{Id}", ct);
            Publish(Parse(Id, json));
        }

        public async Task<bool> PerformAsync(ItemAction action, string itemId)
        {
            string verb = action switch
            {
                ItemAction.MarkRead => "read",
                ItemAction.MarkDone => "done",
                _ => "snooze",
            };
            bool performed = await client.PostAsync($"/v1/sources/{Id}/items/{Uri.EscapeDataString(itemId)}/{verb}",
                CancellationToken.None);
            if (performed)
            {
                // Show the result at once; the next refresh confirms it.
                List<SourceItem> remaining = new List<SourceItem>(Current.Items);
                remaining.RemoveAll(item => item.Id == itemId);
                Publish(new SourceSnapshot(Id, remaining));
            }

            return performed;
        }

        void Publish(SourceSnapshot snapshot)
        {
            Current = snapshot;
            Changed?.Invoke(snapshot);
        }

        /// <summary>Reads the companion's JSON for one source. Times are ISO 8601, converted to local time.</summary>
        public static SourceSnapshot Parse(string sourceId, string json)
        {
            Payload payload = JsonUtility.FromJson<Payload>(json);
            List<SourceItem> items = new List<SourceItem>();
            foreach (Item item in payload?.items ?? Array.Empty<Item>())
            {
                if (!DateTime.TryParse(item.time, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime utc))
                {
                    continue;
                }

                items.Add(new SourceItem(item.id, item.title, item.detail, utc.ToLocalTime(), item.important,
                    string.IsNullOrEmpty(item.state) ? null : item.state));
            }

            return new SourceSnapshot(sourceId, items);
        }

        [Serializable]
        sealed class Payload
        {
            public Item[] items;
        }

        [Serializable]
        sealed class Item
        {
            public string id;
            public string title;
            public string detail;
            public string time;
            public bool important;
            public string state;
        }
    }
}
