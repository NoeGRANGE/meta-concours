using Deskmates.Data.Demo;
using UnityEngine;

namespace Deskmates.Data
{
    /// <summary>
    /// The one place creatures get their data sources and clock from. Demo mode is the default (PRD 7.1)
    /// and, until the Google connection exists, the only mode.
    /// </summary>
    public sealed class DataHub : MonoBehaviour
    {
        [SerializeField] DemoScript demoScript;

        [Tooltip("Speeds the whole demo up, for the video and the tests.")]
        [SerializeField] float speed = 1f;

        DemoPlayer demo;

        public static DataHub Instance { get; private set; }

        public IClock Clock => demo;

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        void Awake()
        {
            Instance = this;
            demo = new DemoPlayer(demoScript != null ? demoScript : ScriptableObject.CreateInstance<DemoScript>());
            if (demoScript == null)
            {
                Debug.LogWarning("[Deskmates] DataHub has no demo script; the creatures will stay calm.", this);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Update()
        {
            demo.Step(Time.deltaTime * speed);
        }

        public IDataSource GetSource(string id) => demo.GetSource(id);
    }
}
