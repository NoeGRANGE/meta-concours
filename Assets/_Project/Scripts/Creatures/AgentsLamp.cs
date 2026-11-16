using System.Collections.Generic;
using System.Linq;
using Deskmates.Data;
using UnityEngine;

namespace Deskmates.Creatures
{
    /// <summary>
    /// The agents creature's hard hat (PRD 4.6): one bead per session above its head (grey running, blue done,
    /// orange waiting, red failed), and the head lamp that glows while a session waits for the user. When a session
    /// finishes, the lamp blinks twice.
    /// </summary>
    [RequireComponent(typeof(CreatureDataLink))]
    public sealed class AgentsLamp : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("baseColorFactor");
        static readonly int EmissiveId = Shader.PropertyToID("emissiveFactor");

        static readonly Color Running = new Color32(0x9A, 0xA3, 0xAB, 0xFF);
        static readonly Color Done = new Color32(0x5B, 0x8D, 0xEF, 0xFF);
        static readonly Color Waiting = new Color32(0xF1, 0xA6, 0x61, 0xFF);
        static readonly Color Failed = new Color32(0xE2, 0x55, 0x3A, 0xFF);
        static readonly Color LampOff = new Color32(0xF4, 0xEE, 0xE2, 0xFF);
        static readonly Color LampOn = new Color32(0xFF, 0xD9, 0x8A, 0xFF);

        [SerializeField] float lampGlow = 2.5f;
        [SerializeField] float pulseSpeed = 2.2f;
        [SerializeField] float blinkSeconds = 0.14f;

        readonly List<Renderer> beads = new();
        Renderer lens;
        CreatureDataLink link;
        MaterialPropertyBlock block;
        int doneBefore;
        float blinkTime = float.MaxValue;

        void Awake()
        {
            link = GetComponent<CreatureDataLink>();
            block = new MaterialPropertyBlock();
            Dictionary<string, Renderer> parts = GetComponentsInChildren<Renderer>(true)
                .GroupBy(r => r.name).ToDictionary(g => g.Key, g => g.First());
            lens = parts.FirstOrDefault(p => p.Key.EndsWith("_LampLens")).Value;
            for (int i = 0; ; i++)
            {
                Renderer bead = parts.FirstOrDefault(p => p.Key.EndsWith($"_Bead{i}")).Value;
                if (bead == null)
                {
                    break;
                }

                beads.Add(bead);
            }
        }

        void Update()
        {
            DataHub hub = DataHub.Instance;
            IDataSource source = hub != null ? hub.GetSource(link.SourceId) : null;
            IReadOnlyList<SourceItem> sessions = source != null ? source.Current.Items : System.Array.Empty<SourceItem>();

            // Beads keep the sessions' arrival order, so a bead does not jump when another session changes state.
            for (int i = 0; i < beads.Count; i++)
            {
                bool used = i < sessions.Count;
                beads[i].enabled = used;
                if (used)
                {
                    Paint(beads[i], StateColor(sessions[i].State), 0f);
                }
            }

            int done = sessions.Count(s => s.State == AgentStates.Done);
            if (done > doneBefore)
            {
                blinkTime = 0f;
            }

            doneBefore = done;
            blinkTime += Time.deltaTime;
            if (lens == null)
            {
                return;
            }

            bool waiting = sessions.Any(s => s.State == AgentStates.Waiting);
            float glow = waiting ? lampGlow * (0.75f + 0.25f * Mathf.Sin(Time.time * pulseSpeed)) : 0f;
            if (blinkTime < blinkSeconds * 4f)
            {
                // Two short flashes: on, off, on, off.
                glow = (int)(blinkTime / blinkSeconds) % 2 == 0 ? lampGlow : 0f;
            }

            Paint(lens, glow > 0f ? LampOn : LampOff, glow);
        }

        void Paint(Renderer target, Color color, float glow)
        {
            target.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(EmissiveId, color * glow);
            target.SetPropertyBlock(block);
        }

        static Color StateColor(string state) => state switch
        {
            AgentStates.Waiting => Waiting,
            AgentStates.Failed => Failed,
            AgentStates.Done => Done,
            _ => Running,
        };
    }
}
