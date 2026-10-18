using System.Collections.Generic;
using UnityEngine;

namespace Deskmates.Creatures
{
    /// <summary>
    /// Stress effects on the model: body colour and glow, the mail creature's flames, extra post-its,
    /// sweat drop and typing dots (PRD 4.3 and 10.1), plus the sigh puff of the relief (PRD 4.5).
    /// </summary>
    public sealed class CreatureVfx : MonoBehaviour
    {
        // glTFast material properties.
        static readonly int BaseColorId = Shader.PropertyToID("baseColorFactor");
        static readonly int EmissiveId = Shader.PropertyToID("emissiveFactor");

        // Flame size per state relative to the modelled size, at calm, alert, stressed and panic (PRD 10.1).
        static readonly Dictionary<string, Vector4> FlameScales = new()
        {
            { "FlameMain", new Vector4(0f, 0.55f, 0.85f, 1.3f) },
            { "FlameLeft", new Vector4(0f, 0f, 0.7f, 1.15f) },
            { "FlameRight", new Vector4(0f, 0f, 0.7f, 1.15f) },
            { "FlameCore", new Vector4(0f, 0f, 0.6f, 1.1f) },
        };

        // Offsets below are in model units, where the body is about one unit tall.
        const float FlameFlicker = 0.08f;
        const float SweatDrop = 0.04f;
        const float DotHop = 0.05f;

        [SerializeField] ParticleSystem sighPuff;

        readonly List<Flame> flames = new();
        readonly List<Transform> typingDots = new();
        readonly List<Vector3> typingDotBases = new();
        MaterialPropertyBlock block;
        Renderer body;
        GameObject postitBlue;
        GameObject postitPink;
        Transform sweat;
        Vector3 sweatBase;
        float time;
        float noiseSeed;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            noiseSeed = Random.value * 100f;
            foreach (Transform part in GetComponentsInChildren<Transform>(true))
            {
                string key = CreatureFace.PartKey(part.name);
                if (key == null)
                {
                    continue;
                }

                if (FlameScales.TryGetValue(key, out Vector4 scales))
                {
                    flames.Add(new Flame(part, scales));
                }
                else if (key.StartsWith("BubbleDot"))
                {
                    typingDots.Add(part);
                    typingDotBases.Add(part.localPosition);
                }
                else
                {
                    switch (key)
                    {
                        case "Body":
                            body = part.GetComponent<Renderer>();
                            break;
                        case "PostitBlue":
                            postitBlue = part.gameObject;
                            break;
                        case "PostitPink":
                            postitPink = part.gameObject;
                            break;
                        case "Sweat":
                            sweat = part;
                            sweatBase = part.localPosition;
                            break;
                    }
                }
            }
        }

        public void PlaySigh()
        {
            if (sighPuff != null)
            {
                sighPuff.Play();
            }
        }

        public void Tick(float deltaTime, float stress, CreatureExpression expression, CreatureConfig config)
        {
            time += deltaTime;

            if (body != null)
            {
                Color color = StressRamp.Evaluate(stress, config.restColor, config.alertColor, config.stressedColor,
                    config.panicColor);
                float glow = StressRamp.Weight(0.8f, 1f, stress) * config.panicEmission;
                body.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                block.SetColor(EmissiveId, color * glow);
                body.SetPropertyBlock(block);
            }

            foreach (Flame flame in flames)
            {
                float size = StressRamp.Evaluate(stress, flame.Scales);
                flame.Part.gameObject.SetActive(size > 0.02f);
                float flicker = 1f + (Mathf.PerlinNoise(time * 6f, noiseSeed + flame.Seed) - 0.5f) * 2f * FlameFlicker;
                flame.Part.localScale = flame.BaseScale * (size * flicker);
            }

            if (postitBlue != null)
            {
                postitBlue.SetActive(expression >= CreatureExpression.Stressed);
            }

            if (postitPink != null)
            {
                postitPink.SetActive(expression == CreatureExpression.Panic);
            }

            if (sweat != null)
            {
                sweat.localPosition = sweatBase + Vector3.down * (Mathf.Repeat(time * 0.6f, 1f) * SweatDrop);
            }

            // The dots of the message bubble type away as soon as someone is waiting.
            bool typing = stress >= config.expressionThresholds.x;
            for (int i = 0; i < typingDots.Count; i++)
            {
                float hop = typing ? Mathf.Max(0f, Mathf.Sin(time * 6f - i * 0.9f)) * DotHop : 0f;
                typingDots[i].localPosition = typingDotBases[i] + Vector3.up * hop;
            }
        }

        sealed class Flame
        {
            public Flame(Transform part, Vector4 scales)
            {
                Part = part;
                Scales = scales;
                BaseScale = part.localScale;
                Seed = Random.value * 50f;
            }

            public Transform Part { get; }
            public Vector4 Scales { get; }
            public Vector3 BaseScale { get; }
            public float Seed { get; }
        }
    }
}
