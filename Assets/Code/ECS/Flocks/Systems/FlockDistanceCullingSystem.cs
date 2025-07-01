using Awaken.CommonInterfaces;
using Awaken.ECS.Components;
using Awaken.Utility.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [BurstCompile]
    public partial class FlockDistanceCullingSystem : SystemBase {
        public const float HysteresisDistance = 5f;
        public const float FlockEntitiesMaxDistanceOutsideGroupBox = 10;

        EntityQuery _flockGroupEntitiesQuery;
        EntityQuery _culledFlockGroupEntitiesQuery, _unCulledFlockGroupEntitiesQuery;

        ComponentLookup<CulledEntityTag> _culledEntityTagLookup;
        NativeArray<Entity> _cullingResults;

        protected override void OnCreate() {
            base.OnCreate();
            _flockGroupEntitiesQuery = SystemAPI.QueryBuilder()
                .WithPresent<FlockData>().Build();

            _culledFlockGroupEntitiesQuery = SystemAPI.QueryBuilder()
                .WithPresent<FlockGroupEntity, CulledEntityTag>().Build();
            _unCulledFlockGroupEntitiesQuery = SystemAPI.QueryBuilder()
                .WithPresent<FlockGroupEntity>().WithNone<CulledEntityTag>().Build();

            _culledEntityTagLookup = SystemAPI.GetComponentLookup<CulledEntityTag>(true);
            _cullingResults = new NativeArray<Entity>(2, ARAlloc.Persistent);
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            Dependency.Complete();
            _cullingResults.Dispose();
        }

        protected override void OnUpdate() {
            Dependency.Complete();
            var flockGroupEntityToCull = _cullingResults[FlockGroupCullingJob.EntityToCullIndex];
            var flockGroupEntityToUnCull = _cullingResults[FlockGroupCullingJob.EntityToUnCullIndex];
            var entityManager = EntityManager;
            if (flockGroupEntityToUnCull != Entity.Null) {
                ref var culledFlockGroupEntitiesQuery = ref _culledFlockGroupEntitiesQuery;
                UnCullFlockEntities(in flockGroupEntityToUnCull, ref entityManager, ref culledFlockGroupEntitiesQuery);
            }
            // Use "else if" because unCulling and culling in one frame can be too big performance hit 
            else if (flockGroupEntityToCull != Entity.Null) {
                ref var unCulledFlockGroupEntitiesQuery = ref _unCulledFlockGroupEntitiesQuery;
                CullFlockEntities(in flockGroupEntityToCull, ref entityManager, ref unCulledFlockGroupEntitiesQuery);
            }

            var heroPos = (float3)HeroPosition.Value;
            _cullingResults[0] = Entity.Null;
            _cullingResults[1] = Entity.Null;
            _culledEntityTagLookup.Update(this);
            Dependency = new FlockGroupCullingJob() {
                referencePosition = heroPos,
                culledEntityTagLookup = _culledEntityTagLookup,
                outResult = _cullingResults,
            }.Schedule(_flockGroupEntitiesQuery, Dependency);
        }

        [BurstCompile]
        static void UnCullFlockEntities(in Entity flockGroupEntityToUncull, ref EntityManager entityManager, ref EntityQuery culledFlockEntitiesQuery) {
            culledFlockEntitiesQuery.SetSharedComponentFilter(new FlockGroupEntity(flockGroupEntityToUncull));
            entityManager.RemoveComponent<CulledEntityTag>(culledFlockEntitiesQuery);
            entityManager.RemoveComponent<CulledEntityTag>(flockGroupEntityToUncull);
        }

        [BurstCompile]
        static void CullFlockEntities(in Entity flockGroupEntityToCull, ref EntityManager entityManager, ref EntityQuery unCulledFlockEntitiesQuery) {
            unCulledFlockEntitiesQuery.SetSharedComponentFilter(new FlockGroupEntity(flockGroupEntityToCull));
            entityManager.AddComponent<CulledEntityTag>(unCulledFlockEntitiesQuery);
            entityManager.AddComponent<CulledEntityTag>(flockGroupEntityToCull);
        }

        [BurstCompile]
        public partial struct FlockGroupCullingJob : IJobEntity {
            public const int EntityToUnCullIndex = 0;
            public const int EntityToCullIndex = 1;

            [ReadOnly] public ComponentLookup<CulledEntityTag> culledEntityTagLookup;
            public float3 referencePosition;
            [NativeDisableParallelForRestriction] public NativeArray<Entity> outResult;

            void Execute(Entity flockGroupEntity, in FlockData data) {
                var distanceSq = data.AreaAABB.DistanceSq(referencePosition);
                bool isCulled = culledEntityTagLookup.HasComponent(flockGroupEntity);
                var unCullingDistance = data.flockGroupSimulationDistance + FlockEntitiesMaxDistanceOutsideGroupBox;
                if ((distanceSq < math.square(unCullingDistance)) & isCulled) {
                    // un-cull
                    outResult[EntityToUnCullIndex] = flockGroupEntity;
                } else if ((distanceSq > math.square(unCullingDistance + HysteresisDistance)) & !isCulled) {
                    // cull
                    outResult[EntityToCullIndex] = flockGroupEntity;
                }
            }
        }
    }
}