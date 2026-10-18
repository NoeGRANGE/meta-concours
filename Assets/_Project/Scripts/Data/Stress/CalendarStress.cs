using System;
using UnityEngine;

namespace Deskmates.Data.Stress
{
    /// <summary>
    /// Meetings: nothing until the next one is an hour away, then a linear rise, full stress in the last minutes,
    /// and back to zero once it has started.
    /// </summary>
    [CreateAssetMenu(menuName = "Deskmates/Stress/Calendar", fileName = "CalendarStress")]
    public sealed class CalendarStress : StressCalculator
    {
        [Tooltip("Minutes before a meeting when the creature starts to worry.")]
        public float rampStartMinutes = 60f;

        [Tooltip("Minutes before a meeting when the ramp reaches Ramp End Stress.")]
        public float rampEndMinutes = 5f;

        public float rampEndStress = 0.9f;

        [Tooltip("Minutes before a meeting from which stress is at its maximum.")]
        public float imminentMinutes = 2f;

        public override float Evaluate(SourceSnapshot snapshot, DateTime now)
        {
            // Only meetings that have not started count.
            double minutes = double.MaxValue;
            foreach (SourceItem item in snapshot.Items)
            {
                double until = (item.Time - now).TotalMinutes;
                if (until > 0d && until < minutes)
                {
                    minutes = until;
                }
            }

            if (minutes > rampStartMinutes)
            {
                return 0f;
            }

            float left = (float)minutes;
            if (left <= imminentMinutes)
            {
                return 1f;
            }

            if (left >= rampEndMinutes)
            {
                return Mathf.Lerp(0f, rampEndStress, Mathf.InverseLerp(rampStartMinutes, rampEndMinutes, left));
            }

            return Mathf.Lerp(rampEndStress, 1f, Mathf.InverseLerp(rampEndMinutes, imminentMinutes, left));
        }
    }
}
