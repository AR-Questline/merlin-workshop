using Awaken.CommonInterfaces;
using Awaken.ECS.Components;
using Awaken.ECS.Critters.Components;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Critters {
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [BurstCompile]
    public partial class CrittersDistanceCullingSystem : SystemBase {
        public const float HysteresisDistance = 5f;
        public const float CrittersMaxDistanceOutsideSpawnerRadius = 2;

        EntityQuery _crittersGroupEntitiesQuery;
        EntityQuery _culledCrittersGroupEntitiesQuery, _unCulledCrittersGroupEntitiesQuery;

        ComponentLookup<CulledEntityTag> _culledEntityTagLookup;
        NativeArray<Entity> _cullingResults;

        protected override void OnCreate() {
            base.OnCreate();
            _crittersGroupEntitiesQuery = SystemAPI.QueryBuilder()
                .WithPresent<CrittersGroupData>().Build();

            _culledCrittersGroupEntitiesQuery = SystemAPI.QueryBuilder()
                .WithPresent<CrittersGroupEntity, CulledEntityTag>().Build();
            _unCulledCrittersGroupEntitiesQuery = SystemAPI.QueryBuilder()
                .WithPresent<CrittersGroupEntity>().WithNone<CulledEntityTag>().Build();

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
            var crittersGroupEntityToCull = _cullingResults[CrittersGroupCullingJob.EntityToCullIndex];
            var crittersGroupEntityToUnCull = _cullingResults[CrittersGroupCullingJob.EntityToUnCullIndex];
            var entityManager = EntityManager;
            if (crittersGroupEntityToUnCull != Entity.Null) {
                ref var culledCrittersGroupEntitiesQuery = ref _culledCrittersGroupEntitiesQuery;
                UnCullCritterEntities(in crittersGroupEntityToUnCull, ref entityManager, ref culledCrittersGroupEntitiesQuery);
            }
            // Use "else if" because unCulling and culling in one frame can be too big performance hit 
            else if (crittersGroupEntityToCull != Entity.Null) {
                ref var unCulledCrittersGroupEntitiesQuery = ref _unCulledCrittersGroupEntitiesQuery;
                CullCritterEntities(in crittersGroupEntityToCull, ref entityManager, ref unCulledCrittersGroupEntitiesQuery);
            }

            var heroPos = (float3)HeroPosition.Value;
            _cullingResults[0] = Entity.Null;
            _cullingResults[1] = Entity.Null;
            _culledEntityTagLookup.Update(this);
            Dependency = new CrittersGroupCullingJob() {
                referencePosition = heroPos,
                culledEntityTagLookup = _culledEntityTagLookup,
                outResult = _cullingResults,
            }.Schedule(_crittersGroupEntitiesQuery, Dependency);
        }

        [BurstCompile]
        static void UnCullCritterEntities(in Entity critterGroupEntityToUncull, ref EntityManager entityManager, ref EntityQuery culledCrittersGroupEntitiesQuery) {
            culledCrittersGroupEntitiesQuery.SetSharedComponentFilter(new CrittersGroupEntity(critterGroupEntityToUncull));
            entityManager.RemoveComponent<CulledEntityTag>(culledCrittersGroupEntitiesQuery);
            entityManager.RemoveComponent<CulledEntityTag>(critterGroupEntityToUncull);
        }

        [BurstCompile]
        static void CullCritterEntities(in Entity critterGroupEntityToCull, ref EntityManager entityManager, ref EntityQuery unCulledCrittersGroupEntitiesQuery) {
            unCulledCrittersGroupEntitiesQuery.SetSharedComponentFilter(new CrittersGroupEntity(critterGroupEntityToCull));
            entityManager.AddComponent<CulledEntityTag>(unCulledCrittersGroupEntitiesQuery);
            entityManager.AddComponent<CulledEntityTag>(critterGroupEntityToCull);
        }

        [BurstCompile]
        public partial struct CrittersGroupCullingJob : IJobEntity {
            public const int EntityToUnCullIndex = 0;
            public const int EntityToCullIndex = 1;

            [ReadOnly] public ComponentLookup<CulledEntityTag> culledEntityTagLookup;
            public float3 referencePosition;
            [NativeDisableParallelForRestriction] public NativeArray<Entity> outResult;

            void Execute(Entity crittersGroupEntity, in CrittersGroupData data) {
                var distanceSq = mathExt.GetDistanceSqToOutsideSphereSurface(data.spawnerCenter, data.spawnerRadius, referencePosition);
                bool isCulled = culledEntityTagLookup.HasComponent(crittersGroupEntity);
                var unCullingDistance = data.cullingDistance + CrittersMaxDistanceOutsideSpawnerRadius;
                if ((distanceSq < math.square(unCullingDistance)) & isCulled) {
                    // un-cull
                    outResult[EntityToUnCullIndex] = crittersGroupEntity;
                } else if ((distanceSq > math.square(unCullingDistance + HysteresisDistance)) & !isCulled) {
                    // cull
                    outResult[EntityToCullIndex] = crittersGroupEntity;
                }
            }
        }
    }
}