using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Deskmates.Data.Demo
{
    /// <summary>
    /// A data source fed by the demo script instead of a real service.
    /// </summary>
    public sealed class DemoDataSource : IDataSource
    {
        readonly List<SourceItem> items = new();

        public DemoDataSource(string id)
        {
            Id = id;
            Current = SourceSnapshot.Empty(id);
        }

        public event Action<SourceSnapshot> Changed;

        public string Id { get; }

        public bool IsConnected => true;

        public SourceSnapshot Current { get; private set; }

        // The demo pushes its own changes as the script plays.
        public Task RefreshAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<bool> PerformAsync(ItemAction action, string itemId)
        {
            // Snooze silences a whole creature (PRD 5), it never touches an item.
            bool performed = action != ItemAction.Snooze && Remove(itemId);
            return Task.FromResult(performed);
        }

        internal void Add(SourceItem item)
        {
            items.RemoveAll(existing => existing.Id == item.Id);
            items.Add(item);
            Publish();
        }

        internal bool Remove(string itemId)
        {
            if (items.RemoveAll(existing => existing.Id == itemId) == 0)
            {
                return false;
            }

            Publish();
            return true;
        }

        internal void Clear()
        {
            if (items.Count == 0)
            {
                return;
            }

            items.Clear();
            Publish();
        }

        void Publish()
        {
            Current = new SourceSnapshot(Id, items.ToArray());
            Changed?.Invoke(Current);
        }
    }
}
