using System;
using System.Collections.Generic;

namespace Deskmates.Data
{
    /// <summary>
    /// One pending thing: an unread mail, a meeting, a task or a waiting message.
    /// </summary>
    public sealed class SourceItem
    {
        public SourceItem(string id, string title, string detail, DateTime time, bool important = false, string state = null)
        {
            Id = id;
            Title = title;
            Detail = detail;
            Time = time;
            Important = important;
            State = state;
        }

        public string Id { get; }

        /// <summary>Subject, meeting name, task or message text.</summary>
        public string Title { get; }

        /// <summary>Sender, organiser or list name.</summary>
        public string Detail { get; }

        /// <summary>When the mail or message arrived, the meeting starts or the task is due.</summary>
        public DateTime Time { get; }

        /// <summary>For mail: flagged important or from a priority contact (PRD 4.4).</summary>
        public bool Important { get; }

        /// <summary>For agent sessions: one of <see cref="AgentStates"/>, entered at <see cref="Time"/>. Null elsewhere.</summary>
        public string State { get; }
    }

    /// <summary>
    /// The items a source holds at one moment. Stress formulas derive their metrics from it at the current time,
    /// so a meeting keeps getting closer between two snapshots.
    /// </summary>
    public sealed class SourceSnapshot
    {
        public SourceSnapshot(string sourceId, IReadOnlyList<SourceItem> items)
        {
            SourceId = sourceId;
            Items = items;
        }

        public string SourceId { get; }

        public IReadOnlyList<SourceItem> Items { get; }

        public static SourceSnapshot Empty(string sourceId) => new SourceSnapshot(sourceId, Array.Empty<SourceItem>());
    }
}
