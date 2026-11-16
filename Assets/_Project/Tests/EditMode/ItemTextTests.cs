using System;
using Deskmates.Data;
using Deskmates.UI;
using NUnit.Framework;

namespace Deskmates.Tests
{
    /// <summary>Wording of the peek bubble and the panel's order of items (PRD 5).</summary>
    public class ItemTextTests
    {
        static readonly DateTime Now = new DateTime(2026, 11, 12, 10, 0, 0);

        [Test]
        public void SummariesReadNaturally()
        {
            Assert.AreEqual("3 important emails · oldest 2 h", ItemText.Summary(SourceIds.Mail,
                Snapshot(Item("a", -120, true), Item("b", -5, true), Item("c", -1, true), Item("d", -300)), Now));
            Assert.AreEqual("Inbox calm", ItemText.Summary(SourceIds.Mail, Snapshot(Item("d", -300)), Now));
            Assert.AreEqual("Design review in 13 min", ItemText.Summary(SourceIds.Calendar,
                Snapshot(Item("past", -5), Item("Design review", 12.5f), Item("later", 90)), Now));
            Assert.AreEqual("1 overdue · 1 due today", ItemText.Summary(SourceIds.Tasks,
                Snapshot(Item("a", -10), Item("b", 30), Item("c", 24 * 60)), Now));
            Assert.AreEqual("1 message waiting · oldest 25 min", ItemText.Summary(SourceIds.Messages,
                Snapshot(Item("a", -25)), Now));
        }

        [Test]
        public void TheMostUrgentItemsComeFirst()
        {
            var top = ItemText.Top(SourceIds.Mail, Snapshot(Item("plain", -300), Item("new", -1, true), Item("old", -60, true)), Now);
            Assert.AreEqual(new[] { "old", "new", "plain" }, new[] { top[0].Id, top[1].Id, top[2].Id });
            Assert.AreEqual(2, ItemText.Top(SourceIds.Calendar, Snapshot(Item("started", -1), Item("a", 5), Item("b", 50)), Now).Count,
                "Meetings that started are not listed.");
        }

        [Test]
        public void MeetingsCannotBeMarked()
        {
            Assert.IsNull(ItemText.Action(SourceIds.Calendar));
            Assert.AreEqual(ItemAction.MarkDone, ItemText.Action(SourceIds.Tasks));
            Assert.AreEqual(ItemAction.MarkRead, ItemText.Action(SourceIds.Mail));
        }

        [Test]
        public void AgentSessionsNeedingTheUserComeFirst()
        {
            SourceSnapshot sessions = Snapshot(Session("running", -9, AgentStates.Running), Session("done", -3, AgentStates.Done),
                Session("waiting", -2, AgentStates.Waiting), Session("failed", -1, AgentStates.Failed));
            Assert.AreEqual("1 waiting for you · 1 failed · 1 done", ItemText.Summary(SourceIds.Agents, sessions, Now));
            var top = ItemText.Top(SourceIds.Agents, sessions, Now);
            Assert.AreEqual(new[] { "waiting", "failed", "done" }, new[] { top[0].Id, top[1].Id, top[2].Id });
            Assert.AreEqual("1 session running", ItemText.Summary(SourceIds.Agents, Snapshot(Session("r", 0, AgentStates.Running)), Now));
            Assert.AreEqual("seen", ItemText.ActionVerb(SourceIds.Agents));
        }

        static SourceItem Session(string id, float minutesFromNow, string state) =>
            new SourceItem(id, id, "Detail", Now.AddMinutes(minutesFromNow), false, state);

        static SourceSnapshot Snapshot(params SourceItem[] items) => new SourceSnapshot("test", items);

        static SourceItem Item(string id, float minutesFromNow, bool important = false) =>
            new SourceItem(id, id, "Someone", Now.AddMinutes(minutesFromNow), important);
    }
}
