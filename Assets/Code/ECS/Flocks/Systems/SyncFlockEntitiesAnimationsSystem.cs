using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer;
using TAO.VertexAnimation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Awaken.ECS.Flocks {
    [UpdateInGroup(typeof(FlockSystemGroup), OrderLast = true)]
    [BurstCompile] [RequireMatchingQueriesForUpdate]
    public partial class SyncFlockEntitiesAnimationsSystem : SystemBase {
        ComponentLookup<VA_AnimatorParams> _animatorParamsLookup;
        EntityQuery _query;

        protected override void OnCreate() {
            base.OnCreate();
            _animatorParamsLookup = GetComponentLookup<VA_AnimatorParams>();
            _query = SystemAPI.QueryBuilder()
                .WithPresent<DrakeVisualEntity, FlockAnimatorParams>()
                .WithNone<CulledEntityTag>().Build();
        }

        protected override void OnUpdate() {
            _animatorParamsLookup.Update(this);
            
            Dependency = new SyncDrakeEntitiesAnimations() {
                animatorParamsLookup = _animatorParamsLookup
            }.ScheduleParallel(_query, Dependency);
        }

        [BurstCompile]
        public partial struct SyncDrakeEntitiesAnimations : IJobEntity {
            [WriteOnly, NativeDisableParallelForRestriction]
            public ComponentLookup<VA_AnimatorParams> animatorParamsLookup;

            [BurstCompile]
            void Execute(in DynamicBuffer<DrakeVisualEntity> drakeEntityVisualEntities, in FlockAnimatorParams animatorParams) {
                int visualEntitiesCount = drakeEntityVisualEntities.Length;
                for (int i = 0; i < visualEntitiesCount; i++) {
                    animatorParamsLookup.GetRefRW(drakeEntityVisualEntities[i]).ValueRW = animatorParams.value;
                }
            }
        }
    }
}