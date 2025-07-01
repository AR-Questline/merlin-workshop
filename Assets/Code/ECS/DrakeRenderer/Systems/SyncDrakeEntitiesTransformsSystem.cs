using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;

namespace Awaken.ECS.Flocks {
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [BurstCompile] [RequireMatchingQueriesForUpdate]
    public partial class SyncDrakeEntitiesTransformsSystem : SystemBase {
        ComponentLookup<LocalToWorld> _localToWorldLookup;
        ComponentLookup<LODWorldReferencePoint> _lodReferencePointLookup;
        ComponentLookup<WorldRenderBounds> _worldRenderBoundsLookup;
        EntityQuery _query;

        protected override void OnCreate() {
            base.OnCreate();
            _localToWorldLookup = GetComponentLookup<LocalToWorld>();
            _lodReferencePointLookup = GetComponentLookup<LODWorldReferencePoint>();
            _worldRenderBoundsLookup = GetComponentLookup<WorldRenderBounds>();
            _query = SystemAPI.QueryBuilder()
                .WithPresent<DrakeVisualEntity, DrakeVisualEntitiesTransform>()
                .WithNone<CulledEntityTag>().Build();
        }

        protected override void OnUpdate() {
            _localToWorldLookup.Update(this);
            _lodReferencePointLookup.Update(this);
            _worldRenderBoundsLookup.Update(this);
            Dependency = new SyncDrakeEntitiesVisualsTransformsJob() {
                localToWorldLookup = _localToWorldLookup,
                lodReferencePointLookup = _lodReferencePointLookup,
                worldRenderBoundsLookup = _worldRenderBoundsLookup
            }.ScheduleParallel(_query, Dependency);
        }

        
        [BurstCompile]
        public partial struct SyncDrakeEntitiesVisualsTransformsJob : IJobEntity {
            [WriteOnly, NativeDisableParallelForRestriction] public ComponentLookup<LocalToWorld> localToWorldLookup;
            [WriteOnly, NativeDisableParallelForRestriction] public ComponentLookup<LODWorldReferencePoint> lodReferencePointLookup;
            [WriteOnly, NativeDisableParallelForRestriction] public ComponentLookup<WorldRenderBounds> worldRenderBoundsLookup;

            void Execute(in DynamicBuffer<DrakeVisualEntity> drakeVisualEntities, in DrakeVisualEntitiesTransform drakeVisualEntitiesTransform) {
                int visualEntitiesCount = drakeVisualEntities.Length;
               
                for (int i = 0; i < visualEntitiesCount; i++) {
                    var visualEntity = drakeVisualEntities[i];
                    localToWorldLookup.GetRefRW(visualEntity).ValueRW.Value = drakeVisualEntitiesTransform.Matrix;
                    lodReferencePointLookup.GetRefRW(visualEntity).ValueRW.Value = drakeVisualEntitiesTransform.position;
                    worldRenderBoundsLookup.GetRefRW(visualEntity).ValueRW.Value.Center = drakeVisualEntitiesTransform.position;
                }
            }
        }
    }
}