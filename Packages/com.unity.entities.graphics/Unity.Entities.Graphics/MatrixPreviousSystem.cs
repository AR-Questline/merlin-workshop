using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Unity.Rendering
{
    //@TODO: Updating always necessary due to empty component group. When Component group and archetype chunks are unified, [RequireMatchingQueriesForUpdate] can be added.
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(EntitiesGraphicsSystem))]
    internal partial class MatrixPreviousSystem : SystemBase
    {
        private EntityQuery m_GroupPrev;

        [BurstCompile]
        struct UpdateMatrixPrevious : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorldTypeHandle;
            public ComponentTypeHandle<BuiltinMaterialPropertyUnity_MatrixPreviousM> MatrixPreviousTypeHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // This job is not written to support queries with enableable component types.
                Assert.IsFalse(useEnabledMask);

                var chunkLocalToWorld = chunk.GetNativeArray(ref LocalToWorldTypeHandle);
                var chunkMatrixPrevious = chunk.GetNativeArray(ref MatrixPreviousTypeHandle);
                for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++)
                {
                    var localToWorld = chunkLocalToWorld[i].Value;
                    chunkMatrixPrevious[i] = new BuiltinMaterialPropertyUnity_MatrixPreviousM {Value = localToWorld};
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            if (!EntitiesGraphicsSystem.EntitiesGraphicsEnabled)
            {
                Enabled = false;
                return;
            }

            m_GroupPrev = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ReadWrite<BuiltinMaterialPropertyUnity_MatrixPreviousM>(),
                },
                None = new []
                {
                    ComponentType.ReadOnly<SkipBuiltinMaterialPropertyUnity_MatrixPreviousMUpdate>()
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
            m_GroupPrev.SetChangedVersionFilter(new[]
            {
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<BuiltinMaterialPropertyUnity_MatrixPreviousM>()
            });
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var updateMatrixPreviousJob = new UpdateMatrixPrevious
            {
                LocalToWorldTypeHandle = GetComponentTypeHandle<LocalToWorld>(true),
                MatrixPreviousTypeHandle = GetComponentTypeHandle<BuiltinMaterialPropertyUnity_MatrixPreviousM>(),
            };
            Dependency = updateMatrixPreviousJob.ScheduleParallel(m_GroupPrev, Dependency);
        }
    }
}
