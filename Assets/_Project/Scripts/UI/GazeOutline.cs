using System.Linq;
using UnityEngine;

namespace Deskmates.UI
{
    /// <summary>
    /// A soft rim in the creature's colour while the user looks at it, like the outline of tracked hands, so the
    /// user knows what a pinch will select (PRD 5). It follows the body's squash, stretch and stress scaling.
    /// </summary>
    [RequireComponent(typeof(CreatureInfo))]
    public sealed class GazeOutline : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [Tooltip("Material using the Fretlings/GazeOutline shader.")]
        [SerializeField] Material material;

        [SerializeField] float fadeSeconds = 0.15f;
        [SerializeField, Range(0f, 1f)] float opacity = 0.9f;

        MeshRenderer outline;
        MaterialPropertyBlock block;
        Color color;
        float shown;
        float target;

        public bool IsShown => outline != null && outline.enabled;

        void Awake()
        {
            color = GetComponent<CreatureInfo>().Accent;
            block = new MaterialPropertyBlock();
            MeshRenderer body = GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name.EndsWith("_Body"));
            MeshFilter mesh = body != null ? body.GetComponent<MeshFilter>() : null;
            if (mesh == null || material == null)
            {
                return;
            }

            GameObject rim = new GameObject("GazeOutline");
            rim.transform.SetParent(body.transform, false);
            rim.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            outline = rim.AddComponent<MeshRenderer>();
            outline.sharedMaterial = material;
            outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outline.receiveShadows = false;
            outline.enabled = false;
        }

        public void Show(bool on) => target = on ? 1f : 0f;

        void Update()
        {
            if (outline == null || Mathf.Approximately(shown, target))
            {
                return;
            }

            shown = Mathf.MoveTowards(shown, target, Time.deltaTime / fadeSeconds);
            outline.enabled = shown > 0f;
            Color faded = color;
            faded.a = opacity * shown;
            outline.GetPropertyBlock(block);
            block.SetColor(ColorId, faded);
            outline.SetPropertyBlock(block);
        }
    }
}
