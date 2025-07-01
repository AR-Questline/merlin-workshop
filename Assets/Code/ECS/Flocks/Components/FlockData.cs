using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    public struct FlockData : IComponentData {
        public uint flockHash;
        public float3 areaCenter;
        public float3 areaExtents;
        public float3 positionVarianceExtents;
        public float diveOnReachedTargetChance;
        public float diveMaxHeightDiff;
        public float maxDelayForUsingNewFlockTarget;
        public float2 steeringRotationDampingMinMax;
        public float2 movementSpeedMinMax;
        public float2 targetPositionUpdateMinMax;
        public float flockGroupSimulationDistance;
        public AABB AreaAABB => new AABB() { Center = areaCenter, Extents = areaExtents };

        public FlockData(uint flockHash, float3 areaCenter, float3 areaExtents, float3 positionVarianceExtents, float diveOnReachedTargetChance, float diveMaxHeightDiff, float maxDelayForUsingNewFlockTarget, float2 steeringRotationDampingMinMax, float2 movementSpeedMinMax, float2 targetPositionUpdateMinMax,
            float flockGroupSimulationDistance) {
            this.flockHash = flockHash;
            this.areaCenter = areaCenter;
            this.areaExtents = areaExtents;
            this.positionVarianceExtents = positionVarianceExtents;
            this.diveOnReachedTargetChance = diveOnReachedTargetChance;
            this.diveMaxHeightDiff = diveMaxHeightDiff;
            this.maxDelayForUsingNewFlockTarget = maxDelayForUsingNewFlockTarget;
            this.steeringRotationDampingMinMax = steeringRotationDampingMinMax;
            this.movementSpeedMinMax = movementSpeedMinMax;
            this.targetPositionUpdateMinMax = targetPositionUpdateMinMax;
            this.flockGroupSimulationDistance = flockGroupSimulationDistance;
        }
    }
}