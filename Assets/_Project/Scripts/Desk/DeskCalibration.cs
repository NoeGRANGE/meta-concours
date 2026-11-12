using System;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Deskmates.Desk
{
    /// <summary>
    /// Measures the desk from the user's hand laid flat on it (PRD 5.1). Listens to the hands only between
    /// <see cref="Begin"/> and the end of the calibration, never in the background.
    /// </summary>
    public sealed class DeskCalibration : MonoBehaviour
    {
        [SerializeField] DeskCalibrationConfig config;
        [SerializeField] Transform head;
        [SerializeField] DeskRipple ripple;

        readonly IsdkHandSampler hands = new IsdkHandSampler();
        FlatHandDetector detector;

        public event Action<DeskPlane> Completed;
        public event Action Cancelled;

        public bool IsCalibrating { get; private set; }

        public Transform Head => head;

        void Awake()
        {
            if (config == null)
            {
                Debug.LogWarning("[Deskmates] DeskCalibration has no config; using defaults.", this);
                config = ScriptableObject.CreateInstance<DeskCalibrationConfig>();
            }

            detector = new FlatHandDetector(config);
        }

        public void Begin()
        {
            detector.Reset();
            IsCalibrating = true;
        }

        public void Cancel()
        {
            if (!IsCalibrating)
            {
                return;
            }

            IsCalibrating = false;
            Cancelled?.Invoke();
        }

        /// <summary>Ends the current calibration with a known measurement. Editor shortcuts and tests use it.</summary>
        public void Complete(DeskMeasurement measurement)
        {
            if (!IsCalibrating)
            {
                return;
            }

            IsCalibrating = false;
            DeskPlane plane = BuildPlane(measurement);
            if (ripple != null)
            {
                ripple.Play(plane);
            }

            Completed?.Invoke(plane);
        }

        void Update()
        {
            if (!IsCalibrating)
            {
                return;
            }

            hands.Sample(out HandSample left, out HandSample right);
            if (detector.Step(Time.deltaTime, head.position, left, right, out DeskMeasurement measurement))
            {
                Complete(measurement);
            }
        }

        DeskPlane BuildPlane(DeskMeasurement measurement)
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            float yaw = forward.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(forward).eulerAngles.y : 0f;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);

            Vector3 center = Vector3.zero;
            foreach (Vector3 hand in measurement.HandCenters)
            {
                center += hand;
            }

            center /= measurement.HandCenters.Count;
            center.y = SnapToScannedTable(measurement.Height, center);

            Vector2 halfSize = config.oneHandZoneSize * 0.5f;
            if (measurement.HandCenters.Count > 1)
            {
                // The rectangle around both hands, plus a margin, when it is larger than the one-hand zone.
                Vector3 spread = Quaternion.Inverse(rotation) * (measurement.HandCenters[1] - measurement.HandCenters[0]);
                halfSize.x = Mathf.Max(halfSize.x, Mathf.Abs(spread.x) * 0.5f + config.twoHandMargin);
                halfSize.y = Mathf.Max(halfSize.y, Mathf.Abs(spread.z) * 0.5f + config.twoHandMargin);
            }

            return new DeskPlane(center, yaw, halfSize);
        }

        // A scanned table under the hand at about the same height is more precise than the hand itself.
        float SnapToScannedTable(float height, Vector3 point)
        {
            MRUKRoom room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
            if (room == null)
            {
                return height;
            }

            foreach (MRUKAnchor anchor in room.Anchors)
            {
                if (!anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE) || !anchor.IsPositionInVolume(point, false))
                {
                    continue;
                }

                float top = TopHeight(anchor);
                if (Mathf.Abs(top - height) <= config.tableSnapDistance)
                {
                    return top;
                }
            }

            return height;
        }

        static float TopHeight(MRUKAnchor anchor)
        {
            if (!anchor.VolumeBounds.HasValue)
            {
                return anchor.transform.position.y;
            }

            Bounds volume = anchor.VolumeBounds.Value;
            float top = float.MinValue;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = new Vector3(
                    (corner & 1) == 0 ? volume.min.x : volume.max.x,
                    (corner & 2) == 0 ? volume.min.y : volume.max.y,
                    (corner & 4) == 0 ? volume.min.z : volume.max.z);
                top = Mathf.Max(top, anchor.transform.TransformPoint(local).y);
            }

            return top;
        }

        /// <summary>A measurement for a single hand resting at <paramref name="point"/>.</summary>
        public static DeskMeasurement MeasurementAt(Vector3 point)
        {
            return new DeskMeasurement(point.y, new List<Vector3> { point });
        }
    }
}
