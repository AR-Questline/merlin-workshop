using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Rendering
{
    [BurstCompile]
    internal struct EntitiesGraphicsChunkUpdater
    {
        public ComponentTypeCache.BurstCompatibleTypeArray ComponentTypes;

        [NativeDisableParallelForRestriction]
        public NativeArray<long> UnreferencedBatchIndices;

        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public NativeArray<ChunkProperty> ChunkProperties;

        [NativeDisableParallelForRestriction]
        public NativeArray<GpuUploadOperation> GpuUploadOperations;
        public NativeArray<int> NumGpuUploadOperations;

        [NativeDisableParallelForRestriction]
        public NativeArray<ThreadLocalAABB> ThreadLocalAABBs;

        public uint LastSystemVersion;

#pragma warning disable 649
        [NativeSetThreadIndex] public int ThreadIndex;
#pragma warning restore 649

        public int LocalToWorldType;
        public int WorldToLocalType;
        public int PrevLocalToWorldType;
        public int PrevWorldToLocalType;

#if PROFILE_BURST_JOB_INTERNALS
        public ProfilerMarker ProfileAddUpload;
        public ProfilerMarker ProfilePickingMatrices;
#endif

        unsafe void MarkBatchAsReferenced(int batchIndex)
        {
            // If the batch is referenced, remove it from the unreferenced bitfield

            AtomicHelpers.IndexToQwIndexAndMask(batchIndex, out int qw, out long mask);

            Assert.IsTrue(qw < UnreferencedBatchIndices.Length, "Batch index out of bounds");

            AtomicHelpers.AtomicAnd(
                (long*)UnreferencedBatchIndices.GetUnsafePtr(),
                qw,
                ~mask);
        }

        public void ProcessChunk(in EntitiesGraphicsChunkInfo chunkInfo, ArchetypeChunk chunk, ChunkWorldRenderBounds chunkBounds)
        {
#if DEBUG_LOG_CHUNKS
            Debug.Log($"HybridChunkUpdater.ProcessChunk(internalBatchIndex: {chunkInfo.BatchIndex}, valid: {chunkInfo.Valid}, count: {chunk.Count}, chunk: {chunk.GetHashCode()})");
#endif

            if (chunkInfo.Valid)
                ProcessValidChunk(chunkInfo, chunk, chunkBounds.Value, false);
        }

        public unsafe void ProcessValidChunk(in EntitiesGraphicsChunkInfo chunkInfo, ArchetypeChunk chunk,
            MinMaxAABB chunkAABB, bool isNewChunk)
        {
            if (!isNewChunk)
                MarkBatchAsReferenced(chunkInfo.BatchIndex);

            bool structuralChanges = chunk.DidOrderChange(LastSystemVersion);

            var dstOffsetWorldToLocal = -1;
            var dstOffsetPrevWorldToLocal = -1;

            fixed(DynamicComponentTypeHandle* fixedT0 = &ComponentTypes.t0)
            {
                for (int i = chunkInfo.ChunkTypesBegin; i < chunkInfo.ChunkTypesEnd; ++i)
                {
                    var chunkProperty = ChunkProperties[i];
                    var type = chunkProperty.ComponentTypeIndex;
                    if (type == WorldToLocalType)
                        dstOffsetWorldToLocal = chunkProperty.GPUDataBegin;
                    else if (type == PrevWorldToLocalType)
                        dstOffsetPrevWorldToLocal = chunkProperty.GPUDataBegin;
                }

                for (int i = chunkInfo.ChunkTypesBegin; i < chunkInfo.ChunkTypesEnd; ++i)
                {
                    var chunkProperty = ChunkProperties[i];
                    var type = ComponentTypes.Type(fixedT0, chunkProperty.ComponentTypeIndex);

                    var chunkType = chunkProperty.ComponentTypeIndex;
                    var isLocalToWorld = chunkType == LocalToWorldType;
                    var isWorldToLocal = chunkType == WorldToLocalType;
                    var isPrevLocalToWorld = chunkType == PrevLocalToWorldType;
                    var isPrevWorldToLocal = chunkType == PrevWorldToLocalType;

                    var skipComponent = (isWorldToLocal || isPrevWorldToLocal);

                    bool componentChanged = chunk.DidChange(ref type, LastSystemVersion);
                    bool copyComponentData = (isNewChunk || structuralChanges || componentChanged) && !skipComponent;

                    if (copyComponentData)
                    {
#if DEBUG_LOG_PROPERTY_UPDATES
                        Debug.Log($"UpdateChunkProperty(internalBatchIndex: {chunkInfo.BatchIndex}, property: {i}, elementSize: {chunkProperty.ValueSizeBytesCPU})");
#endif

                        var src = chunk.GetDynamicComponentDataArrayReinterpret<int>(ref type,
                            chunkProperty.ValueSizeBytesCPU);

#if PROFILE_BURST_JOB_INTERNALS
                        ProfileAddUpload.Begin();
#endif

                        int sizeBytes = (int)((uint)chunk.Count * (uint)chunkProperty.ValueSizeBytesCPU);
                        var srcPtr = src.GetUnsafeReadOnlyPtr();
                        var dstOffset = chunkProperty.GPUDataBegin;
                        if (isLocalToWorld || isPrevLocalToWorld)
                        {
                            var numMatrices = sizeBytes / sizeof(float4x4);
                            AddMatrixUpload(
                                srcPtr,
                                numMatrices,
                                dstOffset,
                                isLocalToWorld ? dstOffsetWorldToLocal : dstOffsetPrevWorldToLocal,
                                (chunkProperty.ValueSizeBytesCPU == 4 * 4 * 3)
                                    ? ThreadedSparseUploader.MatrixType.MatrixType3x4
                                    : ThreadedSparseUploader.MatrixType.MatrixType4x4,
                                (chunkProperty.ValueSizeBytesGPU == 4 * 4 * 3)
                                    ? ThreadedSparseUploader.MatrixType.MatrixType3x4
                                    : ThreadedSparseUploader.MatrixType.MatrixType4x4);
                        }
                        else
                        {
                            AddUpload(
                                srcPtr,
                                sizeBytes,
                                dstOffset);
                        }
#if PROFILE_BURST_JOB_INTERNALS
                        ProfileAddUpload.End();
#endif
                    }
                }
            }

            UpdateAABB(chunkAABB);
        }

        private unsafe void UpdateAABB(MinMaxAABB chunkAABB)
        {
            var threadLocalAABB = ((ThreadLocalAABB*) ThreadLocalAABBs.GetUnsafePtr()) + ThreadIndex;
            ref var aabb = ref threadLocalAABB->AABB;
            aabb.Encapsulate(chunkAABB);
        }

        private unsafe void AddUpload(void* srcPtr, int sizeBytes, int dstOffset)
        {
            int* numGpuUploadOperations = (int*) NumGpuUploadOperations.GetUnsafePtr();
            int index = System.Threading.Interlocked.Add(ref numGpuUploadOperations[0], 1) - 1;

            if (index < GpuUploadOperations.Length)
            {
                GpuUploadOperations[index] = new GpuUploadOperation
                {
                    Kind = GpuUploadOperation.UploadOperationKind.Memcpy,
                    Src = srcPtr,
                    DstOffset = dstOffset,
                    DstOffsetInverse = -1,
                    Size = sizeBytes,
                };
            }
            else
            {
                // Debug.Assert(false, "Maximum amount of GPU upload operations exceeded");
            }
        }

        private unsafe void AddMatrixUpload(
            void* srcPtr,
            int numMatrices,
            int dstOffset,
            int dstOffsetInverse,
            ThreadedSparseUploader.MatrixType matrixTypeCpu,
            ThreadedSparseUploader.MatrixType matrixTypeGpu)
        {
            int* numGpuUploadOperations = (int*) NumGpuUploadOperations.GetUnsafePtr();
            int index = System.Threading.Interlocked.Add(ref numGpuUploadOperations[0], 1) - 1;

            if (index < GpuUploadOperations.Length)
            {
                GpuUploadOperations[index] = new GpuUploadOperation
                {
                    Kind = (matrixTypeGpu == ThreadedSparseUploader.MatrixType.MatrixType3x4)
                        ? GpuUploadOperation.UploadOperationKind.SOAMatrixUpload3x4
                        : GpuUploadOperation.UploadOperationKind.SOAMatrixUpload4x4,
                    SrcMatrixType = matrixTypeCpu,
                    Src = srcPtr,
                    DstOffset = dstOffset,
                    DstOffsetInverse = dstOffsetInverse,
                    Size = numMatrices,
                };
            }
            else
            {
                // Debug.Assert(false, "Maximum amount of GPU upload operations exceeded");
            }
        }
    }

    [BurstCompile]
    internal struct ClassifyNewChunksJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<ChunkHeader> ChunkHeader;
        [ReadOnly] public ComponentTypeHandle<EntitiesGraphicsChunkInfo> EntitiesGraphicsChunkInfo;

        [NativeDisableParallelForRestriction]
        public NativeArray<ArchetypeChunk> NewChunks;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> NumNewChunks;

        public void Execute(in ArchetypeChunk metaChunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            // This job is not written to support queries with enableable component types.
            Assert.IsFalse(useEnabledMask);

            var chunkHeaders = metaChunk.GetNativeArray(ref ChunkHeader);
            var entitiesGraphicsChunkInfos = metaChunk.GetNativeArray(ref EntitiesGraphicsChunkInfo);

            for (int i = 0, chunkEntityCount = metaChunk.Count; i < chunkEntityCount; i++)
            {
                var chunkInfo = entitiesGraphicsChunkInfos[i];
                var chunkHeader = chunkHeaders[i];

                if (ShouldCountAsNewChunk(chunkInfo, chunkHeader.ArchetypeChunk))
                {
                    ClassifyNewChunk(chunkHeader.ArchetypeChunk);
                }
            }
        }

        bool ShouldCountAsNewChunk(in EntitiesGraphicsChunkInfo chunkInfo, in ArchetypeChunk chunk)
        {
            return !chunkInfo.Valid && !chunk.Archetype.Prefab && !chunk.Archetype.Disabled;
        }

        public unsafe void ClassifyNewChunk(ArchetypeChunk chunk)
        {
            int* numNewChunks = (int*)NumNewChunks.GetUnsafePtr();
            int iPlus1 = System.Threading.Interlocked.Add(ref numNewChunks[0], 1);
            int i = iPlus1 - 1; // C# Interlocked semantics are weird
            Assert.IsTrue(i < NewChunks.Length, "Out of space in the NewChunks buffer");
            NewChunks[i] = chunk;
        }
    }

    [BurstCompile]
    internal struct UpdateOldEntitiesGraphicsChunksJob : IJobChunk
    {
        public ComponentTypeHandle<EntitiesGraphicsChunkInfo> EntitiesGraphicsChunkInfo;
        [ReadOnly] public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBounds;
        [ReadOnly] public ComponentTypeHandle<ChunkHeader> ChunkHeader;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorld;
        [ReadOnly] public ComponentTypeHandle<LODRange> LodRange;
        [ReadOnly] public ComponentTypeHandle<MaterialMeshInfo> MaterialMeshInfo;
        public EntitiesGraphicsChunkUpdater EntitiesGraphicsChunkUpdater;

        public void Execute(in ArchetypeChunk metaChunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            // This job is not written to support queries with enableable component types.
            Assert.IsFalse(useEnabledMask);

            // metaChunk is the chunk which contains the meta entities (= entities holding the chunk components) for the actual chunks

            var entitiesGraphicsChunkInfos = metaChunk.GetNativeArray(ref EntitiesGraphicsChunkInfo);
            var chunkHeaders = metaChunk.GetNativeArray(ref ChunkHeader);
            var chunkBoundsArray = metaChunk.GetNativeArray(ref ChunkWorldRenderBounds);

            for (int i = 0, chunkEntityCount = metaChunk.Count; i < chunkEntityCount; i++)
            {
                var chunkInfo = entitiesGraphicsChunkInfos[i];
                var chunkHeader = chunkHeaders[i];
                var chunk = chunkHeader.ArchetypeChunk;

                // Skip chunks that for some reason have EntitiesGraphicsChunkInfo, but don't have the
                // other required components. This should normally not happen, but can happen
                // if the user manually deletes some components after the fact.
                bool hasMaterialMeshInfo = chunk.Has(ref MaterialMeshInfo);
                bool hasLocalToWorld = chunk.Has(ref LocalToWorld);

                if (!math.all(new bool2(hasMaterialMeshInfo, hasLocalToWorld)))
                    continue;

                ChunkWorldRenderBounds chunkBounds = chunkBoundsArray[i];

                bool localToWorldChange = chunkHeader.ArchetypeChunk.DidChange(ref LocalToWorld, EntitiesGraphicsChunkUpdater.LastSystemVersion);

                // When LOD ranges change, we must reset the movement grace to avoid using stale data
                bool lodRangeChange =
                    chunkHeader.ArchetypeChunk.DidOrderChange(EntitiesGraphicsChunkUpdater.LastSystemVersion) |
                    chunkHeader.ArchetypeChunk.DidChange(ref LodRange, EntitiesGraphicsChunkUpdater.LastSystemVersion);

                if (lodRangeChange)
                {
                    chunkInfo.CullingData.MovementGraceFixed16 = 0;
                    entitiesGraphicsChunkInfos[i] = chunkInfo;
                }

                EntitiesGraphicsChunkUpdater.ProcessChunk(chunkInfo, chunkHeader.ArchetypeChunk, chunkBounds);
            }
        }
    }

    [BurstCompile]
    internal struct UpdateNewEntitiesGraphicsChunksJob : IJobParallelFor
    {
        [ReadOnly] public ComponentTypeHandle<EntitiesGraphicsChunkInfo> EntitiesGraphicsChunkInfo;
        [ReadOnly] public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBounds;

        public NativeArray<ArchetypeChunk> NewChunks;
        public EntitiesGraphicsChunkUpdater EntitiesGraphicsChunkUpdater;

        public void Execute(int index)
        {
            var chunk = NewChunks[index];
            var chunkInfo = chunk.GetChunkComponentData(ref EntitiesGraphicsChunkInfo);

            ChunkWorldRenderBounds chunkBounds = chunk.GetChunkComponentData(ref ChunkWorldRenderBounds);

            Assert.IsTrue(chunkInfo.Valid, "Attempted to process a chunk with uninitialized Hybrid chunk info");
            EntitiesGraphicsChunkUpdater.ProcessValidChunk(chunkInfo, chunk, chunkBounds.Value, true);
        }
    }

}
