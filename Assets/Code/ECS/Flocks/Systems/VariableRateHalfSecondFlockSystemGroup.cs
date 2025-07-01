using Unity.Entities;
using UnityEngine.Scripting;

namespace Awaken.ECS.Flocks {
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(FlockSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(FlockRestSpotSystem))]
    public partial class VariableRateHalfSecondFlockSystemGroup : ComponentSystemGroup
    {
        /// <summary>
        /// The timestep use by this group, in seconds. This value will reflect the total elapsed time since the last update.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public float Timestep
        {
            get => RateManager != null ? RateManager.Timestep : 0;
        }

        /// <summary>
        /// Construct a new VariableRateFlockSystemGroup object
        /// </summary>
        [Preserve]
        public VariableRateHalfSecondFlockSystemGroup()
        {
            SetRateManagerCreateAllocator(new RateUtils.VariableRateManager(500));
        }
    }
}