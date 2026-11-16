using System;
using UnityEngine;

namespace Deskmates.Core
{
    /// <summary>The app's few sounds (STYLE.md 6): short, soft, and played where they happen.</summary>
    public enum Sound
    {
        Landing,
        Sigh,
        PanelOpen,
        PanelClose,
        ItemHandled,
        Ripple,
        FamilyArrives,
        AgentWaiting,
    }

    /// <summary>
    /// Plays the one-shot sounds of the scene from a small pool of 3D sources, so each sound comes from the creature
    /// or place it belongs to. Without a bank in the scene (sandbox, tests), sounds are simply skipped.
    /// </summary>
    public sealed class SoundBank : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public Sound sound;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        const int PoolSize = 8;

        [SerializeField] Entry[] entries;

        AudioSource[] pool;
        int next;

        public static SoundBank Instance { get; private set; }

        /// <summary>Plays <paramref name="sound"/> at <paramref name="position"/>, slightly higher or lower with <paramref name="pitch"/>.</summary>
        public static void Play(Sound sound, Vector3 position, float pitch = 1f)
        {
            if (Instance != null)
            {
                Instance.PlayHere(sound, position, pitch);
            }
        }

        /// <summary>A desk-scale 3D source: full volume within 30 cm, fading out over a few metres.</summary>
        public static void Configure(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.3f;
            source.maxDistance = 4f;
            source.dopplerLevel = 0f;
        }

        void Awake()
        {
            Instance = this;
            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                GameObject voice = new GameObject($"Voice {i}");
                voice.transform.SetParent(transform, false);
                pool[i] = voice.AddComponent<AudioSource>();
                Configure(pool[i]);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void PlayHere(Sound sound, Vector3 position, float pitch)
        {
            foreach (Entry entry in entries)
            {
                if (entry.sound != sound || entry.clip == null)
                {
                    continue;
                }

                AudioSource voice = pool[next];
                next = (next + 1) % pool.Length;
                voice.transform.position = position;
                voice.pitch = pitch;
                voice.PlayOneShot(entry.clip, entry.volume);
                return;
            }
        }
    }
}
