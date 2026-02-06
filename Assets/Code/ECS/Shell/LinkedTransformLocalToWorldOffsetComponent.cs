using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Components {
    public readonly struct LinkedTransformLocalToWorldOffsetComponent : IComponentData {
        public LinkedTransformLocalToWorldOffsetComponent(float4x4 offsetMatrix) {
            throw new NotImplementedException();
        }
    }
}