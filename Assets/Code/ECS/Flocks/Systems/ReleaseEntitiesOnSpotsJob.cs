using Unity.Burst;
using Unity.Entities;

namespace Awaken.ECS.Flocks {
    [BurstCompile]
    public partial struct ReleaseEntitiesOnSpotsJob : IJobEntity {
        public const float BlockLandingTime = 20;

        public float allowedLandingTime;
        public ComponentLookup<TargetParams> targetParamsLookup;

        void Execute(ref FlockRestSpotStatus status) {
            ExecuteIml(ref status, ref targetParamsLookup, in allowedLandingTime);
        }
        
        [BurstCompile]
        public static void ExecuteIml(ref FlockRestSpotStatus status, ref ComponentLookup<TargetParams> targetParamsLookup, in float allowedLandingTime) {
            status.blockChangesUntilTime = allowedLandingTime;
            if (status.HasEntity) {
                var flockEntityToRelease = status.restingEntity;
                status.restingEntity = Entity.Null;
                var targetParamsRef = targetParamsLookup.GetRefRW(flockEntityToRelease);
                var targetParams = targetParamsRef.ValueRO;
                targetParams.targetPositionIsRestPosition = false;
                targetParams.overridenTargetPosition = targetParams.flockTargetPosition;
                targetParams.useOverridenTargetPosition = false;
                targetParamsRef.ValueRW = targetParams;
            }
        }
    }
}