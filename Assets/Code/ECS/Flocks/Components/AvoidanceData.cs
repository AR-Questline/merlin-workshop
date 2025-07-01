using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    public struct AvoidanceData : IComponentData {
        public float3 upDownHitPosition;
        public int valueSetFrame;
        public float3 leftRightHitPosition;
        public AvoidanceData(float3 upDownHitPosition, float3 leftRightHitPosition, int valueSetFrame) {
            this.upDownHitPosition = upDownHitPosition;
            this.leftRightHitPosition = leftRightHitPosition;
            this.valueSetFrame = valueSetFrame;
        }
    }
}