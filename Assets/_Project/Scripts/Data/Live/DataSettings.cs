using UnityEngine;

namespace Deskmates.Data.Live
{
    public enum DataMode
    {
        /// <summary>The scripted demo: fictional data, no account needed (PRD 7.1).</summary>
        Demo,

        /// <summary>The user's real mail, calendar, tasks and coding-agent sessions, through the desktop companion.</summary>
        Live,
    }

    /// <summary>
    /// Where the creatures' data comes from. Switching <see cref="mode"/> to Live is the only change needed to move
    /// from the demo to the user's own accounts; the rest of the app reads every source the same way (PRD 7.4).
    /// </summary>
    [CreateAssetMenu(menuName = "Deskmates/Data Settings", fileName = "DataSettings")]
    public sealed class DataSettings : ScriptableObject
    {
        public DataMode mode = DataMode.Demo;

        [Header("Desktop companion (Live mode)")]
        [Tooltip("Address of the computer running the companion. Leave empty to find it on the local network.")]
        public string companionHost = "";

        public int companionPort = 7878;

        [Tooltip("The pairing code the companion prints the first time it starts.")]
        public string pairingCode = "";

        [Tooltip("Seconds between two refreshes of each source.")]
        public float refreshSeconds = 30f;

        [Tooltip("Port on which the companion announces itself on the local network.")]
        public int discoveryPort = 7879;
    }
}
