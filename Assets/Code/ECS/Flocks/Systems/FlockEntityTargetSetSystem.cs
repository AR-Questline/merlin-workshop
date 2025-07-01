using Awaken.ECS.Components;
using Awaken.Utility.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    [UpdateInGroup(typeof(FlockSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(FlockEntityMovementSystem))]
    [RequireMatchingQueriesForUpdate] [BurstCompile]
    public partial class FlockEntityTargetSetSystem : SystemBase {
        const uint MaxRandomPositionTriesCount = 16;
        const float TargetPositionLockMinTime = 0.5f;

        EntityQuery _flockEntitiesQuery;
        EntityQuery _flockGroupEntitiesQuery;

        EntityTypeHandle _entityTypeHandle;
        ComponentTypeHandle<TargetParams> _targetParamsHandle;
        ComponentTypeHandle<MovementParams> _movementParamsHandle;
        ComponentTypeHandle<ReachDistanceToTarget> _reachDistanceToTargetHandle;
        SharedComponentTypeHandle<FlockGroupEntity> _flockGroupEntityHandle;

        protected override void OnCreate() {
            base.OnCreate();
            _flockEntitiesQuery = SystemAPI.QueryBuilder()
                .WithPresent<FlockGroupEntity>()
                .WithPresentRW<TargetParams, MovementParams>()
                .WithPresentRW<ReachDistanceToTarget>().WithNone<CulledEntityTag>().Build();
                
            _flockGroupEntitiesQuery = SystemAPI.QueryBuilder()
                .WithPresent<FlockData>()
                .WithPresentRW<RequestChangeFlockGroupTarget, FlockGroupLastTargetUpdatedTime>()
                .WithPresentRW<FlockGroupTargetPosition>()
                .WithNone<CulledEntityTag>().Build();

            _entityTypeHandle = GetEntityTypeHandle();
            _targetParamsHandle = GetComponentTypeHandle<TargetParams>();
            _movementParamsHandle = GetComponentTypeHandle<MovementParams>();
            _reachDistanceToTargetHandle = GetComponentTypeHandle<ReachDistanceToTarget>();
            _flockGroupEntityHandle = GetSharedComponentTypeHandle<FlockGroupEntity>();
        }

        protected override void OnUpdate() {
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime == 0) {
                return;
            }

            Dependency.Complete();

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            ref var systemState = ref CheckedStateRef;
            var entityManager = EntityManager;
            var dependency = Dependency;

            TryGetFlockGroupToUpdate(in currentTime, ref _entityTypeHandle, ref _targetParamsHandle, ref _movementParamsHandle, ref _reachDistanceToTargetHandle, ref _flockGroupEntityHandle, 
                ref _flockGroupEntitiesQuery, ref _flockEntitiesQuery, ref entityManager, ref systemState, ref dependency);

            Dependency = dependency;
        }

        [BurstCompile]
        static void TryGetFlockGroupToUpdate(in float currentTime, ref EntityTypeHandle entityTypeHandle, ref ComponentTypeHandle<TargetParams> targetParamsHandle,
            ref ComponentTypeHandle<MovementParams> movementParamsHandle, ref ComponentTypeHandle<ReachDistanceToTarget> reachDistanceToTargetHandle,
            ref SharedComponentTypeHandle<FlockGroupEntity> flockGroupEntityHandle,
            ref EntityQuery flockGroupEntitiesQuery, ref EntityQuery flockEntitiesQuery, ref EntityManager entityManager, ref SystemState systemState, ref JobHandle dependency) {
            var flockGroupEntities = flockGroupEntitiesQuery.ToEntityArray(ARAlloc.Temp);
            var flockDataArray = flockGroupEntitiesQuery.ToComponentDataArray<FlockData>(ARAlloc.Temp);
            var requestChangeFlockGroupTargets = flockGroupEntitiesQuery.ToComponentDataArray<RequestChangeFlockGroupTarget>(ARAlloc.Temp);
            var lastTargetUpdateTimeArray = flockGroupEntitiesQuery.ToComponentDataArray<FlockGroupLastTargetUpdatedTime>(ARAlloc.Temp);
            for (int i = 0; i < flockGroupEntities.Length; i++) {
                var flockGroupEntity = flockGroupEntities[i];
                var flockData = flockDataArray[i];
                var requestedChangeTarget = requestChangeFlockGroupTargets[i].value;
                var lastTargetUpdateTime = lastTargetUpdateTimeArray[i].value;
                var hash = math.hash(new double2(flockData.flockHash, lastTargetUpdateTime));
                var flockPosUpdateTimeDelay = StatelessRandom.GetRandomTime(flockData.targetPositionUpdateMinMax, hash);
                var changeFlockTargetPosByTime = (currentTime > lastTargetUpdateTime + flockPosUpdateTimeDelay);
                bool changeFlockTargetPos = (changeFlockTargetPosByTime | (requestedChangeTarget)) &
                                            (lastTargetUpdateTime + TargetPositionLockMinTime < currentTime);

                if (changeFlockTargetPos) {
                    var flockGroupTargetPrevPosition = entityManager.GetComponentData<FlockGroupTargetPosition>(flockGroupEntity).value;
                    var minDistanceSqFromPrevPos = math.square(math.cmax(flockData.positionVarianceExtents) * 3); // 1.5 times the max extents
                    float3 flockGroupTargetNewPosition = default;
                    GenerateNewFlockGroupTargetPosition(in flockData, in flockGroupTargetPrevPosition,
                        in currentTime, in minDistanceSqFromPrevPos, ref flockGroupTargetNewPosition);

                    entityManager.SetComponentData(flockGroupEntity, new RequestChangeFlockGroupTarget(false));
                    entityManager.SetComponentData(flockGroupEntity, new FlockGroupLastTargetUpdatedTime(currentTime));
                    entityManager.SetComponentData(flockGroupEntity, new FlockGroupTargetPosition(flockGroupTargetNewPosition));

                    flockGroupEntities.Dispose();
                    flockDataArray.Dispose();
                    lastTargetUpdateTimeArray.Dispose();
                    requestChangeFlockGroupTargets.Dispose();

                    entityTypeHandle.Update(ref systemState);
                    targetParamsHandle.Update(ref systemState);
                    movementParamsHandle.Update(ref systemState);
                    reachDistanceToTargetHandle.Update(ref systemState);
                    flockGroupEntityHandle.Update(ref systemState);

                    dependency = new UpdateTargetPositionsAndRandomizeMovementJob {
                        entityTypeHandle = entityTypeHandle,
                        targetParamsHandle = targetParamsHandle,
                        movementParamsHandle = movementParamsHandle,
                        reachDistanceToTargetHandle = reachDistanceToTargetHandle,
                        flockGroupEntityHandle = flockGroupEntityHandle,
                        flockData = flockData,
                        newTargetPosition = flockGroupTargetNewPosition,
                        currentTime = currentTime,
                        filterFlockGroupEntity = flockGroupEntity,
                    }.Schedule(flockEntitiesQuery, dependency);
                    return;
                }
            }

            flockGroupEntities.Dispose();
            flockDataArray.Dispose();
            lastTargetUpdateTimeArray.Dispose();
            requestChangeFlockGroupTargets.Dispose();
        }

        [BurstCompile]
        static void GenerateNewFlockGroupTargetPosition(in FlockData flockData, in float3 prevTargetPosition, in float currentTime,
            in float minDistanceSqFromPrevPos, ref float3 newTargetPosition) {
            var random = new Random(math.hash(new double2(flockData.flockHash, (currentTime))));
            float3 areaMin = flockData.areaCenter - flockData.areaExtents;
            float3 areaMax = flockData.areaCenter + flockData.areaExtents;
            int triesCount = 0;
            float distanceSqFromPrevPos;
            do {
                newTargetPosition = random.NextFloat3(areaMin, areaMax);
                distanceSqFromPrevPos = math.distancesq(newTargetPosition, prevTargetPosition);
                triesCount++;
            } while ((triesCount < MaxRandomPositionTriesCount) & (distanceSqFromPrevPos < minDistanceSqFromPrevPos));

            if (distanceSqFromPrevPos < minDistanceSqFromPrevPos) {
                var prevPosOffsetFromMinCorner = prevTargetPosition - areaMin;
                newTargetPosition = areaMax - (prevPosOffsetFromMinCorner + random.NextFloat3(-flockData.positionVarianceExtents, flockData.positionVarianceExtents));
                newTargetPosition = math.clamp(newTargetPosition, areaMin, areaMax);
            }
        }

        [BurstCompile]
        public struct UpdateTargetPositionsAndRandomizeMovementJob : IJobChunk {
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<TargetParams> targetParamsHandle;
            public ComponentTypeHandle<MovementParams> movementParamsHandle;
            public ComponentTypeHandle<ReachDistanceToTarget> reachDistanceToTargetHandle;

            public SharedComponentTypeHandle<FlockGroupEntity> flockGroupEntityHandle;

            public FlockData flockData;
            public float3 newTargetPosition;
            public float currentTime;
            public Entity filterFlockGroupEntity;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
                var flockGroupEntity = chunk.GetSharedComponent(flockGroupEntityHandle);
                if (flockGroupEntity.value.Equals(filterFlockGroupEntity) == false) {
                    return;
                }

                var entities = chunk.GetNativeArray(entityTypeHandle);
                var targetParamsArray = chunk.GetNativeArray(ref targetParamsHandle);
                var movementParamsArray = chunk.GetNativeArray(ref movementParamsHandle);
                var reachDistanceToTargetArray = chunk.GetNativeArray(ref reachDistanceToTargetHandle);

                for (int i = 0; i < chunk.Count; i++) {
                    var entity = entities[i];
                    var targetParams = targetParamsArray[i];
                    var movementParams = movementParamsArray[i];

                    // Calculate random values based on entity data
                    var uniqueHash = math.hash(new int2(entity.Index, entity.Version));
                    var random = new Random(uniqueHash);
                    var positionVarianceExtents = flockData.positionVarianceExtents;
                    var flockEntityTargetPos = random.NextFloat3(newTargetPosition - positionVarianceExtents, newTargetPosition + positionVarianceExtents);
                    var delayForUsingNewFlockTarget = (random.NextFloat() * flockData.maxDelayForUsingNewFlockTarget);
                    var areaMin = flockData.areaCenter - flockData.areaExtents;
                    var areaMax = flockData.areaCenter + flockData.areaExtents;
                    flockEntityTargetPos = math.clamp(flockEntityTargetPos, areaMin, areaMax);

                    // Update targetParams
                    targetParams.overridenTargetPosition = math.select(
                        targetParams.flockTargetPosition, targetParams.overridenTargetPosition, targetParams.useOverridenTargetPosition);
                    targetParams.flockTargetPosition = flockEntityTargetPos;
                    targetParams.useOverridenTargetPosition = targetParams.useOverridenTargetPosition & targetParams.targetPositionIsRestPosition;
                    targetParams.useFlockTargetPosMinTime = currentTime + delayForUsingNewFlockTarget;
                    targetParamsArray[i] = targetParams;

                    // Update movementParams
                    movementParams.steeringSpeedMult = random.NextFloat(flockData.steeringRotationDampingMinMax.x, flockData.steeringRotationDampingMinMax.y);
                    movementParams.movementSpeed = random.NextFloat(flockData.movementSpeedMinMax.x, flockData.movementSpeedMinMax.y);
                    movementParamsArray[i] = movementParams;

                    // Update reachDistanceToTarget
                    reachDistanceToTargetArray[i] = new ReachDistanceToTarget(0.001f);
                }
            }
        }
    }
}