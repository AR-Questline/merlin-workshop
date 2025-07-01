using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Rendering
{
    /// <summary>
    /// Updates previous matrix data to match LocalToWorld value on initialization.
    /// </summary>
    //@TODO: Updating always necessary due to empty component group. When Component group and archetype chunks are unified, [RequireMatchingQueriesForUpdate] can be added.
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(UpdatePresentationSystemGroup))]
    [UpdateBefore(typeof(EntitiesGraphicsSystem))]
    internal partial class MatrixPreviousInitializationSystem : SystemBase
    {
        private EntityQuery m_GroupPrev;

        [BurstCompile]
        public struct InitializeMatrixPrevious : IJobChunk
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
                    // The assumption is made here that if the initial value of the previous matrix is zero that
                    // it needs to be initialized to the localToWorld matrix value. This avoids issues with incorrect
                    // motion vector results on the first frame and entity is rendered.
                    if (chunkMatrixPrevious[i].Value.Equals(float4x4.zero))
                    {
                        chunkMatrixPrevious[i] = new BuiltinMaterialPropertyUnity_MatrixPreviousM { Value = localToWorld };
                    }
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
            m_GroupPrev.SetOrderVersionFilter();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var initializeMatrixPreviousJob = new InitializeMatrixPrevious
            {
                LocalToWorldTypeHandle = GetComponentTypeHandle<LocalToWorld>(true),
                MatrixPreviousTypeHandle = GetComponentTypeHandle<BuiltinMaterialPropertyUnity_MatrixPreviousM>(),
            };
            Dependency = initializeMatrixPreviousJob.ScheduleParallel(m_GroupPrev, Dependency);
        }
    }
}
