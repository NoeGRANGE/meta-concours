using System.Collections.Generic;
using UnityEngine;

namespace Deskmates.Desk
{
    /// <summary>One hand as the calibration sees it.</summary>
    public struct HandSample
    {
        /// <summary>Tracked with high confidence this frame.</summary>
        public bool Valid;

        /// <summary>Fingertips, knuckles and wrist, in world space.</summary>
        public Vector3[] Contacts;
    }

    /// <summary>The desk height measured under the resting hands, and where those hands are.</summary>
    public readonly struct DeskMeasurement
    {
        public DeskMeasurement(float height, IReadOnlyList<Vector3> handCenters)
        {
            Height = height;
            HandCenters = handCenters;
        }

        public float Height { get; }

        /// <summary>Centre of each resting hand, on the surface.</summary>
        public IReadOnlyList<Vector3> HandCenters { get; }
    }

    /// <summary>
    /// Watches both hands and reports when one or two have rested flat and still on a surface long enough
    /// (PRD 5.1). A plain class so tests can feed it made-up hands.
    /// </summary>
    public sealed class FlatHandDetector
    {
        readonly DeskCalibrationConfig config;
        readonly HandState[] hands = { new HandState(), new HandState() };

        public FlatHandDetector(DeskCalibrationConfig config)
        {
            this.config = config;
        }

        public void Reset()
        {
            foreach (HandState hand in hands)
            {
                hand.Clear();
            }
        }

        /// <summary>
        /// Feeds one frame. Returns true once a hand has rested long enough; a second hand resting at the same time
        /// joins the measurement.
        /// </summary>
        public bool Step(float deltaTime, Vector3 eyes, HandSample left, HandSample right, out DeskMeasurement measurement)
        {
            bool ready = Track(hands[0], left, eyes, deltaTime) | Track(hands[1], right, eyes, deltaTime);
            measurement = default;
            if (!ready)
            {
                return false;
            }

            List<Vector3> centers = new List<Vector3>(2);
            float heightSum = 0f;
            foreach (HandState hand in hands)
            {
                if (hand.Resting)
                {
                    centers.Add(hand.Center);
                    heightSum += hand.Height;
                }
            }

            float height = heightSum / centers.Count;
            for (int i = 0; i < centers.Count; i++)
            {
                centers[i] = new Vector3(centers[i].x, height, centers[i].z);
            }

            measurement = new DeskMeasurement(height, centers);
            return true;
        }

        bool Track(HandState hand, HandSample sample, Vector3 eyes, float deltaTime)
        {
            if (!sample.Valid || sample.Contacts == null || sample.Contacts.Length == 0)
            {
                hand.Clear();
                return false;
            }

            float lowest = float.MaxValue;
            float highest = float.MinValue;
            Vector3 sum = Vector3.zero;
            foreach (Vector3 point in sample.Contacts)
            {
                lowest = Mathf.Min(lowest, point.y);
                highest = Mathf.Max(highest, point.y);
                sum += point;
            }

            Vector3 center = sum / sample.Contacts.Length;
            bool flat = highest - lowest <= config.flatTolerance;
            bool belowEyes = eyes.y - center.y >= config.minDropBelowEyes;
            if (!flat || !belowEyes)
            {
                hand.Clear();
                return false;
            }

            // Hand tracking jitters by a few millimetres per frame, so stillness is measured as a drift
            // from where the rest began rather than as a speed.
            if (!hand.Resting || Vector3.Distance(center, hand.RestStart) > config.stillRadius)
            {
                hand.Resting = true;
                hand.RestStart = center;
                hand.HeldFor = 0f;
            }
            else
            {
                hand.HeldFor += deltaTime;
            }

            hand.Height = lowest - config.handThickness;
            hand.Center = center;
            return hand.HeldFor >= config.holdSeconds;
        }

        sealed class HandState
        {
            public bool Resting;
            public Vector3 RestStart;
            public float HeldFor;
            public float Height;
            public Vector3 Center;

            public void Clear()
            {
                Resting = false;
                HeldFor = 0f;
            }
        }
    }
}
