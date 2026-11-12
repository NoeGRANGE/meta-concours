using UnityEngine;

namespace Deskmates.Desk
{
    /// <summary>
    /// Tuning of the hand-on-desk calibration (PRD 5.1).
    /// </summary>
    [CreateAssetMenu(menuName = "Deskmates/Desk Calibration Config", fileName = "DeskCalibrationConfig")]
    public sealed class DeskCalibrationConfig : ScriptableObject
    {
        [Header("Flat hand")]
        [Tooltip("Largest height difference between the fingertips, knuckles and wrist of a flat hand, in metres.")]
        public float flatTolerance = 0.025f;

        [Tooltip("How far a resting hand may drift while it holds still, in metres.")]
        public float stillRadius = 0.015f;

        [Tooltip("How long a hand must stay flat and still, in seconds.")]
        public float holdSeconds = 0.6f;

        [Tooltip("Minimum height between the eyes and the hand, in metres.")]
        public float minDropBelowEyes = 0.25f;

        [Tooltip("Distance from the lowest tracked joints of a flat hand down to the surface, in metres.")]
        public float handThickness = 0.008f;

        [Header("Zone")]
        [Tooltip("Width and depth of the zone around one hand, in metres.")]
        public Vector2 oneHandZoneSize = new Vector2(1.2f, 0.7f);

        [Tooltip("Margin around two hands when their rectangle is larger than the one-hand zone, in metres.")]
        public float twoHandMargin = 0.15f;

        [Tooltip("A scanned table this close to the measured height gives the height instead, in metres.")]
        public float tableSnapDistance = 0.03f;
    }
}
