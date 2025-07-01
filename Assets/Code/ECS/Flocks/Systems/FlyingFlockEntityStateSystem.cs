using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    [UpdateInGroup(typeof(FlockSystemGroup))]
    [UpdateAfter(typeof(FlockEntityMovementSystem))]
    [BurstCompile] [RequireMatchingQueriesForUpdate]
    public partial class FlyingFlockEntityStateSystem : SystemBase {
        const float SoarLandingMinDistance = 0.5f;
        const float SoarLandingMaxDistance = 5f;
        const float DiveHeightMinDiff = 1;
        const float DiveHeightMaxDiff = 7;
        const float RisingUpHeightDiff = 2;
        const float SoarDiveAverageTime = 4;
        const float SoarAverageTime = 3;
        const float SoarTimeVariance = 1f;
        const float SoarDiveMaxChance = 0.7f;
        const float SoarChance = 0.2f;
        const float SoarDiveChanceInv = 1 / SoarDiveMaxChance;
        const float SoarChanceInv = 1 / SoarChance;
        const float RandomUpdateFrequency = 60 * 10;

        Random _random;
        uint _randomHash;
        double _randomHashUpdatedTime;

        protected override void OnCreate() {
            base.OnCreate();
            _random = new Random(1451438763);
        }

        protected override void OnUpdate() {
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime == 0) {
                return;
            }

            double currentTime = SystemAPI.Time.ElapsedTime;
            bool needsUpdateRandom = currentTime - _randomHashUpdatedTime > RandomUpdateFrequency;
            _randomHashUpdatedTime = math.select(_randomHashUpdatedTime, currentTime, needsUpdateRandom);
            _randomHash = math.select(_randomHash, _random.NextUInt(99, int.MaxValue), needsUpdateRandom);
            
            var randomHash = _randomHash;
            Dependency = Entities.WithNone<CulledEntityTag>().ForEach((Entity entity, ref FlyingFlockEntityState state,
                in TargetParams targetParams, in FlyingFlockEntityAnimationsData animationsData, in DrakeVisualEntitiesTransform flockEntityTransform) => {
                var position = flockEntityTransform.position;
                bool isMovingTowardRestPosition = targetParams.targetPositionIsRestPosition;
                var useOverridenTargetPosition = targetParams.useOverridenTargetPosition | (targetParams.useFlockTargetPosMinTime > currentTime);
                var targetPosition = math.select(targetParams.flockTargetPosition, targetParams.overridenTargetPosition, useOverridenTargetPosition);
                var distanceToTargetPos = math.distance(position, targetPosition);
                bool isInRestPosition = distanceToTargetPos <= FlockEntityMovementSystem.DistanceToTargetEpsilon;
                var prevState = state.value;
                state.value = FlyingFlockEntityState.State.None;
                if (isMovingTowardRestPosition) {
                    bool isLandingClose = distanceToTargetPos < SoarLandingMinDistance;
                    if (isInRestPosition) {
                        state.value |= FlyingFlockEntityState.State.Resting;
                    } else {
                        bool useSoarLanding = animationsData.useSoarLanding & (distanceToTargetPos < SoarLandingMaxDistance) & (distanceToTargetPos > SoarLandingMinDistance);
                        state.value |= useSoarLanding ? FlyingFlockEntityState.State.Soaring : FlyingFlockEntityState.State.Flapping;
                        state.value |= isLandingClose ? FlyingFlockEntityState.State.None : FlyingFlockEntityState.State.LandingFar;
                    }
                    // Intermediate state for one frame
                    state.value |= (((prevState & FlyingFlockEntityState.State.LandingFar) != 0) & isLandingClose) ? FlyingFlockEntityState.State.Landing : FlyingFlockEntityState.State.None;
                } else {
                    var entityStableHash = math.hash(new int2(entity.Index, entity.Version));
                    var entityNotFrequentlyChangingHash = math.hash(new uint2(entityStableHash, randomHash));
                    var heightDiff = position.y - targetPosition.y;
                    var isDiving = heightDiff > DiveHeightMinDiff;
                    var soarAverageTime = math.select(SoarAverageTime, SoarDiveAverageTime, isDiving);
                    var soarTimeMinMax = new float2(soarAverageTime - SoarTimeVariance, soarAverageTime + SoarTimeVariance);
                    var soarChanceInv = math.select(SoarChanceInv, SoarDiveChanceInv, isDiving);
                    var soarChanceMult = math.select(1, math.rcp(math.clamp(math.unlerp(DiveHeightMinDiff, DiveHeightMaxDiff, heightDiff), 0, 1)), isDiving);
                    bool useSoar = StatelessRandom.IsRandomContinuousStateEnabled(soarTimeMinMax,
                        soarChanceInv * soarChanceMult, currentTime, entityNotFrequentlyChangingHash);
                    var isRisingUp = heightDiff < -RisingUpHeightDiff;
                    var isSoarMovement = useSoar & !isRisingUp;
                    state.value |= isSoarMovement ? FlyingFlockEntityState.State.Soaring : FlyingFlockEntityState.State.Flapping;
                    // Intermediate state for one frame
                    state.value |= ((prevState & FlyingFlockEntityState.State.Resting) != 0) ? FlyingFlockEntityState.State.TakingOff : FlyingFlockEntityState.State.None;
                }
            }).WithBurst().ScheduleParallel(Dependency);
        }
    }
}