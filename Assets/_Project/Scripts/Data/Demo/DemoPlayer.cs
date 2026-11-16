using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Deskmates.Data.Demo
{
    /// <summary>
    /// Plays a <see cref="DemoScript"/>: runs the demo clock and applies each event when its time comes.
    /// A plain class so tests can step it with any time step.
    /// </summary>
    public sealed class DemoPlayer : IClock
    {
        readonly DemoScript script;
        readonly List<DemoEvent> events;
        readonly Dictionary<string, DemoDataSource> sources = new();
        int nextEvent;
        DateTime start;

        public DemoPlayer(DemoScript script)
        {
            this.script = script;
            events = script.events.OrderBy(e => e.atSecond).ToList();
            foreach (string id in SourceIds.All)
            {
                sources.Add(id, new DemoDataSource(id));
            }

            Restart();
        }

        /// <summary>Real seconds since the demo started, after the speed multiplier.</summary>
        public float Elapsed { get; private set; }

        public float Duration => (events.Count > 0 ? events[events.Count - 1].atSecond : 0f) + script.tailSeconds;

        public DateTime Now => ClockAt(Elapsed);

        public IDataSource GetSource(string id) => sources.TryGetValue(id, out DemoDataSource source) ? source : null;

        public void Step(float deltaSeconds)
        {
            Elapsed += deltaSeconds;
            while (nextEvent < events.Count && events[nextEvent].atSecond <= Elapsed)
            {
                Apply(events[nextEvent++]);
            }

            if (script.loop && Elapsed >= Duration)
            {
                Restart();
            }
        }

        void Restart()
        {
            Elapsed = 0f;
            nextEvent = 0;
            start = DateTime.Today.AddHours(script.startHour);
            foreach (DemoDataSource source in sources.Values)
            {
                source.Clear();
            }
        }

        DateTime ClockAt(float seconds) => start.AddMinutes(seconds * script.demoMinutesPerSecond);

        void Apply(DemoEvent demoEvent)
        {
            if (!sources.TryGetValue(demoEvent.sourceId, out DemoDataSource source))
            {
                Debug.LogWarning($"[Deskmates] Demo event at {demoEvent.atSecond}s targets unknown source '{demoEvent.sourceId}'.");
                return;
            }

            switch (demoEvent.kind)
            {
                case DemoEventKind.AddItem:
                    DateTime time = ClockAt(demoEvent.atSecond).AddMinutes(demoEvent.minutesFromNow);
                    source.Add(new SourceItem(demoEvent.itemId, demoEvent.title, demoEvent.detail, time, demoEvent.important,
                        string.IsNullOrEmpty(demoEvent.state) ? null : demoEvent.state));
                    break;
                case DemoEventKind.Resolve when script.autoResolve:
                    if (string.IsNullOrEmpty(demoEvent.itemId))
                    {
                        source.Clear();
                    }
                    else
                    {
                        source.Remove(demoEvent.itemId);
                    }

                    break;
            }
        }
    }
}
