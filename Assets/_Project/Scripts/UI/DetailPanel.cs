using System.Collections.Generic;
using Deskmates.Data;
using Deskmates.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace Deskmates.UI
{
    /// <summary>
    /// The small panel of a creature's three most urgent items (PRD 5). Look at a line and pinch to mark it read or
    /// done; looking away for a few seconds closes it.
    /// </summary>
    public sealed class DetailPanel : MonoBehaviour
    {
        [SerializeField] GazeSelector gaze;
        [SerializeField] Canvas canvas;
        [SerializeField] Text title;
        [SerializeField] Text hint;
        [SerializeField] PanelRow[] rows;

        [Tooltip("Seconds without looking at the panel or its creature before the panel closes.")]
        [SerializeField] float closeAfter = 5f;

        [Tooltip("Where the panel sits relative to the creature, in metres.")]
        [SerializeField] Vector3 offset = new Vector3(0f, 0.17f, 0f);

        readonly List<SourceItem> shown = new();
        float lookedAwayFor;

        public static DetailPanel Instance { get; private set; }

        public CreatureInfo Creature { get; private set; }

        public IReadOnlyList<SourceItem> Shown => shown;

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].Bind(this, i);
            }

            Close();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Open(CreatureInfo creature)
        {
            Creature = creature;
            lookedAwayFor = 0f;
            canvas.gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            Creature = null;
            canvas.gameObject.SetActive(false);
        }

        /// <summary>Marks the item on a row read or done, through the creature's data source.</summary>
        public async void Resolve(int row)
        {
            ItemAction? action = Creature != null ? ItemText.Action(Creature.SourceId) : null;
            if (action == null || row >= shown.Count || !TryGetSource(out IDataSource source))
            {
                return;
            }

            await source.PerformAsync(action.Value, shown[row].Id);
            Refresh();
        }

        void LateUpdate()
        {
            if (Creature == null)
            {
                return;
            }

            // Never closes while the user looks at it: only after a while looking elsewhere, or a pinch on the creature.
            IGazeTarget looked = gaze != null ? gaze.Hovered : null;
            bool onPanel = looked is PanelRow || ReferenceEquals(looked, Creature);
            lookedAwayFor = onPanel ? 0f : lookedAwayFor + Time.deltaTime;
            if (lookedAwayFor >= closeAfter)
            {
                Close();
                return;
            }

            canvas.transform.position = Creature.transform.position + offset;
            Billboard.Face(canvas.transform);
            Refresh();
        }

        void Refresh()
        {
            shown.Clear();
            if (Creature == null || !TryGetSource(out IDataSource source))
            {
                return;
            }

            System.DateTime now = DataHub.Instance.Clock.Now;
            shown.AddRange(ItemText.Top(Creature.SourceId, source.Current, now, rows.Length));
            title.text = ItemText.Summary(Creature.SourceId, source.Current, now);
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].Show(i < shown.Count ? ItemText.Row(Creature.SourceId, shown[i], now) : null);
            }

            hint.text = shown.Count == 0 ? "All clear!"
                : ItemText.Action(Creature.SourceId) == ItemAction.MarkDone ? "Pinch a line to mark it done · pinch me to close"
                : ItemText.Action(Creature.SourceId) == null ? "Pinch me to close"
                : "Pinch a line to mark it read · pinch me to close";
        }

        bool TryGetSource(out IDataSource source)
        {
            source = DataHub.Instance != null && Creature != null ? DataHub.Instance.GetSource(Creature.SourceId) : null;
            return source != null;
        }
    }
}
