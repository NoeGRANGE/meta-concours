using System.Collections.Generic;
using Deskmates.Core;
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

        [Tooltip("Small pill at the top of the panel, in the colour of the creature it belongs to.")]
        [SerializeField] Image accent;
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
            SoundBank.Play(Sound.PanelOpen, creature.transform.position);
            Creature = creature;
            lookedAwayFor = 0f;
            if (accent != null)
            {
                accent.color = creature.Accent;
            }

            canvas.gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (Creature != null)
            {
                SoundBank.Play(Sound.PanelClose, Creature.transform.position);
            }

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

            Vector3 at = Creature.transform.position;
            if (await source.PerformAsync(action.Value, shown[row].Id))
            {
                // Up to a semitone higher or lower each time, so several in a row do not sound mechanical.
                SoundBank.Play(Sound.ItemHandled, at, Mathf.Pow(2f, Random.Range(-1f, 1f) / 12f));
            }

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

            string verb = ItemText.ActionVerb(Creature.SourceId);
            hint.text = shown.Count == 0 ? "All clear!"
                : verb == null ? "Pinch me to close"
                : $"Pinch a line to mark it {verb} · pinch me to close";
        }

        bool TryGetSource(out IDataSource source)
        {
            source = DataHub.Instance != null && Creature != null ? DataHub.Instance.GetSource(Creature.SourceId) : null;
            return source != null;
        }
    }
}
