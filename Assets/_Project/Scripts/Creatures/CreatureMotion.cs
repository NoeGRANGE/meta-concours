using UnityEngine;

namespace Deskmates.Creatures
{
    /// <summary>
    /// Procedural motion of the model (PRD 4.3): breathing, size per state, hops when stressed,
    /// trembling in panic, and the deflate-and-bounce of the relief (PRD 4.5).
    /// Only the model under the root moves, so placement and grabbing never fight the animation.
    /// </summary>
    public sealed class CreatureMotion : MonoBehaviour
    {
        // Relief: deflate during the first part, then spring back with a damped overshoot.
        const float DeflatePortion = 0.3f;
        const float DeflateHeight = 0.15f;
        const float DeflateWidth = 0.06f;

        [Tooltip("The model under the creature root. Its pivot sits at the base, so scaling keeps it on the desk.")]
        [SerializeField] Transform visual;

        Vector3 basePosition;
        Quaternion baseRotation;
        Vector3 baseScale;
        float breathPhase;
        float hopPhase;
        float shakeTime;
        float noiseSeed;
        float reliefTime = -1f;

        public bool IsPlayingRelief => reliefTime >= 0f;

        void Awake()
        {
            basePosition = visual.localPosition;
            baseRotation = visual.localRotation;
            baseScale = visual.localScale;
            noiseSeed = Random.value * 100f;
        }

        public void PlayRelief()
        {
            reliefTime = 0f;
        }

        public void Tick(float deltaTime, float stress, CreatureConfig config)
        {
            float breathPeriod = Mathf.Lerp(config.calmBreathPeriod, config.stressedBreathPeriod,
                Mathf.InverseLerp(0f, 0.6f, stress));
            breathPhase = Mathf.Repeat(breathPhase + deltaTime / breathPeriod, 1f);
            float breath = Mathf.Sin(breathPhase * 2f * Mathf.PI) * config.breathAmplitude;

            float hopWeight = StressRamp.Weight(0.45f, 0.6f, stress) * (1f - StressRamp.Weight(0.8f, 0.95f, stress));
            hopPhase = Mathf.Repeat(hopPhase + deltaTime * config.hopsPerSecond, 1f);
            float hop = Mathf.Abs(Mathf.Sin(hopPhase * Mathf.PI)) * config.hopHeight * hopWeight;

            float shakeWeight = StressRamp.Weight(0.8f, 1f, stress);
            shakeTime += deltaTime * config.shakeFrequency;
            Vector3 shake = new Vector3(Noise(shakeTime, 0f), 0f, Noise(shakeTime, 10f)) * (config.shakeAmplitude * shakeWeight);

            ReliefSquash(deltaTime, config, out float reliefHeight, out float reliefWidth);
            float height = (1f + breath) * reliefHeight;
            float width = (1f - breath * 0.5f) * reliefWidth;
            float stateScale = StressRamp.Evaluate(stress, config.stateScales);

            visual.localScale = Vector3.Scale(baseScale, new Vector3(width, height, width)) * stateScale;
            visual.localPosition = basePosition + Vector3.up * hop + shake;
            visual.localRotation = baseRotation;
        }

        void ReliefSquash(float deltaTime, CreatureConfig config, out float height, out float width)
        {
            height = 1f;
            width = 1f;
            if (reliefTime < 0f)
            {
                return;
            }

            reliefTime += deltaTime;
            float progress = reliefTime / config.reliefDuration;
            if (progress >= 1f)
            {
                reliefTime = -1f;
                return;
            }

            float squash;
            if (progress < DeflatePortion)
            {
                float t = progress / DeflatePortion;
                squash = 1f - (1f - t) * (1f - t);
            }
            else
            {
                float t = (progress - DeflatePortion) / (1f - DeflatePortion);
                squash = Mathf.Exp(-5f * t) * Mathf.Cos(3f * Mathf.PI * t);
            }

            height = 1f - DeflateHeight * squash;
            width = 1f + DeflateWidth * squash;
        }

        float Noise(float time, float offset)
        {
            return (Mathf.PerlinNoise(time, noiseSeed + offset) - 0.5f) * 2f;
        }
    }
}
