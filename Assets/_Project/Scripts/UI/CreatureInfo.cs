using Deskmates.Data;
using Deskmates.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace Deskmates.UI
{
    /// <summary>
    /// Makes a creature answer the user's gaze: a peek bubble after a short look, the detail panel on a pinch (PRD 5).
    /// </summary>
    [RequireComponent(typeof(CreatureDataLink))]
    public sealed class CreatureInfo : MonoBehaviour, IGazeTarget
    {
        [Tooltip("World-space canvas above the creature, hidden until the user looks at it.")]
        [SerializeField] Canvas peek;

        [SerializeField] Text peekText;

        [Tooltip("The creature's own colour, used for the accent of its panel.")]
        [SerializeField] Color accent = new Color32(0xF1, 0xA6, 0x61, 0xFF);

        CreatureDataLink link;

        public string SourceId => link.SourceId;

        public Color Accent => accent;

        void Awake()
        {
            link = GetComponent<CreatureDataLink>();
            peek.gameObject.SetActive(false);
        }

        public void OnGazeDwell() => peek.gameObject.SetActive(true);

        public void OnGazeExit() => peek.gameObject.SetActive(false);

        public void OnPinch()
        {
            peek.gameObject.SetActive(false);
            DetailPanel panel = DetailPanel.Instance;
            if (panel == null)
            {
                return;
            }

            // Pinching the creature again closes its panel.
            if (panel.Creature == this)
            {
                panel.Close();
            }
            else
            {
                panel.Open(this);
            }
        }

        void LateUpdate()
        {
            if (!peek.gameObject.activeSelf)
            {
                return;
            }

            DataHub hub = DataHub.Instance;
            IDataSource source = hub != null ? hub.GetSource(SourceId) : null;
            peekText.text = source != null ? ItemText.Summary(SourceId, source.Current, hub.Clock.Now) : "";
            Billboard.Face(peek.transform);
        }
    }

    static class Billboard
    {
        /// <summary>Turns a world-space canvas to face the user, upright.</summary>
        public static void Face(Transform canvas)
        {
            Camera eyes = Camera.main;
            if (eyes == null)
            {
                return;
            }

            Vector3 away = Vector3.ProjectOnPlane(canvas.position - eyes.transform.position, Vector3.up);
            if (away.sqrMagnitude > 1e-4f)
            {
                canvas.rotation = Quaternion.LookRotation(away, Vector3.up);
            }
        }
    }
}
