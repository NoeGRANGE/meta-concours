using UnityEngine;

namespace Deskmates.UI
{
    /// <summary>
    /// Grows a card in with a small overshoot each time it is shown, like the creatures' soft bounces, instead of
    /// popping into existence.
    /// </summary>
    public sealed class PopIn : MonoBehaviour
    {
        [SerializeField] float duration = 0.28f;
        [SerializeField] float startScale = 0.6f;

        Vector3 baseScale;
        float elapsed;

        void Awake()
        {
            baseScale = transform.localScale;
        }

        void OnEnable()
        {
            elapsed = 0f;
            transform.localScale = baseScale * startScale;
        }

        void Update()
        {
            if (elapsed >= duration)
            {
                return;
            }

            elapsed += Time.deltaTime;
            transform.localScale = baseScale * Mathf.LerpUnclamped(startScale, 1f, EaseOutBack(Mathf.Clamp01(elapsed / duration)));
        }

        static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float u = t - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }
    }
}
