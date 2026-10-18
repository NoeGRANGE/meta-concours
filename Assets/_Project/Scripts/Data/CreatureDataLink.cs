using Deskmates.Creatures;
using Deskmates.Data.Stress;
using UnityEngine;

namespace Deskmates.Data
{
    /// <summary>
    /// Feeds a creature the stress of its data source, every frame, so time-based stress such as an approaching
    /// meeting keeps moving between two snapshots. Does nothing while the scene's DataHub is missing or disabled.
    /// </summary>
    [RequireComponent(typeof(Creature))]
    public sealed class CreatureDataLink : MonoBehaviour
    {
        [Tooltip("One of the SourceIds: mail, calendar, tasks or messages.")]
        [SerializeField] string sourceId;

        [SerializeField] StressCalculator calculator;

        Creature creature;

        public string SourceId => sourceId;

        void Awake()
        {
            creature = GetComponent<Creature>();
        }

        void Update()
        {
            DataHub hub = DataHub.Instance;
            if (hub == null || !hub.isActiveAndEnabled || calculator == null)
            {
                return;
            }

            IDataSource source = hub.GetSource(sourceId);
            if (source != null)
            {
                creature.SetTargetStress(calculator.Evaluate(source.Current, hub.Clock.Now));
            }
        }
    }
}
