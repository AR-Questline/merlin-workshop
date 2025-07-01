using Unity.Entities;

namespace Awaken.ECS.Mipmaps.Components {
    public struct UVDistributionMetricComponent : IComponentData {
        public float value;

        public UVDistributionMetricComponent(float value) {
            this.value = value;
        }
    }
}
