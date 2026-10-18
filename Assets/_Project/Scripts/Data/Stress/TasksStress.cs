using System;
using UnityEngine;

namespace Deskmates.Data.Stress
{
    /// <summary>Tasks: overdue ones weigh most, then those still due today.</summary>
    [CreateAssetMenu(menuName = "Deskmates/Stress/Tasks", fileName = "TasksStress")]
    public sealed class TasksStress : StressCalculator
    {
        public float perOverdue = 0.35f;
        public float perDueToday = 0.15f;

        public override float Evaluate(SourceSnapshot snapshot, DateTime now)
        {
            int overdue = 0;
            int dueToday = 0;
            foreach (SourceItem item in snapshot.Items)
            {
                if (item.Time < now)
                {
                    overdue++;
                }
                else if (item.Time.Date == now.Date)
                {
                    dueToday++;
                }
            }

            return Mathf.Clamp01(perOverdue * overdue + perDueToday * dueToday);
        }
    }
}
