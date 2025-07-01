using System.Collections.Generic;
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
    [BurstCompile] [RequireMatchingQueriesForUpdate]
    public partial class FlockRestSpotSystem : SystemBase {
        EntityQuery _flockEntitiesQuery;
        ComponentLookup<TargetParams> _targetParamsLookup;
        ComponentLookup<FlockRestSpotStatus> _flockRestSpotStatusLookup;
        NativeList<EntityAndDistanceSqToSpot> _entitiesInRestSpotRadius;
        EntityQuery _restSpotsQuery;
        bool _releaseAll;
        float _spotsForceActiveTimeEnd;
        bool _forceSpotsActive;
        public bool ForceSpotsActive => _forceSpotsActive;
        public float ElapsedTime { get; private set; }

        protected override void OnCreate() {
            base.OnCreate();
            _flockEntitiesQuery = SystemAPI.QueryBuilder()
                .WithPresent<DrakeVisualEntitiesTransform, FlockGroupEntity>()
                .WithPresentRW<TargetParams>()
                .WithNone<CulledEntityTag>().Build();

            _restSpotsQuery = SystemAPI.QueryBuilder()
                .WithPresent<FlockRestSpotStatus, FlockRestSpotData>()
                .WithPresentRW<RestSpotTryFindOrRelease, FlockRestSpotTimeData>()
                .WithNone<CulledEntityTag>().Build();

            _targetParamsLookup = GetComponentLookup<TargetParams>();
            _flockRestSpotStatusLookup = GetComponentLookup<FlockRestSpotStatus>();
            _entitiesInRestSpotRadius = new NativeList<EntityAndDistanceSqToSpot>(FindEntitiesInRestSpotRadius.MaxEntitiesCount, ARAlloc.Persistent);
        }

        protected override void OnUpdate() {
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime == 0) {
                return;
            }

            Dependency.Complete();
            var elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            ElapsedTime = elapsedTime;
            _forceSpotsActive = elapsedTime < _spotsForceActiveTimeEnd;
            var forceSpotsActiveAndLocked = _spotsForceActiveTimeEnd == float.PositiveInfinity;
            Entity flockEntityToRelease = Entity.Null;
            Entity restSpotEntityToTryFindEntity = Entity.Null;
            FlockRestSpotData restSpotDataToTryFindEntity = default;
            foreach (var (tryFindOrRelease, statusDataRef, dataRef, entity) in SystemAPI.Query<
                         RefRW<RestSpotTryFindOrRelease>, RefRW<FlockRestSpotStatus>, RefRO<FlockRestSpotData>>().WithEntityAccess().WithNone<CulledEntityTag>()) {
                var statusData = statusDataRef.ValueRO;
                var data = dataRef.ValueRO;
                if (tryFindOrRelease.ValueRO.enabled == false & (!_forceSpotsActive)) {
                    continue;
                }

                if ((flockEntityToRelease.Equals(Entity.Null)) & (statusData.HasEntity) & (elapsedTime >= statusData.blockChangesUntilTime) & (!forceSpotsActiveAndLocked)) {
                    tryFindOrRelease.ValueRW.enabled = false;
                    statusDataRef.ValueRW.restingEntity = Entity.Null;
                    flockEntityToRelease = statusData.restingEntity;
                } else if (restSpotEntityToTryFindEntity.Equals(Entity.Null) & (statusDataRef.ValueRO.restingEntity.Equals(Entity.Null)) &
                           ((elapsedTime >= statusData.blockChangesUntilTime) | _forceSpotsActive)) {
                    // Set TryFindOrRelease = false to try to find entity in radius one time and then if failed timer will start delay again.
                    // Otherwise, rest spot would just wait in a state ready to catch entity and when larger flock of entities
                    // would come nearby all adjacent rest spots will trigger catch simultaneously. 
                    tryFindOrRelease.ValueRW.enabled = false;
                    restSpotEntityToTryFindEntity = entity;
                    restSpotDataToTryFindEntity = data;
                }
            }

            var entityManager = EntityManager;
            // Separate releasing and trying to catch action from entities for loop because if entity manager will 
            // be used after UseRestSpotForNearestEntity is scheduled - this will trigger an error
            if (flockEntityToRelease.Equals(Entity.Null) == false) {
                if (entityManager.HasComponent<TargetParams>(flockEntityToRelease)) {
                    var targetParams = entityManager.GetComponentData<TargetParams>(flockEntityToRelease);
                    targetParams.targetPositionIsRestPosition = false;
                    targetParams.overridenTargetPosition = targetParams.flockTargetPosition;
                    targetParams.useOverridenTargetPosition = false;
                    entityManager.SetComponentData(flockEntityToRelease, targetParams);
                }
            }

            if (restSpotEntityToTryFindEntity.Equals(Entity.Null) == false) {
                var data = restSpotDataToTryFindEntity;
                _flockEntitiesQuery.SetSharedComponentFilter(new FlockGroupEntity(data.flockGroupEntity));
                _entitiesInRestSpotRadius.Clear();
                Dependency = new FindEntitiesInRestSpotRadius() {
                    restSpotPosition = data.position,
                    restSpotRadiusSq = math.square(data.radius),
                    outEntitiesInRestSpotRadius = _entitiesInRestSpotRadius
                }.Schedule(_flockEntitiesQuery, Dependency);

                _targetParamsLookup.Update(this);
                _flockRestSpotStatusLookup.Update(this);

                Dependency = new UseRestSpotForNearestEntity() {
                    entitiesInRestSpotRadius = _entitiesInRestSpotRadius.AsDeferredJobArray(),
                    targetParamsLookup = _targetParamsLookup,
                    flockRestSpotStatusLookup = _flockRestSpotStatusLookup,
                    restPosition = data.position,
                    restSpotEntity = restSpotEntityToTryFindEntity,
                }.Schedule(Dependency);
            }

            if (_releaseAll) {
                _releaseAll = false;
                _targetParamsLookup.Update(this);
                var allowedLandingTime = elapsedTime + ReleaseEntitiesOnSpotsJob.BlockLandingTime;
                Dependency = new ReleaseEntitiesOnSpotsJob() {
                    targetParamsLookup = _targetParamsLookup,
                    allowedLandingTime = allowedLandingTime
                }.Schedule(Dependency);
            }

            Dependency = new UpdateTimerAndStateJob() {
                deltaTime = deltaTime,
                currentTime = elapsedTime
            }.Schedule(_restSpotsQuery, Dependency);
        }

        public static void ReleaseAllEntitiesInRestSpots() {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<FlockRestSpotSystem>();
            system.ScheduleReleaseAllEntitiesInRestSpots();
        }

        public static void ForceActivateAllRestSpots(float spotsActiveTime) {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<FlockRestSpotSystem>();
            system.ScheduleForceActivateAllRestSpots(spotsActiveTime);
        }

        public static void StopForceActiveAllRestSpots() {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<FlockRestSpotSystem>();
            system.ScheduleStopForceActiveAllRestSpots();
        }

        public void ScheduleReleaseAllEntitiesInRestSpots() {
            _spotsForceActiveTimeEnd = 0;
            _releaseAll = true;
        }

        public void ScheduleForceActivateAllRestSpots(float spotsActiveTime) {
            _spotsForceActiveTimeEnd = (float)World.Time.ElapsedTime + spotsActiveTime;
        }

        public void ScheduleStopForceActiveAllRestSpots() {
            _spotsForceActiveTimeEnd = 0;
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            Dependency.Complete();
            _entitiesInRestSpotRadius.Dispose();
        }

        [BurstCompile]
        public partial struct UpdateTimerAndStateJob : IJobEntity {
            public float deltaTime;
            public double currentTime;

            [BurstCompile]
            void Execute(ref RestSpotTryFindOrRelease tryFindOrReleaseEntityRef, ref FlockRestSpotTimeData timeData, in FlockRestSpotStatus statusData, in FlockRestSpotData data) {
                timeData.waitTimeElapsed += deltaTime;
                var delayMinMax = math.select(data.autoCatchDelayMinMax, data.autoDismountDelayMinMax, statusData.HasEntity);
                var delay = StatelessRandom.GetRandomTime(delayMinMax, math.hash(new double2(currentTime, timeData.currentWaitTimeHash)));
                bool tryFindOrReleaseEntity = tryFindOrReleaseEntityRef.enabled;
                tryFindOrReleaseEntity |= (timeData.waitTimeElapsed >= delay);
                timeData.currentWaitTimeHash = math.select(
                    timeData.currentWaitTimeHash, math.hash(new uint2(timeData.currentWaitTimeHash)), tryFindOrReleaseEntity);
                timeData.waitTimeElapsed = math.select(timeData.waitTimeElapsed, 0, tryFindOrReleaseEntity);
                tryFindOrReleaseEntityRef.enabled = tryFindOrReleaseEntity;
            }
        }

        public struct EntityAndDistanceSqToSpot {
            public Entity entity;
            public float distanceSqToSpot;

            public EntityAndDistanceSqToSpot(Entity entity, float distanceSqToSpot) {
                this.entity = entity;
                this.distanceSqToSpot = distanceSqToSpot;
            }
        }

        [BurstCompile]
        partial struct FindEntitiesInRestSpotRadius : IJobEntity {
            public const int MaxEntitiesCount = 8;

            public float3 restSpotPosition;
            public float restSpotRadiusSq;
            public NativeList<EntityAndDistanceSqToSpot> outEntitiesInRestSpotRadius;

            [BurstCompile]
            void Execute(Entity entity, in DrakeVisualEntitiesTransform transform, in TargetParams targetParams) {
                var distanceSqToSpot = math.distancesq(transform.position, restSpotPosition);
                if (Hint.Unlikely((distanceSqToSpot < restSpotRadiusSq) & (outEntitiesInRestSpotRadius.Length < MaxEntitiesCount) & (targetParams.targetPositionIsRestPosition == false))) {
                    outEntitiesInRestSpotRadius.AddNoResize(new(entity, distanceSqToSpot));
                }
            }
        }

        [BurstCompile]
        struct UseRestSpotForNearestEntity : IJob {
            public NativeArray<EntityAndDistanceSqToSpot> entitiesInRestSpotRadius;
            public ComponentLookup<TargetParams> targetParamsLookup;
            public ComponentLookup<FlockRestSpotStatus> flockRestSpotStatusLookup;
            public float3 restPosition;
            public Entity restSpotEntity;

            [BurstCompile]
            public void Execute() {
                if (entitiesInRestSpotRadius.Length == 0) {
                    return;
                }

                entitiesInRestSpotRadius.Sort(new Comparer());
                var closestEntity = entitiesInRestSpotRadius[0].entity;
                var targetParams = targetParamsLookup[closestEntity];
                targetParams.useOverridenTargetPosition = true;
                targetParams.targetPositionIsRestPosition = true;
                targetParams.overridenTargetPosition = restPosition;
                targetParamsLookup[closestEntity] = targetParams;
                var restSpotStatusRef = flockRestSpotStatusLookup.GetRefRW(restSpotEntity);
                var restSpotStatus = restSpotStatusRef.ValueRO;
                restSpotStatus.restingEntity = closestEntity;
                restSpotStatusRef.ValueRW = restSpotStatus;
            }

            [BurstCompile]
            struct Comparer : IComparer<EntityAndDistanceSqToSpot> {
                [BurstCompile]
                public int Compare(EntityAndDistanceSqToSpot x, EntityAndDistanceSqToSpot y) {
                    return x.distanceSqToSpot.CompareTo(y.distanceSqToSpot);
                }
            }
        }
    }
}