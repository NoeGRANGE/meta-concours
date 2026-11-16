using Deskmates.Core;
using UnityEngine;

namespace Deskmates.Desk
{
    /// <summary>
    /// The light ring that spreads over the desk when a calibration succeeds, so the user sees whether the
    /// measured plane matches their real desk (PRD 5.1).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class DeskRipple : MonoBehaviour
    {
        const int Segments = 64;

        [SerializeField] float duration = 1.2f;
        [SerializeField] float maxRadius = 0.3f;
        [SerializeField] float width = 0.006f;
        [SerializeField] Color color = new Color(0.85f, 0.95f, 1f, 0.9f);

        readonly Vector3[] points = new Vector3[Segments];
        LineRenderer line;
        Vector3 center;
        float time = -1f;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = true;
            line.positionCount = Segments;
            line.enabled = false;
        }

        public void Play(DeskPlane plane)
        {
            // Just above the surface so the real desk does not hide it.
            center = plane.Center + Vector3.up * 0.002f;
            SoundBank.Play(Sound.Ripple, center);
            time = 0f;
            line.enabled = true;
        }

        void Update()
        {
            if (time < 0f)
            {
                return;
            }

            time += Time.deltaTime;
            float progress = time / duration;
            if (progress >= 1f)
            {
                time = -1f;
                line.enabled = false;
                return;
            }

            float radius = maxRadius * (1f - (1f - progress) * (1f - progress));
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * 2f * Mathf.PI / Segments;
                points[i] = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            }

            line.SetPositions(points);
            line.widthMultiplier = width;
            Color faded = color;
            faded.a *= 1f - progress;
            line.startColor = faded;
            line.endColor = faded;
        }
    }
}
