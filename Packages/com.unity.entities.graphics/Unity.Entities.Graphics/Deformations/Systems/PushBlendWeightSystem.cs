using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Deformations;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;

namespace Unity.Rendering
{
    [RequireMatchingQueriesForUpdate]
    partial class PushBlendWeightSystem : SystemBase
    {
        static readonly ProfilerMarker k_Marker = new ProfilerMarker("PrepareBlendWeightForGPU");

        EntityQuery m_BlendShapedEntityQuery;

        PushMeshDataSystem m_PushMeshDataSystem;

        protected override void OnCreate()
        {
            if (!UnityEngine.SystemInfo.supportsComputeShaders)
            {
                Enabled = false;
                return;
            }

            m_PushMeshDataSystem = World.GetOrCreateSystemManaged<PushMeshDataSystem>();
            Assert.IsNotNull(m_PushMeshDataSystem, $"{nameof(PushMeshDataSystem)} system was not found in the world!");

            m_BlendShapedEntityQuery = GetEntityQuery(
                ComponentType.ReadOnly<SharedMeshTracker>(),
                ComponentType.ReadOnly<BlendWeightBufferIndex>(),
                ComponentType.ReadOnly<DeformedEntity>()
            );
        }

        [BurstCompile]
        [WithAll(typeof(SharedMeshTracker))]
        partial struct ConstructHashMapJob : IJobEntity
        {
            public NativeParallelMultiHashMap<Entity, int>.ParallelWriter DeformedEntityToComputeIndexParallel;

            private void Execute(in BlendWeightBufferIndex index, in DeformedEntity deformedEntity)
            {
                // Skip if we have an invalid index.
                if (index.Value == BlendWeightBufferIndex.Null)
                    return;

                DeformedEntityToComputeIndexParallel.Add(deformedEntity.Value, index.Value);
            }
        }

        [BurstCompile]
        partial struct CopyBlendShapeWeightsToGPUJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction] public NativeArray<float> BlendShapeWeightsBuffer;
            [ReadOnly] public NativeParallelMultiHashMap<Entity, int> DeformedEntityToComputeIndex;

            private void Execute(in DynamicBuffer<BlendShapeWeight> weights, in Entity entity)
            {
                // Not all deformed entities in the world will have a renderer attached to them.
                if (!DeformedEntityToComputeIndex.ContainsKey(entity))
                    return;

                var length = weights.Length * UnsafeUtility.SizeOf<float>();
                var indices = DeformedEntityToComputeIndex.GetValuesForKey(entity);

                foreach (var index in indices)
                {
                    unsafe
                    {
                        UnsafeUtility.MemCpy(
                            (float*)BlendShapeWeightsBuffer.GetUnsafePtr() + index,
                            weights.GetUnsafeReadOnlyPtr(),
                            length
                        );
                    }
                }
            }
        }
        
        protected override void OnUpdate()
        {
            if (m_PushMeshDataSystem.BlendShapeWeightCount == 0)
                return;

            k_Marker.Begin();

            var deformedEntityToComputeIndex = new NativeParallelMultiHashMap<Entity, int>(m_BlendShapedEntityQuery.CalculateEntityCount(), Allocator.TempJob);
            var deformedEntityToComputeIndexParallel = deformedEntityToComputeIndex.AsParallelWriter();
            Dependency = new ConstructHashMapJob
            {
                DeformedEntityToComputeIndexParallel = deformedEntityToComputeIndexParallel
            }.ScheduleParallel(Dependency);

            var blendShapeWeightsBuffer = m_PushMeshDataSystem.BlendShapeBufferManager.LockBlendWeightBufferForWrite(m_PushMeshDataSystem.BlendShapeWeightCount);
            Dependency = new CopyBlendShapeWeightsToGPUJob
            {
                BlendShapeWeightsBuffer = blendShapeWeightsBuffer,
                DeformedEntityToComputeIndex = deformedEntityToComputeIndex
            }.ScheduleParallel(Dependency);

            Dependency = deformedEntityToComputeIndex.Dispose(Dependency);

            k_Marker.End();
        }
    }
}
