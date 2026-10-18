using Deskmates.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace Deskmates.Tests
{
    public class StressSmootherTests
    {
        const float Frame = 1f / 72f;

        CreatureConfig config;
        StressSmoother smoother;
        int reliefs;
        int expressionChanges;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<CreatureConfig>();
            smoother = new StressSmoother(config);
            reliefs = 0;
            expressionChanges = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void EasesTowardsTheTargetInOneToTwoSeconds()
        {
            smoother.Target = 1f;
            Run(0.25f);
            Assert.Less(smoother.Value, 0.5f, "Stress must not jump to its target.");
            Run(1.75f);
            Assert.Greater(smoother.Value, 0.95f, "Stress should settle within two seconds.");
        }

        [TestCase(0f, CreatureExpression.Calm)]
        [TestCase(0.3f, CreatureExpression.Alert)]
        [TestCase(0.6f, CreatureExpression.Stressed)]
        [TestCase(1f, CreatureExpression.Panic)]
        public void ReferenceStatesShowTheirExpression(float stress, CreatureExpression expected)
        {
            smoother.Target = stress;
            Run(3f);
            Assert.AreEqual(expected, smoother.Expression);
        }

        [Test]
        public void HoveringOnAThresholdDoesNotMakeTheFaceFlicker()
        {
            smoother.Target = 0.45f;
            Run(3f);
            expressionChanges = 0;
            for (int i = 0; i < 20; i++)
            {
                smoother.Target = i % 2 == 0 ? 0.43f : 0.47f;
                Run(0.5f);
            }

            Assert.AreEqual(0, expressionChanges);
        }

        [Test]
        public void ReliefPlaysOnceWhenCalmFollowsAPanic()
        {
            smoother.Target = 1f;
            Run(3f);
            smoother.Target = 0f;
            Run(3f);
            Assert.AreEqual(1, reliefs);
        }

        [Test]
        public void NoReliefWithoutAStressfulMoment()
        {
            smoother.Target = 0.5f;
            Run(3f);
            smoother.Target = 0f;
            Run(3f);
            Assert.AreEqual(0, reliefs);
        }

        void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                CreatureExpression before = smoother.Expression;
                if (smoother.Step(Frame))
                {
                    reliefs++;
                }

                if (smoother.Expression != before)
                {
                    expressionChanges++;
                }
            }
        }
    }
}
