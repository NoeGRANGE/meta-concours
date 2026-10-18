using System;
using UnityEngine;

namespace Deskmates.Data.Stress
{
    /// <summary>Messages: direct messages or mentions waiting for an answer, and how long the oldest has waited.</summary>
    [CreateAssetMenu(menuName = "Deskmates/Stress/Messages", fileName = "MessagesStress")]
    public sealed class MessagesStress : StressCalculator
    {
        public float perPending = 0.2f;
        public float perTenMinutesOldest = 0.15f;

        public override float Evaluate(SourceSnapshot snapshot, DateTime now)
        {
            double minutesOldest = 0d;
            foreach (SourceItem item in snapshot.Items)
            {
                minutesOldest = Math.Max(minutesOldest, (now - item.Time).TotalMinutes);
            }

            float waited = perTenMinutesOldest * (float)minutesOldest / 10f;
            return Mathf.Clamp01(perPending * snapshot.Items.Count + waited);
        }
    }
}
