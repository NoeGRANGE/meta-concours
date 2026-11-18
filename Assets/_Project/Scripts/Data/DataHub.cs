using Deskmates.Data.Demo;
using Deskmates.Data.Live;
using UnityEngine;

namespace Deskmates.Data
{
    /// <summary>
    /// The one place creatures get their data sources and clock from. <see cref="DataSettings"/> picks the mode:
    /// the scripted demo (the default, PRD 7.1) or the user's own accounts through the desktop companion.
    /// </summary>
    public sealed class DataHub : MonoBehaviour
    {
        [Tooltip("Demo or Live. Without settings, the demo plays.")]
        [SerializeField] DataSettings settings;

        [SerializeField] DemoScript demoScript;

        [Tooltip("Speeds the whole demo up, for the video and the tests.")]
        [SerializeField] float speed = 1f;

        DemoPlayer demo;
        LiveSources live;

        public static DataHub Instance { get; private set; }

        public DataMode Mode => live != null ? DataMode.Live : DataMode.Demo;

        public IClock Clock => live != null ? live : demo;

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        void Awake()
        {
            Instance = this;
            if (settings != null && settings.mode == DataMode.Live)
            {
                live = new LiveSources(settings);
                Debug.Log("[Deskmates] Live mode: reading from the desktop companion.");
                return;
            }

            demo = new DemoPlayer(demoScript != null ? demoScript : ScriptableObject.CreateInstance<DemoScript>());
            if (demoScript == null)
            {
                Debug.LogWarning("[Deskmates] DataHub has no demo script; the creatures will stay calm.", this);
            }
        }

        void OnDestroy()
        {
            live?.Dispose();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Update()
        {
            if (live != null)
            {
                live.Step(Time.deltaTime);
            }
            else
            {
                demo.Step(Time.deltaTime * speed);
            }
        }

        public IDataSource GetSource(string id) => live != null ? live.GetSource(id) : demo.GetSource(id);
    }
}
