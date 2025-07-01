using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    [Serializable]
    public struct TargetParams : IComponentData {
        public quaternion restRotation;
        public float3 flockTargetPosition;
        public float useFlockTargetPosMinTime;
        public float3 overridenTargetPosition;
        public bool useOverridenTargetPosition;
        public bool targetPositionIsRestPosition;
    }
}