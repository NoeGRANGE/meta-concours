using System;
using UnityEngine;

namespace Deskmates.Data.Stress
{
    /// <summary>Mail: important unread mails and how long the oldest one has been waiting.</summary>
    [CreateAssetMenu(menuName = "Deskmates/Stress/Mail", fileName = "MailStress")]
    public sealed class MailStress : StressCalculator
    {
        public float perImportantUnread = 0.18f;
        public float perHourOldestImportant = 0.10f;

        public override float Evaluate(SourceSnapshot snapshot, DateTime now)
        {
            int importantUnread = 0;
            double hoursOldest = 0d;
            foreach (SourceItem item in snapshot.Items)
            {
                if (!item.Important)
                {
                    continue;
                }

                importantUnread++;
                hoursOldest = Math.Max(hoursOldest, (now - item.Time).TotalHours);
            }

            return Mathf.Clamp01(perImportantUnread * importantUnread + perHourOldestImportant * (float)hoursOldest);
        }
    }
}
