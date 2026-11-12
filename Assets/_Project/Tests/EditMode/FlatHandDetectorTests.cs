using Deskmates.Desk;
using NUnit.Framework;
using UnityEngine;

namespace Deskmates.Tests
{
    /// <summary>The hand-on-desk gesture of PRD 5.1, fed with made-up hands.</summary>
    public class FlatHandDetectorTests
    {
        const float Frame = 1f / 72f;
        const float Desk = 0.75f;
        static readonly Vector3 Eyes = new Vector3(0f, 1.2f, 0f);

        DeskCalibrationConfig config;
        FlatHandDetector detector;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<DeskCalibrationConfig>();
            detector = new FlatHandDetector(config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AFlatStillHandGivesTheDeskHeight()
        {
            Assert.IsTrue(Run(0.7f, () => FlatHand(new Vector3(0.1f, Desk, 0.4f)), () => default, out DeskMeasurement m));
            Assert.AreEqual(Desk + 0.008f - config.handThickness, m.Height, 1e-4, "Lowest joints sit 8 mm above the desk.");
            Assert.AreEqual(1, m.HandCenters.Count);
            Assert.AreEqual(m.Height, m.HandCenters[0].y, 1e-5);
        }

        [Test]
        public void TheHandMustRestLongEnough()
        {
            Assert.IsFalse(Run(0.4f, () => FlatHand(new Vector3(0f, Desk, 0.4f)), () => default, out _));
        }

        [Test]
        public void AMovingHandIsIgnored()
        {
            float x = 0f;
            Assert.IsFalse(Run(1.5f, () => FlatHand(new Vector3(x += 0.002f, Desk, 0.4f)), () => default, out _),
                "A hand sliding 14 cm/s never holds still.");
        }

        [Test]
        public void ATrackingJitterDoesNotResetTheHold()
        {
            int frame = 0;
            Assert.IsTrue(Run(0.8f, () => FlatHand(new Vector3(0.003f * Mathf.Sin(frame++), Desk, 0.4f)), () => default, out _),
                "A few millimetres of jitter still count as still.");
        }

        [Test]
        public void ATiltedOrBentHandIsIgnored()
        {
            Assert.IsFalse(Run(1f, () => FlatHand(new Vector3(0f, Desk, 0.4f), tilt: 0.05f), () => default, out _));
        }

        [Test]
        public void AHandNearTheEyesIsIgnored()
        {
            Assert.IsFalse(Run(1f, () => FlatHand(new Vector3(0f, Eyes.y - 0.1f, 0.3f)), () => default, out _));
        }

        [Test]
        public void LosingTrackingResetsTheHold()
        {
            int frame = 0;
            Assert.IsFalse(Run(1f, () => frame++ % 30 == 29 ? default : FlatHand(new Vector3(0f, Desk, 0.4f)), () => default, out _),
                "Tracking lost every 30 frames: the hold never reaches 0.6 s.");
        }

        [Test]
        public void TwoRestingHandsAreAveraged()
        {
            Assert.IsTrue(Run(0.7f, () => FlatHand(new Vector3(-0.2f, Desk, 0.4f)),
                () => FlatHand(new Vector3(0.2f, Desk + 0.01f, 0.4f)), out DeskMeasurement m));
            Assert.AreEqual(2, m.HandCenters.Count);
            Assert.AreEqual(Desk + 0.005f + 0.008f - config.handThickness, m.Height, 1e-4);
        }

        bool Run(float seconds, System.Func<HandSample> left, System.Func<HandSample> right, out DeskMeasurement measurement)
        {
            measurement = default;
            for (float t = 0f; t < seconds; t += Frame)
            {
                if (detector.Step(Frame, Eyes, left(), right(), out measurement))
                {
                    return true;
                }
            }

            return false;
        }

        // Nine joints over a flat hand 18 cm long, lowest joints 8 mm above the surface at <paramref name="surface"/>.
        static HandSample FlatHand(Vector3 surface, float tilt = 0f)
        {
            Vector3 joints = surface + Vector3.up * 0.008f;
            Vector3[] contacts =
            {
                joints + new Vector3(0f, 0.01f, -0.09f),
                joints + new Vector3(-0.03f, 0.004f, 0f), joints + new Vector3(-0.01f, 0.004f, 0f),
                joints + new Vector3(0.01f, 0.004f, 0f), joints + new Vector3(0.03f, 0.004f, 0f),
                joints + new Vector3(-0.035f, 0f, 0.08f), joints + new Vector3(-0.01f, 0f, 0.09f),
                joints + new Vector3(0.015f, 0f, 0.085f), joints + new Vector3(0.035f, tilt, 0.07f),
            };
            return new HandSample { Valid = true, Contacts = contacts };
        }
    }
}
