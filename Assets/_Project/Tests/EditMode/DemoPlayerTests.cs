using Deskmates.Data;
using Deskmates.Data.Demo;
using NUnit.Framework;
using UnityEngine;

namespace Deskmates.Tests
{
    public class DemoPlayerTests
    {
        DemoScript script;

        [SetUp]
        public void SetUp()
        {
            script = ScriptableObject.CreateInstance<DemoScript>();
            script.demoMinutesPerSecond = 2f;
            script.tailSeconds = 5f;
            script.events.Add(new DemoEvent
            {
                atSecond = 1f, sourceId = SourceIds.Mail, kind = DemoEventKind.AddItem, itemId = "a",
                title = "Hello", detail = "Someone", minutesFromNow = -3f, important = true,
            });
            script.events.Add(new DemoEvent
            {
                atSecond = 3f, sourceId = SourceIds.Mail, kind = DemoEventKind.Resolve, itemId = "a",
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(script);
        }

        [Test]
        public void EventsPlayWhenTheirTimeComes()
        {
            DemoPlayer player = new DemoPlayer(script);
            IDataSource mail = player.GetSource(SourceIds.Mail);
            int changes = 0;
            mail.Changed += _ => changes++;

            player.Step(0.5f);
            Assert.IsEmpty(mail.Current.Items);

            player.Step(1f);
            Assert.AreEqual(1, mail.Current.Items.Count);
            SourceItem item = mail.Current.Items[0];
            Assert.AreEqual("a", item.Id);
            Assert.IsTrue(item.Important);
            Assert.AreEqual(player.Now.AddMinutes(-3f - 1f), item.Time,
                "Item time is relative to the demo clock when the event fired, 0.5 s (1 demo minute) ago.");

            player.Step(2f);
            Assert.IsEmpty(mail.Current.Items);
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void ResolveEventsWaitForTheUserWhenAutoResolveIsOff()
        {
            script.autoResolve = false;
            DemoPlayer player = new DemoPlayer(script);
            player.Step(10f);
            Assert.AreEqual(1, player.GetSource(SourceIds.Mail).Current.Items.Count);
        }

        [Test]
        public void TheClockRunsAtTheScriptRate()
        {
            DemoPlayer player = new DemoPlayer(script);
            System.DateTime start = player.Now;
            player.Step(30f);
            Assert.AreEqual(60.0, (player.Now - start).TotalMinutes, 1e-6);
            Assert.AreEqual(9.5, start.TimeOfDay.TotalHours, 1e-6, "The demo day starts at 9:30.");
        }

        [Test]
        public void UserActionsRemoveItems()
        {
            DemoPlayer player = new DemoPlayer(script);
            player.Step(2f);
            IDataSource mail = player.GetSource(SourceIds.Mail);
            Assert.IsFalse(mail.PerformAsync(ItemAction.Snooze, "a").Result, "Snooze is per creature, not per item.");
            Assert.IsTrue(mail.PerformAsync(ItemAction.MarkRead, "a").Result);
            Assert.IsFalse(mail.PerformAsync(ItemAction.MarkRead, "a").Result);
            Assert.IsEmpty(mail.Current.Items);
        }

        [Test]
        public void LoopingStartsOverAfterTheTail()
        {
            script.loop = true;
            DemoPlayer player = new DemoPlayer(script);
            player.Step(2f);
            Assert.AreEqual(1, player.GetSource(SourceIds.Mail).Current.Items.Count);
            player.Step(6.5f);
            Assert.Less(player.Elapsed, 1f);
            Assert.IsEmpty(player.GetSource(SourceIds.Mail).Current.Items);
        }
    }
}
