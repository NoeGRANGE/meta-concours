using System.Collections.Generic;
using Deskmates.Creatures;
using Deskmates.Data;
using Deskmates.Data.Demo;
using Deskmates.Data.Stress;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deskmates.Tests
{
    /// <summary>
    /// Plays the default demo scenario through the real stress formulas and smoothing, and checks PRD 7.1:
    /// within three to four minutes every creature goes through every state and calms down again.
    /// </summary>
    public class DemoScenarioTests
    {
        const string DemoScriptPath = "Assets/_Project/Settings/Data/DemoScript_Default.asset";
        const float Frame = 1f / 30f;

        static readonly Dictionary<string, string> Calculators = new()
        {
            { SourceIds.Mail, "Assets/_Project/Settings/Stress/Stress_Mail.asset" },
            { SourceIds.Calendar, "Assets/_Project/Settings/Stress/Stress_Calendar.asset" },
            { SourceIds.Tasks, "Assets/_Project/Settings/Stress/Stress_Tasks.asset" },
            { SourceIds.Messages, "Assets/_Project/Settings/Stress/Stress_Messages.asset" },
            { SourceIds.Agents, "Assets/_Project/Settings/Stress/Stress_Agents.asset" },
        };

        // Words that would put a brand in the app (PRD 2.1, rule 5).
        static readonly string[] Brands =
        {
            "gmail", "google", "outlook", "microsoft", "teams", "slack", "zoom", "notion", "jira", "todoist",
            "linear", "github", "apple", "meta", "claude", "anthropic", "codex", "openai", "copilot", "cursor",
        };

        [Test]
        public void EveryCreatureGoesThroughEveryStateAndCalmsDown([ValueSource(nameof(Sources))] string sourceId)
        {
            DemoScript script = AssetDatabase.LoadAssetAtPath<DemoScript>(DemoScriptPath);
            StressCalculator calculator = AssetDatabase.LoadAssetAtPath<StressCalculator>(Calculators[sourceId]);
            Assert.IsNotNull(script, "Default demo script missing.");
            Assert.IsNotNull(calculator, $"Stress calculator for {sourceId} missing.");

            DemoPlayer player = new DemoPlayer(script);
            Assert.That(player.Duration, Is.InRange(150f, 240f), "The demo should last three to four minutes.");

            CreatureConfig config = ScriptableObject.CreateInstance<CreatureConfig>();
            float reliefLow = config.reliefLow;
            StressSmoother smoother = new StressSmoother(config);
            HashSet<CreatureExpression> seen = new HashSet<CreatureExpression> { smoother.Expression };
            int reliefs = 0;
            IDataSource source = player.GetSource(sourceId);
            for (float t = 0f; t < player.Duration; t += Frame)
            {
                player.Step(Frame);
                smoother.Target = calculator.Evaluate(source.Current, player.Now);
                if (smoother.Step(Frame))
                {
                    reliefs++;
                }

                seen.Add(smoother.Expression);
            }

            Object.DestroyImmediate(config);
            Assert.AreEqual(4, seen.Count, $"{sourceId} only showed {string.Join(", ", seen)}.");
            Assert.GreaterOrEqual(reliefs, 1, $"{sourceId} never sighed with relief.");
            Assert.Less(smoother.Value, reliefLow, $"{sourceId} should end calm.");
        }

        [Test]
        public void DemoTextHasNoBrandNames()
        {
            DemoScript script = AssetDatabase.LoadAssetAtPath<DemoScript>(DemoScriptPath);
            foreach (DemoEvent demoEvent in script.events)
            {
                string text = $"{demoEvent.title} {demoEvent.detail}".ToLowerInvariant();
                foreach (string brand in Brands)
                {
                    StringAssert.DoesNotContain(brand, text, $"Event at {demoEvent.atSecond}s mentions a brand.");
                }
            }
        }

        static IEnumerable<string> Sources() => SourceIds.All;
    }
}
