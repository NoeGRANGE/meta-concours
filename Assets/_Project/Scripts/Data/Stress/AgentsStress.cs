using System;
using UnityEngine;

namespace Deskmates.Data.Stress
{
    /// <summary>
    /// Agents (PRD 4.4): sessions waiting for the user's reply or permission weigh most, then failed sessions, then
    /// finished ones not looked at yet, plus how long the oldest has been waiting. Running sessions are calm.
    /// </summary>
    [CreateAssetMenu(menuName = "Deskmates/Stress/Agents", fileName = "AgentsStress")]
    public sealed class AgentsStress : StressCalculator
    {
        public float perWaiting = 0.4f;
        public float perFailed = 0.3f;
        public float perFinishedUnseen = 0.15f;
        public float perFiveMinutesOldestWaiting = 0.1f;

        public override float Evaluate(SourceSnapshot snapshot, DateTime now)
        {
            int waiting = 0;
            int failed = 0;
            int finished = 0;
            double minutesOldestWaiting = 0d;
            foreach (SourceItem item in snapshot.Items)
            {
                switch (item.State)
                {
                    case AgentStates.Waiting:
                        waiting++;
                        minutesOldestWaiting = Math.Max(minutesOldestWaiting, (now - item.Time).TotalMinutes);
                        break;
                    case AgentStates.Failed:
                        failed++;
                        break;
                    case AgentStates.Done:
                        finished++;
                        break;
                }
            }

            float waited = perFiveMinutesOldestWaiting * (float)minutesOldestWaiting / 5f;
            return Mathf.Clamp01(perWaiting * waiting + perFailed * failed + perFinishedUnseen * finished + waited);
        }
    }
}
