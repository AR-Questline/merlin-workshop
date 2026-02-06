using System;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;

namespace Awaken.ECS.DrakeRenderer.Components {
    public readonly struct DrakeRendererVisibleRangeComponent : IComponentData, IWithDebugText {
        public readonly float2 value;

        public DrakeRendererVisibleRangeComponent(float2 visibleRange) {
            throw new NotImplementedException();
        }

        public string DebugText => throw new NotImplementedException();
    }
}