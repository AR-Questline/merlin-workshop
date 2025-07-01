using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;

namespace Awaken.ECS.DrakeRenderer.Components {
    public readonly struct DrakeRendererVisibleRangeComponent : IComponentData, IWithDebugText {
        public readonly float2 value;

        public DrakeRendererVisibleRangeComponent(float2 visibleRange) {
            value = visibleRange;
        }

        public string DebugText => $"Visible Range: [{math.sqrt(value.x)}...{math.sqrt(value.y)}]";
    }
}
