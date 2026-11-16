using Deskmates.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace Deskmates.UI
{
    /// <summary>One line of the detail panel: highlights when looked at, acts on a pinch.</summary>
    public sealed class PanelRow : MonoBehaviour, IGazeTarget
    {
        // The creatures' cream at rest, and their "attentive" peach when looked at (PRD 4.3).
        public static readonly Color Idle = new Color32(0xEF, 0xE6, 0xD8, 0xFF);
        public static readonly Color Focused = new Color32(0xF2, 0xD3, 0xAE, 0xFF);

        [SerializeField] Text label;
        [SerializeField] Image background;

        DetailPanel panel;
        int index;

        public void Bind(DetailPanel owner, int row)
        {
            panel = owner;
            index = row;
        }

        public void Show(string text)
        {
            gameObject.SetActive(text != null);
            label.text = text ?? "";
        }

        public void OnGazeDwell() => background.color = Focused;

        public void OnGazeExit() => background.color = Idle;

        public void OnPinch() => panel.Resolve(index);
    }
}
