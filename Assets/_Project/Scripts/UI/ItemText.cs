using System;
using System.Collections.Generic;
using System.Linq;
using Deskmates.Data;

namespace Deskmates.UI
{
    /// <summary>The English wording of the peek bubble and the detail panel (PRD 5), per source.</summary>
    public static class ItemText
    {
        /// <summary>One line for the peek bubble, e.g. "3 important emails · oldest 2 h".</summary>
        public static string Summary(string sourceId, SourceSnapshot snapshot, DateTime now)
        {
            IReadOnlyList<SourceItem> items = snapshot.Items;
            switch (sourceId)
            {
                case SourceIds.Mail:
                    SourceItem[] important = items.Where(i => i.Important).ToArray();
                    return important.Length == 0
                        ? "Inbox calm"
                        : $"{Count(important.Length, "important email")} · oldest {Age(now - important.Min(i => i.Time))}";
                case SourceIds.Calendar:
                    SourceItem next = Upcoming(items, now).FirstOrDefault();
                    return next == null ? "No meetings coming up" : $"{next.Title} in {Minutes(next.Time - now)}";
                case SourceIds.Tasks:
                    int overdue = items.Count(i => i.Time < now);
                    int today = items.Count(i => i.Time >= now && i.Time.Date == now.Date);
                    return overdue + today == 0 ? "All clear for today" : $"{overdue} overdue · {today} due today";
                default:
                    return items.Count == 0
                        ? "No one is waiting"
                        : $"{Count(items.Count, "message")} waiting · oldest {Age(now - items.Min(i => i.Time))}";
            }
        }

        /// <summary>The three most urgent items, most urgent first.</summary>
        public static IReadOnlyList<SourceItem> Top(string sourceId, SourceSnapshot snapshot, DateTime now, int count = 3)
        {
            IEnumerable<SourceItem> items = sourceId switch
            {
                SourceIds.Mail => snapshot.Items.OrderByDescending(i => i.Important).ThenBy(i => i.Time),
                SourceIds.Calendar => Upcoming(snapshot.Items, now),
                _ => snapshot.Items.OrderBy(i => i.Time),
            };
            return items.Take(count).ToList();
        }

        public static string Row(string sourceId, SourceItem item, DateTime now)
        {
            return sourceId switch
            {
                SourceIds.Mail => $"{item.Title}\n<size=18>{item.Detail} · {Age(now - item.Time)} ago</size>",
                SourceIds.Calendar => $"{item.Title}\n<size=18>{item.Detail} · in {Minutes(item.Time - now)}</size>",
                SourceIds.Tasks => item.Time < now
                    ? $"{item.Title}\n<size=18>overdue by {Age(now - item.Time)}</size>"
                    : $"{item.Title}\n<size=18>due in {Age(item.Time - now)}</size>",
                _ => $"{item.Title}\n<size=18>{item.Detail} · {Age(now - item.Time)} ago</size>",
            };
        }

        /// <summary>What pinching a row does, or null when the row cannot be acted on (meetings are read-only).</summary>
        public static ItemAction? Action(string sourceId) => sourceId switch
        {
            SourceIds.Calendar => null,
            SourceIds.Tasks => ItemAction.MarkDone,
            _ => ItemAction.MarkRead,
        };

        public static string Age(TimeSpan span)
        {
            if (span.TotalMinutes < 1d)
            {
                return "<1 min";
            }

            return span.TotalHours < 1d ? $"{(int)span.TotalMinutes} min" : $"{(int)span.TotalHours} h";
        }

        static string Minutes(TimeSpan span) => $"{Math.Max(1, (int)Math.Ceiling(span.TotalMinutes))} min";

        static string Count(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";

        static IEnumerable<SourceItem> Upcoming(IEnumerable<SourceItem> items, DateTime now) =>
            items.Where(i => i.Time > now).OrderBy(i => i.Time);
    }
}
