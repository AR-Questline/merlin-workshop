using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer;
using Awaken.Utility.Collections;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    [UpdateInGroup(typeof(FlockSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(FlockEntityTargetSetSystem))]
    [UpdateBefore(typeof(FlockEntityMovementSystem))]
    [BurstCompile] [RequireMatchingQueriesForUpdate]
    public partial class FlockWanderBehaviourSystem : SystemBase {
        const int MaxEntitiesToCheckIfReachedTargetThisFrame = 300;
        const int MaxEntitiesToCheckIfReachedTarget = 300;
        const int ReachTargetDistanceSq = 2 * 2;
        const int EstimatedMaxFlockGroupsCount = 3;

        NativeArray<Entity> _entitiesWhichReachedTargetThisFrame;
        NativeArray<Entity> _entitiesWhichReachedTarget;
        NativeList<Entity> _flockGroupEntitiesForChangeTargetJob;
        NativeArray<Entity> _entitiesToWander;
        NativeArray<Entity> _flockGroupEntitiesForWanderJob;
        NativeReference<int> _entitiesWhichReachedTargetThisFrameCountRef;
        NativeReference<int> _entitiesWhichReachedTargetCountRef;

        ComponentLookup<TargetParams> _targetParamsLookup;
        ComponentLookup<RequestChangeFlockGroupTarget> _requestChangeFlockGroupTargetLookup;
        ComponentLookup<FlockData> _flockDataLookup;
        JobHandle _jobsHandle;

        protected override void OnCreate() {
            base.OnCreate();
            _entitiesWhichReachedTargetThisFrame = new NativeArray<Entity>(MaxEntitiesToCheckIfReachedTargetThisFrame + 1, ARAlloc.Persistent);
            _entitiesWhichReachedTarget = new NativeArray<Entity>(MaxEntitiesToCheckIfReachedTargetThisFrame + 1, ARAlloc.Persistent);
            _flockGroupEntitiesForChangeTargetJob = new NativeList<Entity>(EstimatedMaxFlockGroupsCount, ARAlloc.Persistent);
            _entitiesToWander = new NativeArray<Entity>(MaxEntitiesToCheckIfReachedTargetThisFrame, ARAlloc.Persistent);
            _flockGroupEntitiesForWanderJob = new NativeArray<Entity>(MaxEntitiesToCheckIfReachedTargetThisFrame, ARAlloc.Persistent);
            _entitiesWhichReachedTargetThisFrameCountRef = new NativeReference<int>(0, ARAlloc.Persistent);
            _entitiesWhichReachedTargetCountRef = new NativeReference<int>(0, ARAlloc.Persistent);

            _flockDataLookup = GetComponentLookup<FlockData>(true);
            _targetParamsLookup = GetComponentLookup<TargetParams>();
            _requestChangeFlockGroupTargetLookup = GetComponentLookup<RequestChangeFlockGroupTarget>();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            Dependency.Complete();
            _entitiesWhichReachedTargetThisFrame.Dispose();
            _entitiesWhichReachedTarget.Dispose();
            _flockGroupEntitiesForChangeTargetJob.Dispose();
            _entitiesToWander.Dispose();
            _flockGroupEntitiesForWanderJob.Dispose();
            _entitiesWhichReachedTargetThisFrameCountRef.Dispose();
            _entitiesWhichReachedTargetCountRef.Dispose();
        }

        protected override void OnUpdate() {
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime == 0) {
                return;
            }

            Dependency.Complete();

            var currentFrame = UnityEngine.Time.frameCount;

            var entitiesWhichReachedTargetThisFrameCountRef = _entitiesWhichReachedTargetThisFrameCountRef;
            var entitiesWhichReachedTargetThisFrameCount = entitiesWhichReachedTargetThisFrameCountRef.Value;
            entitiesWhichReachedTargetThisFrameCountRef.Value = 0;
            if (entitiesWhichReachedTargetThisFrameCount > 0) {
                _entitiesToWander.CopyFrom(_entitiesWhichReachedTargetThisFrame.GetSubArray(1, MaxEntitiesToCheckIfReachedTargetThisFrame));
                for (int i = 0; i < entitiesWhichReachedTargetThisFrameCount; i++) {
                    var entityToWander = _entitiesToWander[i];
                    if (EntityManager.Exists(entityToWander)) {
                        _flockGroupEntitiesForWanderJob[i] = EntityManager.GetSharedComponent<FlockGroupEntity>(entityToWander);
                    } else {
                        _entitiesToWander[i] = Entity.Null;
                    }
                }

                _flockDataLookup.Update(this);
                _targetParamsLookup.Update(this);
                Dependency = new SetWanderParamsJob() {
                    entities = _entitiesToWander,
                    flockGroupEntities = _flockGroupEntitiesForWanderJob,
                    flockDataLookup = _flockDataLookup,
                    targetParamsLookup = _targetParamsLookup,
                    currentTime = currentFrame,
                    entitiesCount = entitiesWhichReachedTargetThisFrameCount
                }.Schedule(Dependency);
            }

            var entitiesWhichReachedTargetCountRef = _entitiesWhichReachedTargetCountRef;
            var entitiesWhichReachedTargetCount = entitiesWhichReachedTargetCountRef.Value;
            entitiesWhichReachedTargetCountRef.Value = 0;
            if (entitiesWhichReachedTargetCount > 0) {
                _flockGroupEntitiesForChangeTargetJob.Clear();
                var entitiesToProcess = _entitiesWhichReachedTarget.GetSubArray(1, entitiesWhichReachedTargetCount);
                for (int i = 0; i < entitiesWhichReachedTargetCount; i++) {
                    if (EntityManager.Exists(entitiesToProcess[i]) == false) {
                        continue;
                    }
                    var flockGroupEntity = EntityManager.GetSharedComponent<FlockGroupEntity>(entitiesToProcess[i]).value;
                    if (_flockGroupEntitiesForChangeTargetJob.Contains(flockGroupEntity) == false) {
                        _flockGroupEntitiesForChangeTargetJob.Add(flockGroupEntity);
                    }
                }

                _requestChangeFlockGroupTargetLookup.Update(this);
                new RequestChangeFlockGroupTargetJob() {
                    flockGroupEntities = _flockGroupEntitiesForChangeTargetJob.AsArray(),
                    requestChangeFlockGroupTargetLookup = _requestChangeFlockGroupTargetLookup,
                }.Run();
            }

            var entitiesWhichReachedTargetThisFrame = _entitiesWhichReachedTargetThisFrame;
            var entitiesWhichReachedTarget = _entitiesWhichReachedTarget;

            Dependency = Entities.WithNone<CulledEntityTag>()
                .ForEach((Entity entity, ref ReachDistanceToTarget reachDistanceToTarget, in TargetParams targetParams, in DrakeVisualEntitiesTransform flockEntityTransform) => {
                    var targetPosition = targetParams.flockTargetPosition;
                    var distanceSqToTarget = math.distancesq(targetPosition, flockEntityTransform.position);
                    var reachDistanceSq = math.square(reachDistanceToTarget.reachDistance);
                    bool reachedDistance = (distanceSqToTarget < reachDistanceSq);
                    bool isReachDistanceZero = reachDistanceSq == 0;
                    bool reachedTargetThisFrame = reachedDistance & !isReachDistanceZero;
                    bool reachedTarget = reachedDistance | (reachDistanceSq == 0);
                    {
                        var currentCount = entitiesWhichReachedTargetThisFrameCountRef.Value;
                        bool willBeProcessed = currentCount < MaxEntitiesToCheckIfReachedTargetThisFrame;
                        // using index 0 as trash bit to throw there entities which are not valid and avoid branching.
                        var entityIndex = math.select(0, math.min(currentCount + 1, MaxEntitiesToCheckIfReachedTargetThisFrame), reachedTargetThisFrame);
                        var newCount = math.max(entityIndex, currentCount);
                        entitiesWhichReachedTargetThisFrameCountRef.Value = newCount;
                        entitiesWhichReachedTargetThisFrame[entityIndex] = entity;

                        var newReachDistance = math.select(reachDistanceToTarget.reachDistance + deltaTime, 0, ((reachedTarget & willBeProcessed) | (isReachDistanceZero)));
                        reachDistanceToTarget.reachDistance = newReachDistance;
                    }
                    {
                        var currentCount = entitiesWhichReachedTargetCountRef.Value;
                        var reachedTargetClose = distanceSqToTarget < ReachTargetDistanceSq;
                        // using index 0 as trash bit to throw there entities which are not valid and avoid branching.
                        var entityIndex = math.select(0, math.min(currentCount + 1, MaxEntitiesToCheckIfReachedTarget), reachedTargetClose);
                        var newCount = math.max(entityIndex, currentCount);
                        entitiesWhichReachedTargetCountRef.Value = newCount;
                        entitiesWhichReachedTarget[entityIndex] = entity;
                    }
                }).WithBurst().Schedule(Dependency);
        }

        [BurstCompile]
        public struct SetWanderParamsJob : IJob {
            [ReadOnly] public NativeArray<Entity> entities;
            [ReadOnly] public NativeArray<Entity> flockGroupEntities;
            [ReadOnly] public ComponentLookup<FlockData> flockDataLookup;
            public ComponentLookup<TargetParams> targetParamsLookup;

            public int entitiesCount;
            public double currentTime;

            [BurstCompile]
            public void Execute() {
                int count = entitiesCount;
                for (int i = 0; i < count; i++) {
                    var entity = entities[i];
                    if (Hint.Unlikely(entity.Equals(Entity.Null))) {
                        continue;
                    }
                    var targetParamsRef = targetParamsLookup.GetRefRW(entity);
                    var targetParams = targetParamsRef.ValueRO;
                    var flockData = flockDataLookup[flockGroupEntities[i]];
                    var random = new Random(math.hash(new double3(entity.Index, entity.Version, currentTime)));
                    var positionVarianceExtents = flockData.positionVarianceExtents;
                    var wanderMinPos = targetParams.flockTargetPosition - positionVarianceExtents;
                    var wanderMaxPos = targetParams.flockTargetPosition + positionVarianceExtents;
                    var newTargetPos = random.NextFloat3(wanderMinPos, wanderMaxPos);
                    bool dive = random.NextFloat() < flockData.diveOnReachedTargetChance;
                    var diveHeightDiff = random.NextFloat() * flockData.diveMaxHeightDiff;
                    newTargetPos.y = math.select(newTargetPos.y, newTargetPos.y - diveHeightDiff, dive);
                    var areaMin = flockData.areaCenter - flockData.areaExtents;
                    var areaMax = flockData.areaCenter + flockData.areaExtents;
                    newTargetPos = math.clamp(newTargetPos, areaMin, areaMax);
                    targetParams.overridenTargetPosition = newTargetPos;
                    targetParams.useOverridenTargetPosition = true;
                    targetParamsRef.ValueRW = targetParams;
                }
            }
        }

        [BurstCompile]
        public struct RequestChangeFlockGroupTargetJob : IJob {
            [ReadOnly] public NativeArray<Entity> flockGroupEntities;
            [WriteOnly] public ComponentLookup<RequestChangeFlockGroupTarget> requestChangeFlockGroupTargetLookup;

            [BurstCompile]
            public void Execute() {
                int count = flockGroupEntities.Length;
                for (int i = 0; i < count; i++) {
                    var flockGroupEntity = flockGroupEntities[i];
                    requestChangeFlockGroupTargetLookup[flockGroupEntity] = new RequestChangeFlockGroupTarget(true);
                }
            }
        }
    }
}