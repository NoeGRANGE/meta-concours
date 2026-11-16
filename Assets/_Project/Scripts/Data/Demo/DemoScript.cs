using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskmates.Data.Demo
{
    public enum DemoEventKind
    {
        /// <summary>A mail, meeting, task or message appears.</summary>
        AddItem,

        /// <summary>The user deals with an item (mark read, mark done). Played only when Auto Resolve is on.</summary>
        Resolve,
    }

    [Serializable]
    public sealed class DemoEvent
    {
        [Tooltip("Real seconds since the start of the demo, before the speed multiplier.")]
        public float atSecond;

        public string sourceId;
        public DemoEventKind kind;

        [Tooltip("Item to add or resolve. For Resolve, leave empty to resolve every item of the source.")]
        public string itemId;

        public string title;
        public string detail;
        public bool important;

        [Tooltip("Agent sessions only: running, waiting, done or failed. Adding the same item again changes its state.")]
        public string state;

        [Tooltip("Item time relative to the demo clock when the event fires, in demo minutes: when a mail or message " +
                 "arrived (negative), when a meeting starts or a task is due.")]
        public float minutesFromNow;
    }

    /// <summary>
    /// The timed scenario of the demo mode (PRD 7.1): fictional data, in English, with invented names and no brands.
    /// </summary>
    [CreateAssetMenu(menuName = "Deskmates/Demo Script", fileName = "DemoScript")]
    public sealed class DemoScript : ScriptableObject
    {
        [Tooltip("Demo minutes that pass per real second, so a meeting an hour away arrives within the demo.")]
        public float demoMinutesPerSecond = 0.5f;

        [Tooltip("Demo clock time at the start, in hours after midnight.")]
        public float startHour = 9.5f;

        [Tooltip("Plays the user's actions itself. Turn off once the user can mark items read or done by hand.")]
        public bool autoResolve = true;

        [Tooltip("Starts over once the last event has played and the creatures had time to calm down.")]
        public bool loop;

        [Tooltip("Seconds kept after the last event before the demo ends or loops.")]
        public float tailSeconds = 20f;

        public List<DemoEvent> events = new();
    }
}
