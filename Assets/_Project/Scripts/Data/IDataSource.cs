using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deskmates.Data
{
    public enum ItemAction
    {
        MarkRead,
        MarkDone,
        Snooze,
    }

    /// <summary>Source identifiers shared by the demo and the real connectors (PRD 7.4).</summary>
    public static class SourceIds
    {
        public const string Mail = "mail";
        public const string Calendar = "calendar";
        public const string Tasks = "tasks";
        public const string Messages = "messages";
        public const string Agents = "agents";

        public static readonly string[] All = { Mail, Calendar, Tasks, Messages, Agents };
    }

    /// <summary>The states of a coding-agent session (PRD 4.6 and 7.2 bis), carried by <see cref="SourceItem.State"/>.</summary>
    public static class AgentStates
    {
        public const string Running = "running";
        public const string Waiting = "waiting";
        public const string Done = "done";
        public const string Failed = "failed";
    }

    /// <summary>
    /// What a creature reads its data from. The demo and the real connectors implement it the same way,
    /// so the rest of the app never knows whether the data is real (PRD 7.1 and 7.4).
    /// </summary>
    public interface IDataSource
    {
        string Id { get; }

        bool IsConnected { get; }

        SourceSnapshot Current { get; }

        event Action<SourceSnapshot> Changed;

        Task RefreshAsync(CancellationToken ct);

        Task<bool> PerformAsync(ItemAction action, string itemId);
    }

    /// <summary>The time the stress formulas measure against. In demo mode it runs faster than real time.</summary>
    public interface IClock
    {
        DateTime Now { get; }
    }
}
