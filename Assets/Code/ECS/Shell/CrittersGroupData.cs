using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Critters.Components {
    public struct CrittersGroupData : IComponentData {
        public float3 spawnerCenter;
        public float spawnerRadius;
        public float cullingDistance;

        public CrittersGroupData(float3 spawnerCenter, float spawnerRadius, float cullingDistance) {
            throw new NotImplementedException();
        }
    }
}