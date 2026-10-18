using UnityEngine;

namespace Deskmates.Creatures
{
    /// <summary>
    /// Smooths the target stress, picks the expression with hysteresis and detects the relief moment (PRD 4.3–4.5).
    /// A plain class so tests can step it with a fixed time step.
    /// </summary>
    public sealed class StressSmoother
    {
        readonly CreatureConfig config;
        float target;
        float velocity;
        bool reliefArmed;

        public StressSmoother(CreatureConfig config)
        {
            this.config = config;
        }

        public float Target
        {
            get => target;
            set => target = Mathf.Clamp01(value);
        }

        public float Value { get; private set; }

        public CreatureExpression Expression { get; private set; }

        /// <summary>Advances the smoothing. Returns true on the step where the relief should play.</summary>
        public bool Step(float deltaTime)
        {
            if (deltaTime > 0f)
            {
                Value = Mathf.Clamp01(Mathf.SmoothDamp(Value, target, ref velocity, config.smoothTime,
                    Mathf.Infinity, deltaTime));
            }

            Expression = NextExpression(Expression, Value);

            if (Value > config.reliefHigh)
            {
                reliefArmed = true;
            }
            else if (reliefArmed && Value < config.reliefLow)
            {
                reliefArmed = false;
                return true;
            }

            return false;
        }

        // A threshold only flips the expression once stress is half the hysteresis band past it,
        // so a value hovering on a threshold never makes the face flicker.
        CreatureExpression NextExpression(CreatureExpression current, float stress)
        {
            float half = config.hysteresis * 0.5f;
            int level = (int)current;
            while (level < 3 && stress >= config.expressionThresholds[level] + half)
            {
                level++;
            }

            while (level > 0 && stress <= config.expressionThresholds[level - 1] - half)
            {
                level--;
            }

            return (CreatureExpression)level;
        }
    }
}
