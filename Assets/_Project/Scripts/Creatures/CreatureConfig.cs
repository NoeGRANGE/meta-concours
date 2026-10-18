using UnityEngine;

namespace Deskmates.Creatures
{
    /// <summary>
    /// Per-creature tuning (PRD 9.2). Defaults follow PRD 4.3, 4.5 and 10.
    /// </summary>
    [CreateAssetMenu(menuName = "Deskmates/Creature Config", fileName = "CreatureConfig")]
    public sealed class CreatureConfig : ScriptableObject
    {
        [Header("Colour")]
        [Tooltip("Body colour at rest (PRD 4.2).")]
        public Color restColor = new Color32(0xEF, 0xE6, 0xD8, 0xFF);

        [Tooltip("Shared alert ramp, reached at stress 0.3, 0.6 and 1 (PRD 4.3).")]
        public Color alertColor = new Color32(0xF2, 0xD3, 0xAE, 0xFF);
        public Color stressedColor = new Color32(0xF1, 0xA6, 0x61, 0xFF);
        public Color panicColor = new Color32(0xE2, 0x55, 0x3A, 0xFF);

        [Tooltip("Emission added on top of the body colour at full panic.")]
        [Range(0f, 1f)] public float panicEmission = 0.25f;

        [Header("Stress")]
        [Tooltip("SmoothDamp time in seconds; the creature settles in roughly three times this.")]
        public float smoothTime = 0.5f;

        [Tooltip("Thresholds between calm/alert, alert/stressed and stressed/panic.")]
        public Vector3 expressionThresholds = new Vector3(0.15f, 0.45f, 0.8f);

        [Tooltip("Width of the band around each threshold where the face keeps its current expression.")]
        public float hysteresis = 0.05f;

        [Tooltip("Relief plays when stress drops below Relief Low after having been above Relief High (PRD 4.5).")]
        public float reliefHigh = 0.6f;
        public float reliefLow = 0.15f;

        [Header("Scale at calm, alert, stressed, panic")]
        public Vector4 stateScales = new Vector4(1f, 1.1f, 1.25f, 1.4f);

        [Header("Motion")]
        [Tooltip("Squash and stretch of the breathing, as a fraction of the height.")]
        public float breathAmplitude = 0.025f;
        public float calmBreathPeriod = 4f;
        public float stressedBreathPeriod = 1.2f;

        [Tooltip("Height of the small hops when stressed, in metres.")]
        public float hopHeight = 0.006f;
        public float hopsPerSecond = 1.6f;

        [Tooltip("Amplitude of the panic tremble, in metres.")]
        public float shakeAmplitude = 0.0015f;
        public float shakeFrequency = 22f;

        [Header("Face")]
        [Tooltip("Random delay between two blinks, in seconds.")]
        public Vector2 blinkInterval = new Vector2(2.5f, 6f);
        public float blinkDuration = 0.12f;

        [Header("Relief")]
        public float reliefDuration = 1.3f;
    }
}
