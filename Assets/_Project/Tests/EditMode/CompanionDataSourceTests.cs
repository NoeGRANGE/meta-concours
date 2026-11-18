using System;
using Deskmates.Data;
using Deskmates.Data.Live;
using NUnit.Framework;

namespace Deskmates.Tests
{
    public class CompanionDataSourceTests
    {
        [Test]
        public void CompanionItemsBecomeSourceItems()
        {
            const string json = @"{""source"":""agents"",""items"":[
                {""id"":""s1"",""title"":""refactor-auth"",""detail"":""Asks to run the migration"",
                 ""time"":""2026-11-18T09:30:00Z"",""important"":false,""state"":""waiting""},
                {""id"":""m1"",""title"":""Contract draft"",""detail"":""Maya"",""time"":""2026-11-18T08:00:00Z"",
                 ""important"":true,""state"":""""},
                {""id"":""bad"",""title"":""No time"",""detail"":"""",""time"":""not a date""}]}";

            SourceSnapshot snapshot = CompanionDataSource.Parse(SourceIds.Agents, json);

            Assert.AreEqual(2, snapshot.Items.Count, "An item without a valid time is skipped.");
            Assert.AreEqual(AgentStates.Waiting, snapshot.Items[0].State);
            Assert.AreEqual(new DateTime(2026, 11, 18, 9, 30, 0, DateTimeKind.Utc).ToLocalTime(), snapshot.Items[0].Time);
            Assert.IsTrue(snapshot.Items[1].Important);
            Assert.IsNull(snapshot.Items[1].State, "An empty state means the item has none.");
        }

        [Test]
        public void AnEmptyAnswerIsAnEmptySource()
        {
            Assert.AreEqual(0, CompanionDataSource.Parse(SourceIds.Mail, @"{""items"":[]}").Items.Count);
        }
    }
}
