using System;
using UnityEngine;

namespace Deskmates.Data.Stress
{
    /// <summary>
    /// Turns what a source holds into a creature's stress in [0, 1] (PRD 4.4).
    /// One asset per creature, so the constants can be tuned in the inspector.
    /// </summary>
    public abstract class StressCalculator : ScriptableObject
    {
        public abstract float Evaluate(SourceSnapshot snapshot, DateTime now);
    }
}
