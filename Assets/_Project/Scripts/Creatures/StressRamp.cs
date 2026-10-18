using UnityEngine;

namespace Deskmates.Creatures
{
    /// <summary>
    /// Interpolates a value defined at the four reference states of PRD 4.3 (stress 0, 0.3, 0.6 and 1).
    /// </summary>
    public static class StressRamp
    {
        static readonly float[] Stops = { 0f, 0.3f, 0.6f, 1f };

        public static float Evaluate(float stress, Vector4 values)
        {
            Segment(stress, out int index, out float t);
            return Mathf.Lerp(values[index], values[index + 1], t);
        }

        public static Color Evaluate(float stress, Color calm, Color alert, Color stressed, Color panic)
        {
            Segment(stress, out int index, out float t);
            Color from = index == 0 ? calm : index == 1 ? alert : stressed;
            Color to = index == 0 ? alert : index == 1 ? stressed : panic;
            return Color.Lerp(from, to, t);
        }

        /// <summary>0 below <paramref name="from"/>, 1 above <paramref name="to"/>, eased in between.</summary>
        public static float Weight(float from, float to, float stress)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, stress));
        }

        static void Segment(float stress, out int index, out float t)
        {
            stress = Mathf.Clamp01(stress);
            index = stress < Stops[1] ? 0 : stress < Stops[2] ? 1 : 2;
            t = Mathf.InverseLerp(Stops[index], Stops[index + 1], stress);
        }
    }
}
