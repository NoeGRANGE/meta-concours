using System;
using Deskmates.Data;
using Deskmates.Data.Stress;
using NUnit.Framework;
using UnityEngine;

namespace Deskmates.Tests
{
    /// <summary>The stress formulas of PRD 4.4, with their default constants.</summary>
    public class StressCalculatorTests
    {
        static readonly DateTime Now = new DateTime(2026, 10, 19, 9, 30, 0);

        [Test]
        public void MailCountsImportantUnreadAndTheOldestWait()
        {
            MailStress mail = ScriptableObject.CreateInstance<MailStress>();
            Assert.AreEqual(0f, mail.Evaluate(Snapshot(), Now));
            Assert.AreEqual(0f, mail.Evaluate(Snapshot(Item(-120)), Now), 1e-4, "Unimportant mail is ignored.");
            Assert.AreEqual(0.36f, mail.Evaluate(Snapshot(Item(0, true), Item(0, true)), Now), 1e-4);
            Assert.AreEqual(0.18f + 0.3f, mail.Evaluate(Snapshot(Item(-180, true)), Now), 1e-4, "Three hours waiting add 0.3.");
            Assert.AreEqual(1f, mail.Evaluate(Snapshot(Item(0, true), Item(0, true), Item(0, true), Item(0, true),
                Item(0, true), Item(0, true)), Now), "Stress is clamped to 1.");
        }

        [TestCase(90f, 0f)]
        [TestCase(60f, 0f)]
        [TestCase(32.5f, 0.45f)]
        [TestCase(5f, 0.9f)]
        [TestCase(3.5f, 0.95f)]
        [TestCase(2f, 1f)]
        [TestCase(0.5f, 1f)]
        public void CalendarRampsUpBeforeTheNextMeeting(float minutesAway, float expected)
        {
            CalendarStress calendar = ScriptableObject.CreateInstance<CalendarStress>();
            Assert.AreEqual(expected, calendar.Evaluate(Snapshot(Item(minutesAway)), Now), 1e-3);
        }

        [Test]
        public void CalendarDropsToZeroOnceTheMeetingStarts()
        {
            CalendarStress calendar = ScriptableObject.CreateInstance<CalendarStress>();
            Assert.AreEqual(0f, calendar.Evaluate(Snapshot(Item(0f), Item(-10f)), Now));
            Assert.AreEqual(0f, calendar.Evaluate(Snapshot(), Now));
            Assert.AreEqual(0.45f, calendar.Evaluate(Snapshot(Item(-5f), Item(32.5f), Item(120f)), Now), 1e-3,
                "Only the next meeting that has not started counts.");
        }

        [Test]
        public void TasksWeighOverdueMoreThanDueToday()
        {
            TasksStress tasks = ScriptableObject.CreateInstance<TasksStress>();
            Assert.AreEqual(0.35f, tasks.Evaluate(Snapshot(Item(-1f)), Now), 1e-4);
            Assert.AreEqual(0.15f, tasks.Evaluate(Snapshot(Item(60f)), Now), 1e-4);
            Assert.AreEqual(0f, tasks.Evaluate(Snapshot(Item(24f * 60f)), Now), "A task due tomorrow does not count.");
            Assert.AreEqual(0.85f, tasks.Evaluate(Snapshot(Item(-30f), Item(-5f), Item(90f)), Now), 1e-4);
        }

        [Test]
        public void MessagesCountPendingAndTheOldestWait()
        {
            MessagesStress messages = ScriptableObject.CreateInstance<MessagesStress>();
            Assert.AreEqual(0.2f, messages.Evaluate(Snapshot(Item(0f)), Now), 1e-4);
            Assert.AreEqual(0.4f + 0.3f, messages.Evaluate(Snapshot(Item(-20f), Item(-5f)), Now), 1e-4,
                "Twenty minutes of waiting add 0.3.");
        }

        static SourceSnapshot Snapshot(params SourceItem[] items) => new SourceSnapshot("test", items);

        static int next;

        static SourceItem Item(float minutesFromNow, bool important = false)
        {
            return new SourceItem($"item-{next++}", "Title", "Detail", Now.AddMinutes(minutesFromNow), important);
        }
    }
}
