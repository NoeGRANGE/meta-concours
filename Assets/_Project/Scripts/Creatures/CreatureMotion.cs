using UnityEngine;

namespace Deskmates.Creatures
{
    /// <summary>
    /// Procedural motion of the model (PRD 4.3): breathing, size per state, hops when stressed,
    /// trembling in panic, the deflate-and-bounce of the relief (PRD 4.5) and the squash of a landing (PRD 5.1).
    /// Only the model under the root moves, so placement and grabbing never fight the animation.
    /// </summary>
    public sealed class CreatureMotion : MonoBehaviour
    {
        // Relief: deflate during the first part, then spring back with a damped overshoot.
        const float DeflatePortion = 0.3f;
        const float DeflateHeight = 0.15f;
        const float DeflateWidth = 0.06f;

        // Landing: squashed on impact, then a damped wobble back to shape.
        const float LandingDuration = 0.4f;
        const float LandingHeight = 0.25f;
        const float LandingWidth = 0.12f;

        // Impact speed, in metres per second, that gives the strongest squash.
        const float HardestImpact = 2f;

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
        float landingTime = -1f;
        float landingStrength;

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

        /// <summary>Squashes the creature as it hits the desk; harder impacts squash more.</summary>
        public void PlayLanding(float impactSpeed)
        {
            landingStrength = Mathf.Clamp01(impactSpeed / HardestImpact);
            landingTime = 0f;
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
            LandingSquash(deltaTime, out float landingHeight, out float landingWidth);
            float height = (1f + breath) * reliefHeight * landingHeight;
            float width = (1f - breath * 0.5f) * reliefWidth * landingWidth;
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

        void LandingSquash(float deltaTime, out float height, out float width)
        {
            height = 1f;
            width = 1f;
            if (landingTime < 0f)
            {
                return;
            }

            landingTime += deltaTime;
            float progress = landingTime / LandingDuration;
            if (progress >= 1f)
            {
                landingTime = -1f;
                return;
            }

            float squash = landingStrength * Mathf.Exp(-4f * progress) * Mathf.Cos(3f * Mathf.PI * progress);
            height = 1f - LandingHeight * squash;
            width = 1f + LandingWidth * squash;
        }

        float Noise(float time, float offset)
        {
            return (Mathf.PerlinNoise(time, noiseSeed + offset) - 0.5f) * 2f;
        }
    }
}
