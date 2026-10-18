using System;
using UnityEngine;

namespace Deskmates.Creatures
{
    /// <summary>
    /// Root component of a desk creature: smooths its stress and drives the face, motion and effects from it
    /// (PRD 4.3 and 9.1).
    /// </summary>
    [RequireComponent(typeof(CreatureFace), typeof(CreatureMotion), typeof(CreatureVfx))]
    public sealed class Creature : MonoBehaviour
    {
        [SerializeField] CreatureConfig config;

        CreatureFace face;
        CreatureMotion motion;
        CreatureVfx vfx;
        StressSmoother smoother;

        /// <summary>Raised when the creature sighs with relief after a stressful moment (PRD 4.5).</summary>
        public event Action Relieved;

        public CreatureConfig Config => config;

        /// <summary>Smoothed stress in [0, 1], the value everything on screen follows.</summary>
        public float Stress => smoother.Value;

        public CreatureExpression Expression => smoother.Expression;

        void Awake()
        {
            if (config == null)
            {
                Debug.LogWarning($"[Deskmates] {name} has no CreatureConfig; using defaults.", this);
                config = ScriptableObject.CreateInstance<CreatureConfig>();
            }

            face = GetComponent<CreatureFace>();
            motion = GetComponent<CreatureMotion>();
            vfx = GetComponent<CreatureVfx>();
            smoother = new StressSmoother(config);
            face.Apply(smoother.Expression);
        }

        void Start()
        {
            Tick(0f);
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Sets the stress the creature eases towards over a second or two.</summary>
        public void SetTargetStress(float stress)
        {
            smoother.Target = stress;
        }

        /// <summary>Plays the deflate, sigh and bounce. It also plays on its own after a stressful moment.</summary>
        public void PlayRelief()
        {
            motion.PlayRelief();
            vfx.PlaySigh();
            Relieved?.Invoke();
        }

        void Tick(float deltaTime)
        {
            CreatureExpression previous = smoother.Expression;
            bool relief = smoother.Step(deltaTime);
            if (smoother.Expression != previous)
            {
                face.Apply(smoother.Expression);
            }

            face.Tick(deltaTime, config);
            motion.Tick(deltaTime, smoother.Value, config);
            vfx.Tick(deltaTime, smoother.Value, smoother.Expression, config);

            if (relief)
            {
                PlayRelief();
            }
        }
    }
}
