using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deskmates.Data.Live
{
    /// <summary>
    /// The sources of Live mode: mail, calendar, tasks and coding agents from the companion, refreshed on a timer.
    /// Messages has no live connector yet and stays calm.
    /// </summary>
    public sealed class LiveSources : IClock, IDisposable
    {
        static readonly string[] Connected = { SourceIds.Mail, SourceIds.Calendar, SourceIds.Tasks, SourceIds.Agents };

        readonly DataSettings settings;
        readonly CompanionClient client;
        readonly Dictionary<string, IDataSource> sources = new();
        readonly CancellationTokenSource stopping = new();
        float untilRefresh;
        bool refreshing;

        public LiveSources(DataSettings settings)
        {
            this.settings = settings;
            client = new CompanionClient(settings);
            foreach (string id in Connected)
            {
                sources.Add(id, new CompanionDataSource(id, client));
            }

            sources.Add(SourceIds.Messages, new StaticSource(SourceIds.Messages));
        }

        public DateTime Now => DateTime.Now;

        public IDataSource GetSource(string id) => sources.TryGetValue(id, out IDataSource source) ? source : null;

        public void Step(float deltaSeconds)
        {
            untilRefresh -= deltaSeconds;
            if (untilRefresh > 0f || refreshing)
            {
                return;
            }

            untilRefresh = Mathf.Max(5f, settings.refreshSeconds);
            RefreshAll();
        }

        async void RefreshAll()
        {
            refreshing = true;
            try
            {
                foreach (string id in Connected)
                {
                    await sources[id].RefreshAsync(stopping.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Deskmates] Could not refresh from the companion: {e.Message}");
            }
            finally
            {
                refreshing = false;
            }
        }

        public void Dispose()
        {
            stopping.Cancel();
            client.Dispose();
        }

        /// <summary>A source with nothing to report.</summary>
        sealed class StaticSource : IDataSource
        {
            public StaticSource(string id)
            {
                Id = id;
                Current = SourceSnapshot.Empty(id);
            }

            public event Action<SourceSnapshot> Changed
            {
                add { }
                remove { }
            }

            public string Id { get; }

            public bool IsConnected => false;

            public SourceSnapshot Current { get; }

            public Task RefreshAsync(CancellationToken ct) => Task.CompletedTask;

            public Task<bool> PerformAsync(ItemAction action, string itemId) => Task.FromResult(false);
        }
    }
}
