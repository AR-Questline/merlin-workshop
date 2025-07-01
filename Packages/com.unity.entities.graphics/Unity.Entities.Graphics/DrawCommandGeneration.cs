using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Entities.Graphics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Unity.Rendering
{
    // Chunk-agnostic parts of draw
    internal unsafe struct DrawCommandSettings : IEquatable<DrawCommandSettings>
    {
        // TODO: This could be thinned to fit in 128 bits?

        public FilterSettingsIndex FilterIndex;
        public BatchDrawCommandFlags Flags;
        public BatchMaterialID MaterialID;
        public BatchMeshID MeshID;
        public ushort SplitMask;
        public ushort SubMeshIndex;
        public BatchID BatchID;
        private int m_CachedHash;

        public bool Equals(DrawCommandSettings other)
        {
            // Use temp variables so CPU can co-issue all comparisons
            bool eq_batch = BatchID == other.BatchID;
            bool eq_rest = math.all(PackedUint4 == other.PackedUint4);

            return eq_batch && eq_rest;
        }

        private uint4 PackedUint4
        {
            get
            {
                Assert.IsTrue(MeshID.value < (1 << 24));
                Assert.IsTrue(SubMeshIndex < (1 << 8));
                Assert.IsTrue((uint)Flags < (1 << 24));
                Assert.IsTrue(SplitMask < (1 << 8));

                return new uint4(
                    (uint)FilterIndex.GetHashCode(),
                    (((uint)SplitMask & 0xff) << 24) | ((uint)Flags & 0x00ffffffff),
                    MaterialID.value,
                    ((MeshID.value & 0x00ffffff) << 8) | ((uint)SubMeshIndex & 0xff)
                );
            }
        }

        public int CompareTo(DrawCommandSettings other)
        {
            uint4 a = PackedUint4;
            uint4 b = other.PackedUint4;
            int cmp_batchID = BatchID.CompareTo(other.BatchID);

            int4 lt = math.select(int4.zero, new int4(-1), a < b);
            int4 gt = math.select(int4.zero, new int4( 1), a > b);
            int4 neq = lt | gt;

            int* firstNonZero = stackalloc int[4];

            bool4 nz = neq != int4.zero;
            bool anyNz = math.any(nz);
            math.compress(firstNonZero, 0, neq, nz);

            return anyNz ? firstNonZero[0] : cmp_batchID;
        }

        // Used to verify correctness of fast CompareTo
        public int CompareToReference(DrawCommandSettings other)
        {
            int cmpFilterIndex = FilterIndex.CompareTo(other.FilterIndex);
            int cmpFlags = ((int)Flags).CompareTo((int)other.Flags);
            int cmpMaterialID = MaterialID.CompareTo(other.MaterialID);
            int cmpMeshID = MeshID.CompareTo(other.MeshID);
            int cmpSplitMask = SplitMask.CompareTo(other.SubMeshIndex);
            int cmpSubMeshIndex = SubMeshIndex.CompareTo(other.SubMeshIndex);
            int cmpBatchID = BatchID.CompareTo(other.BatchID);

            if (cmpFilterIndex != 0) return cmpFilterIndex;
            if (cmpFlags != 0) return cmpFlags;
            if (cmpMaterialID != 0) return cmpMaterialID;
            if (cmpMeshID != 0) return cmpMeshID;
            if (cmpSubMeshIndex != 0) return cmpSubMeshIndex;
            if (cmpSplitMask != 0) return cmpSplitMask;

            return cmpBatchID;
        }

        public override int GetHashCode() => m_CachedHash;

        public void ComputeHashCode()
        {
            m_CachedHash = ChunkDrawCommandOutput.FastHash(this);
        }

        public bool HasSortingPosition => (int) (Flags & BatchDrawCommandFlags.HasSortingPosition) != 0;

        public override string ToString()
        {
            return $"DrawCommandSettings(batchID: {BatchID.value}, materialID: {MaterialID.value}, meshID: {MeshID.value}, submesh: {SubMeshIndex}, filter: {FilterIndex}, flags: {Flags:x}, splitMask: {SplitMask:x})";
        }
    }

    internal unsafe struct ThreadLocalAllocator
    {
        public const int kInitialSize = 1024 * 1024;
        public const Allocator kAllocator = Allocator.Persistent;
        public static readonly int NumThreads = ChunkDrawCommandOutput.NumThreads;

        [StructLayout(LayoutKind.Explicit, Size = JobsUtility.CacheLineSize)]
        public unsafe struct PaddedAllocator
        {
            [FieldOffset(0)]
            public AllocatorHelper<RewindableAllocator> Allocator;
            [FieldOffset(16)]
            public bool UsedSinceRewind;

            public void Initialize(int initialSize)
            {
                Allocator = new AllocatorHelper<RewindableAllocator>(AllocatorManager.Persistent);
                Allocator.Allocator.Initialize(initialSize);
            }
        }

        public UnsafeList<PaddedAllocator> Allocators;

        public ThreadLocalAllocator(int expectedUsedCount = -1, int initialSize = kInitialSize)
        {
            // Note, the comparison is <= as on 32-bit builds this size will be smaller, which is fine.
            Assert.IsTrue(sizeof(AllocatorHelper<RewindableAllocator>) <= 16, $"PaddedAllocator's Allocator size has changed. The type layout needs adjusting.");
            Assert.IsTrue(sizeof(PaddedAllocator) >= JobsUtility.CacheLineSize,
                $"Thread local allocators should be on different cache lines. Size: {sizeof(PaddedAllocator)}, Cache Line: {JobsUtility.CacheLineSize}");

            if (expectedUsedCount < 0)
                expectedUsedCount = math.max(0, JobsUtility.JobWorkerCount + 1);

            Allocators = new UnsafeList<PaddedAllocator>(
                NumThreads,
                kAllocator,
                NativeArrayOptions.ClearMemory);
            Allocators.Resize(NumThreads);

            for (int i = 0; i < NumThreads; ++i)
            {
                if (i < expectedUsedCount)
                    Allocators.ElementAt(i).Initialize(initialSize);
                else
                    Allocators.ElementAt(i).Initialize(1);
            }
        }

        public void Rewind()
        {
            Profiler.BeginSample("RewindAllocators");
            for (int i = 0; i < NumThreads; ++i)
            {
                ref var allocator = ref Allocators.ElementAt(i);
                if (allocator.UsedSinceRewind)
                {
                    Profiler.BeginSample("Rewind");
                    Allocators.ElementAt(i).Allocator.Allocator.Rewind();
                    Profiler.EndSample();
                }
                allocator.UsedSinceRewind = false;
            }
            Profiler.EndSample();

        }

        public void Dispose()
        {
            for (int i = 0; i < NumThreads; ++i)
            {
                Allocators.ElementAt(i).Allocator.Allocator.Dispose();
                Allocators.ElementAt(i).Allocator.Dispose();
            }

            Allocators.Dispose();
        }

        public RewindableAllocator* ThreadAllocator(int threadIndex)
        {
            ref var allocator = ref Allocators.ElementAt(threadIndex);
            allocator.UsedSinceRewind = true;
            return (RewindableAllocator*)UnsafeUtility.AddressOf(ref allocator.Allocator.Allocator);
        }

        public RewindableAllocator* GeneralAllocator => ThreadAllocator(Allocators.Length - 1);
    }

    internal struct DepthSortedDrawCommand
    {
        public DrawCommandSettings Settings;
        public int InstanceIndex;
        public float3 SortingWorldPosition;
    }

    internal struct DrawCommandBin
    {
        public const int MaxInstancesPerCommand = EntitiesGraphicsTuningConstants.kMaxInstancesPerDrawCommand;
        public const int kNoSortingPosition = -1;

        public int NumInstances;
        public int InstanceOffset;
        public int DrawCommandOffset;
        public int PositionOffset;

        // Use a -1 value to signal "no sorting position" here. That way,
        // when the offset is rewritten from a placeholder to a real offset,
        // the semantics are still correct, because -1 is never a valid offset.
        public bool HasSortingPosition => PositionOffset != kNoSortingPosition;

        public int NumDrawCommands => HasSortingPosition ? NumDrawCommandsHasPositions : NumDrawCommandsNoPositions;
        public int NumDrawCommandsHasPositions => NumInstances;
        // Round up to always have enough commands
        public int NumDrawCommandsNoPositions =>
            (MaxInstancesPerCommand - 1 + NumInstances) /
            MaxInstancesPerCommand;
    }

    internal unsafe struct DrawCommandWorkItem
    {
        public DrawStream<DrawCommandVisibility>.Header* Arrays;
        public DrawStream<IntPtr>.Header* TransformArrays;
        public int BinIndex;
        public int PrefixSumNumInstances;
    }

    internal unsafe struct DrawCommandVisibility
    {
        public int ChunkStartIndex;
        public fixed ulong VisibleInstances[2];

        public DrawCommandVisibility(int startIndex)
        {
            ChunkStartIndex = startIndex;
            VisibleInstances[0] = 0;
            VisibleInstances[1] = 0;
        }

        public int VisibleInstanceCount => math.countbits(VisibleInstances[0]) + math.countbits(VisibleInstances[1]);

        public override string ToString()
        {
            return $"Visibility({ChunkStartIndex}, {VisibleInstances[1]:x16}, {VisibleInstances[0]:x16})";
        }
    }

    internal struct ChunkDrawCommand : IComparable<ChunkDrawCommand>
    {
        public DrawCommandSettings Settings;
        public DrawCommandVisibility Visibility;

        public int CompareTo(ChunkDrawCommand other) => Settings.CompareTo(other.Settings);
    }

    [BurstCompile]
    [NoAlias]
    internal unsafe struct DrawStream<T> where T : unmanaged
    {
        public const int kArraySizeElements = 16;
        public static int ElementsPerHeader => (sizeof(Header) + sizeof(T) - 1) / sizeof(T);
        public const int ElementsPerArray = kArraySizeElements;

        public Header* Head;
        private T* m_Begin;
        private int m_Count;
        private int m_TotalInstances;

        public DrawStream(RewindableAllocator* allocator)
        {
            Head = null;
            m_Begin = null;
            m_Count = 0;
            m_TotalInstances = 0;

            Init(allocator);
        }

        public void Init(RewindableAllocator* allocator)
        {
            AllocateNewBuffer(allocator);
        }

        public bool IsCreated => Head != null;

        // No need to dispose anything with RewindableAllocator
        // public void Dispose()
        // {
        //     Header* h = Head;
        //
        //     while (h != null)
        //     {
        //         Header* next = h->Next;
        //         DisposeArray(h, kAllocator);
        //         h = next;
        //     }
        // }

        private void AllocateNewBuffer(RewindableAllocator* allocator)
        {
            LinkHead(AllocateArray(allocator));
            m_Begin = Head->Element(0);
            m_Count = 0;
            Assert.IsTrue(Head->NumElements == 0);
            Assert.IsTrue(Head->NumInstances == 0);
        }

        public void LinkHead(Header* newHead)
        {
            newHead->Next = Head;
            Head = newHead;
        }

        [BurstCompile]
        [NoAlias]
        internal unsafe struct Header
        {
            // Next array in the chain of arrays
            public Header* Next;
            // Number of structs in this array
            public int NumElements;
            // Number of instances in this array
            public int NumInstances;

            public T* Element(int i)
            {
                fixed (Header* self = &this)
                    return (T*)self + i + ElementsPerHeader;
            }
        }

        public int TotalInstanceCount => m_TotalInstances;

        public static Header* AllocateArray(RewindableAllocator* allocator)
        {
            int alignment = math.max(
                UnsafeUtility.AlignOf<Header>(),
                UnsafeUtility.AlignOf<T>());

            // Make sure we always have space for ElementsPerArray elements,
            // so several streams can be kept in lockstep
            int allocCount = ElementsPerHeader + ElementsPerArray;

            Header* buffer = (Header*) allocator->Allocate(sizeof(T), alignment, allocCount);

            // Zero clear the header area (first struct)
            UnsafeUtility.MemSet(buffer, 0, sizeof(Header));

            // Buffer allocation pointer, to be used for Free()
            return buffer;
        }

        // Assume that the given header is part of an array allocated with AllocateArray,
        // and release the array.
        // public static void DisposeArray(Header* header, Allocator allocator)
        // {
        //     UnsafeUtility.Free(header, allocator);
        // }

        [return: NoAlias]
        public T* AppendElement(RewindableAllocator* allocator)
        {
            if (m_Count >= ElementsPerArray)
                AllocateNewBuffer(allocator);

            T* elem = m_Begin + m_Count;
            ++m_Count;
            Head->NumElements += 1;
            return elem;
        }

        public void AddInstances(int numInstances)
        {
            Head->NumInstances += numInstances;
            m_TotalInstances += numInstances;
        }
    }

    [BurstCompile]
    [NoAlias]
    internal unsafe struct DrawCommandStream
    {
        private DrawStream<DrawCommandVisibility> m_Stream;
        private DrawStream<IntPtr> m_ChunkTransformsStream;
        private int m_PrevChunkStartIndex;
        [NoAlias]
        private DrawCommandVisibility* m_PrevVisibility;

        public DrawCommandStream(RewindableAllocator* allocator)
        {
            m_Stream = new DrawStream<DrawCommandVisibility>(allocator);
            m_ChunkTransformsStream = default; // Don't allocate here, only on demand
            m_PrevChunkStartIndex = -1;
            m_PrevVisibility = null;
        }

        public void Dispose()
        {
            // m_Stream.Dispose();
        }

        public void Emit(RewindableAllocator* allocator, int qwordIndex, int bitIndex, int chunkStartIndex)
        {
            DrawCommandVisibility* visibility;

            if (chunkStartIndex == m_PrevChunkStartIndex)
            {
                visibility = m_PrevVisibility;
            }
            else
            {
                visibility = m_Stream.AppendElement(allocator);
                *visibility = new DrawCommandVisibility(chunkStartIndex);
            }

            visibility->VisibleInstances[qwordIndex] |= 1ul << bitIndex;

            m_PrevChunkStartIndex = chunkStartIndex;
            m_PrevVisibility = visibility;
            m_Stream.AddInstances(1);
        }

        public void EmitDepthSorted(RewindableAllocator* allocator,
            int qwordIndex, int bitIndex, int chunkStartIndex,
            float4x4* chunkTransforms)
        {
            DrawCommandVisibility* visibility;

            if (chunkStartIndex == m_PrevChunkStartIndex)
            {
                visibility = m_PrevVisibility;

                // Transforms have already been written when the element was added
            }
            else
            {
                visibility = m_Stream.AppendElement(allocator);
                *visibility = new DrawCommandVisibility(chunkStartIndex);

                // Store a pointer to the chunk transform array, which
                // instance expansion can use to get the positions.

                if (!m_ChunkTransformsStream.IsCreated)
                    m_ChunkTransformsStream.Init(allocator);

                var transforms = m_ChunkTransformsStream.AppendElement(allocator);
                *transforms = (IntPtr) chunkTransforms;
            }

            visibility->VisibleInstances[qwordIndex] |= 1ul << bitIndex;

            m_PrevChunkStartIndex = chunkStartIndex;
            m_PrevVisibility = visibility;
            m_Stream.AddInstances(1);
        }

        public DrawStream<DrawCommandVisibility> Stream => m_Stream;
        public DrawStream<IntPtr> TransformsStream => m_ChunkTransformsStream;
    }

    [BurstCompile]
    internal unsafe struct ThreadLocalDrawCommands
    {
        public const Allocator kAllocator = Allocator.TempJob;

        // Store the actual streams in a separate array so we can mutate them in place,
        // the hash map only supports a get/set API.
        public UnsafeParallelHashMap<DrawCommandSettings, int> DrawCommandStreamIndices;
        public UnsafeList<DrawCommandStream> DrawCommands;
        public ThreadLocalAllocator ThreadLocalAllocator;

        private fixed int m_CacheLinePadding[8]; // The padding here assumes some internal sizes

        public ThreadLocalDrawCommands(int capacity, ThreadLocalAllocator tlAllocator)
        {
            // Make sure we don't get false sharing by placing the thread locals on different cache lines.
            Assert.IsTrue(sizeof(ThreadLocalDrawCommands) >= JobsUtility.CacheLineSize);
            DrawCommandStreamIndices = new UnsafeParallelHashMap<DrawCommandSettings, int>(capacity, kAllocator);
            DrawCommands = new UnsafeList<DrawCommandStream>(capacity, kAllocator);
            ThreadLocalAllocator = tlAllocator;
        }

        public bool IsCreated => DrawCommandStreamIndices.IsCreated;

        public void Dispose()
        {
            if (!IsCreated)
                return;

            for (int i = 0; i < DrawCommands.Length; ++i)
                DrawCommands[i].Dispose();

            if (DrawCommandStreamIndices.IsCreated)
                DrawCommandStreamIndices.Dispose();
            if (DrawCommands.IsCreated)
                DrawCommands.Dispose();
        }

        public bool Emit(DrawCommandSettings settings, int qwordIndex, int bitIndex, int chunkStartIndex, int threadIndex)
        {
            var allocator = ThreadLocalAllocator.ThreadAllocator(threadIndex);

            if (DrawCommandStreamIndices.TryGetValue(settings, out int streamIndex))
            {
                DrawCommandStream* stream = DrawCommands.Ptr + streamIndex;
                stream->Emit(allocator, qwordIndex, bitIndex, chunkStartIndex);
                return false;
            }
            else
            {

                streamIndex = DrawCommands.Length;
                DrawCommands.Add(new DrawCommandStream(allocator));
                DrawCommandStreamIndices.Add(settings, streamIndex);

                DrawCommandStream* stream = DrawCommands.Ptr + streamIndex;
                stream->Emit(allocator, qwordIndex, bitIndex, chunkStartIndex);

                return true;
            }
        }

        public bool EmitDepthSorted(
            DrawCommandSettings settings, int qwordIndex, int bitIndex, int chunkStartIndex,
            float4x4* chunkTransforms,
            int threadIndex)
        {
            var allocator = ThreadLocalAllocator.ThreadAllocator(threadIndex);

            if (DrawCommandStreamIndices.TryGetValue(settings, out int streamIndex))
            {
                DrawCommandStream* stream = DrawCommands.Ptr + streamIndex;
                stream->EmitDepthSorted(allocator, qwordIndex, bitIndex, chunkStartIndex, chunkTransforms);
                return false;
            }
            else
            {

                streamIndex = DrawCommands.Length;
                DrawCommands.Add(new DrawCommandStream(allocator));
                DrawCommandStreamIndices.Add(settings, streamIndex);

                DrawCommandStream* stream = DrawCommands.Ptr + streamIndex;
                stream->EmitDepthSorted(allocator, qwordIndex, bitIndex, chunkStartIndex, chunkTransforms);

                return true;
            }
        }
    }

    [BurstCompile]
    internal unsafe struct ThreadLocalCollectBuffer
    {
        public const Allocator kAllocator = Allocator.TempJob;
        public static readonly int kCollectBufferSize = ChunkDrawCommandOutput.NumThreads;

        public UnsafeList<DrawCommandWorkItem> WorkItems;
        private fixed int m_CacheLinePadding[12]; // The padding here assumes some internal sizes

        public void EnsureCapacity(UnsafeList<DrawCommandWorkItem>.ParallelWriter dst, int count)
        {
            Assert.IsTrue(sizeof(ThreadLocalCollectBuffer) >= JobsUtility.CacheLineSize);
            Assert.IsTrue(count <= kCollectBufferSize);

            if (!WorkItems.IsCreated)
                WorkItems = new UnsafeList<DrawCommandWorkItem>(
                    kCollectBufferSize,
                    kAllocator,
                    NativeArrayOptions.UninitializedMemory);

            if (WorkItems.Length + count > WorkItems.Capacity)
                Flush(dst);
        }

        public void Flush(UnsafeList<DrawCommandWorkItem>.ParallelWriter dst)
        {
            dst.AddRangeNoResize(WorkItems.Ptr, WorkItems.Length);
            WorkItems.Clear();
        }

        public void Add(DrawCommandWorkItem workItem) => WorkItems.Add(workItem);

        public void Dispose()
        {
            if (WorkItems.IsCreated)
                WorkItems.Dispose();
        }
    }

    [BurstCompile]
    internal unsafe struct DrawBinCollector
    {
        public const Allocator kAllocator = Allocator.TempJob;
        public static readonly int NumThreads = ChunkDrawCommandOutput.NumThreads;

        public IndirectList<DrawCommandSettings> Bins;
        private UnsafeParallelHashSet<DrawCommandSettings> m_BinSet;
        private UnsafeList<ThreadLocalDrawCommands> m_ThreadLocalDrawCommands;

        public DrawBinCollector(UnsafeList<ThreadLocalDrawCommands> tlDrawCommands, RewindableAllocator* allocator)
        {
            Bins = new IndirectList<DrawCommandSettings>(0, allocator);
            m_BinSet = new UnsafeParallelHashSet<DrawCommandSettings>(0, kAllocator);
            m_ThreadLocalDrawCommands = tlDrawCommands;
        }

        public bool Add(DrawCommandSettings settings)
        {
            return true;
        }

        [BurstCompile]
        internal struct AllocateBinsJob : IJob
        {
            public IndirectList<DrawCommandSettings> Bins;
            public UnsafeParallelHashSet<DrawCommandSettings> BinSet;
            public UnsafeList<ThreadLocalDrawCommands> ThreadLocalDrawCommands;

            public void Execute()
            {
                int numBinsUpperBound = 0;

                for (int i = 0; i < NumThreads; ++i)
                    numBinsUpperBound += ThreadLocalDrawCommands.ElementAt(i).DrawCommands.Length;

                Bins.SetCapacity(numBinsUpperBound);
                BinSet.Capacity = numBinsUpperBound;
            }
        }

        [BurstCompile]
        internal struct CollectBinsJob : IJobParallelFor
        {
            public const int ThreadLocalArraySize = 256;

            public IndirectList<DrawCommandSettings> Bins;
            public UnsafeParallelHashSet<DrawCommandSettings>.ParallelWriter BinSet;
            public UnsafeList<ThreadLocalDrawCommands> ThreadLocalDrawCommands;

            private UnsafeList<DrawCommandSettings>.ParallelWriter m_BinsParallel;

            public void Execute(int index)
            {
                ref var drawCommands = ref ThreadLocalDrawCommands.ElementAt(index);
                if (!drawCommands.IsCreated)
                    return;

                m_BinsParallel = Bins.List->AsParallelWriter();

                var uniqueSettings = new NativeArray<DrawCommandSettings>(
                    ThreadLocalArraySize,
                    Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
                int numSettings = 0;

                var keys = drawCommands.DrawCommandStreamIndices.GetEnumerator();
                while (keys.MoveNext())
                {
                    var settings = keys.Current.Key;
                    if (BinSet.Add(settings))
                        AddBin(uniqueSettings, ref numSettings, settings);
                }
                keys.Dispose();

                Flush(uniqueSettings, numSettings);
            }

            private void AddBin(
                NativeArray<DrawCommandSettings> uniqueSettings,
                ref int numSettings,
                DrawCommandSettings settings)
            {
                if (numSettings >= ThreadLocalArraySize)
                {
                    Flush(uniqueSettings, numSettings);
                    numSettings = 0;
                }

                uniqueSettings[numSettings] = settings;
                ++numSettings;
            }

            private void Flush(
                NativeArray<DrawCommandSettings> uniqueSettings,
                int numSettings)
            {
                if (numSettings <= 0)
                    return;

                m_BinsParallel.AddRangeNoResize(
                    uniqueSettings.GetUnsafeReadOnlyPtr(),
                    numSettings);
            }
        }

        public JobHandle ScheduleFinalize(JobHandle dependency)
        {
            var allocateDependency = new AllocateBinsJob
            {
                Bins = Bins,
                BinSet = m_BinSet,
                ThreadLocalDrawCommands = m_ThreadLocalDrawCommands,
            }.Schedule(dependency);

            return new CollectBinsJob
            {
                Bins = Bins,
                BinSet = m_BinSet.AsParallelWriter(),
                ThreadLocalDrawCommands = m_ThreadLocalDrawCommands,
            }.Schedule(NumThreads, 1, allocateDependency);
        }

        public JobHandle Dispose(JobHandle dependency)
        {
            return JobHandle.CombineDependencies(
                Bins.Dispose(dependency),
                m_BinSet.Dispose(dependency));
        }
    }

    internal unsafe struct IndirectList<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeList<T>* List;

        public IndirectList(int capacity, RewindableAllocator* allocator)
        {
            List = AllocIndirectList(capacity, allocator);
        }

        public int Length => List->Length;
        public void Resize(int length, NativeArrayOptions options) => List->Resize(length, options);
        public void SetCapacity(int capacity) => List->SetCapacity(capacity);
        public ref T ElementAt(int i) => ref List->ElementAt(i);
        public void Add(T value) => List->Add(value);

        private static UnsafeList<T>* AllocIndirectList(int capacity, RewindableAllocator* allocator)
        {
            AllocatorManager.AllocatorHandle allocatorHandle = allocator->Handle;
            var indirectList = allocatorHandle.Allocate(default(UnsafeList<T>), 1);
            *indirectList = new UnsafeList<T>(capacity, allocatorHandle);
            return indirectList;
        }

        // No need to dispose anything with RewindableAllocator

        public JobHandle Dispose(JobHandle dependency)
        {
            return default;
        }

        public void Dispose()
        {
        }
    }

    internal static class IndirectListExtensions
    {
        public static unsafe JobHandle ScheduleWithIndirectList<T, U>(
            this T jobData,
            IndirectList<U> list,
            int innerLoopBatchCount = 1,
            JobHandle dependencies = default)
            where T : struct, IJobParallelForDefer
            where U : unmanaged
        {
            return jobData.Schedule(&list.List->m_length, innerLoopBatchCount, dependencies);
        }
    }

    internal struct SortedBin
    {
        public int UnsortedIndex;
    }

    [BurstCompile]
    [NoAlias]
    internal unsafe struct ChunkDrawCommandOutput
    {
        public const Allocator kAllocator = Allocator.TempJob;

#if UNITY_2022_2_14F1_OR_NEWER
        public static readonly int NumThreads = JobsUtility.ThreadIndexCount;
#else
        public static readonly int NumThreads = JobsUtility.MaxJobThreadCount;
#endif

        public static readonly int kNumThreadsBitfieldLength = (NumThreads + 63) / 64;
        public const int kNumReleaseThreads = 4;
        public const int kBinPresentFilterSize = 1 << 10;

        public UnsafeList<ThreadLocalDrawCommands> ThreadLocalDrawCommands;
        public UnsafeList<ThreadLocalCollectBuffer> ThreadLocalCollectBuffers;

        public UnsafeList<long> BinPresentFilter;

        public DrawBinCollector BinCollector;
        public IndirectList<DrawCommandSettings> UnsortedBins => BinCollector.Bins;

        [NativeDisableUnsafePtrRestriction]
        public IndirectList<int> SortedBins;

        [NativeDisableUnsafePtrRestriction]
        public IndirectList<DrawCommandBin> BinIndices;

        [NativeDisableUnsafePtrRestriction]
        public IndirectList<DrawCommandWorkItem> WorkItems;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<BatchCullingOutputDrawCommands> CullingOutput;

        public int BinCapacity;

        public ThreadLocalAllocator ThreadLocalAllocator;

        public ProfilerMarker ProfilerEmit;

#pragma warning disable 649
        [NativeSetThreadIndex] public int ThreadIndex;
#pragma warning restore 649

        public ChunkDrawCommandOutput(
            int initialBinCapacity,
            ThreadLocalAllocator tlAllocator,
            BatchCullingOutput cullingOutput)
        {
            BinCapacity = initialBinCapacity;
            CullingOutput = cullingOutput.drawCommands;

            ThreadLocalAllocator = tlAllocator;
            var generalAllocator = ThreadLocalAllocator.GeneralAllocator;

            ThreadLocalDrawCommands = new UnsafeList<ThreadLocalDrawCommands>(
                NumThreads,
                generalAllocator->Handle,
                NativeArrayOptions.ClearMemory);
            ThreadLocalDrawCommands.Resize(ThreadLocalDrawCommands.Capacity);
            ThreadLocalCollectBuffers = new UnsafeList<ThreadLocalCollectBuffer>(
                NumThreads,
                generalAllocator->Handle,
                NativeArrayOptions.ClearMemory);
            ThreadLocalCollectBuffers.Resize(ThreadLocalCollectBuffers.Capacity);
            BinPresentFilter = new UnsafeList<long>(
                kBinPresentFilterSize * kNumThreadsBitfieldLength,
                generalAllocator->Handle,
                NativeArrayOptions.ClearMemory);
            BinPresentFilter.Resize(BinPresentFilter.Capacity);

            BinCollector = new DrawBinCollector(ThreadLocalDrawCommands, generalAllocator);
            SortedBins = new IndirectList<int>(0, generalAllocator);
            BinIndices = new IndirectList<DrawCommandBin>(0, generalAllocator);
            WorkItems = new IndirectList<DrawCommandWorkItem>(0, generalAllocator);


            // Initialized by job system
            ThreadIndex = 0;

            ProfilerEmit = new ProfilerMarker("Emit");
        }

        public void InitializeForEmitThread()
        {
            // First to use the thread local initializes is, but don't double init
            if (!ThreadLocalDrawCommands[ThreadIndex].IsCreated)
                ThreadLocalDrawCommands[ThreadIndex] = new ThreadLocalDrawCommands(BinCapacity, ThreadLocalAllocator);
        }

        public BatchCullingOutputDrawCommands* CullingOutputDrawCommands =>
            (BatchCullingOutputDrawCommands*) CullingOutput.GetUnsafePtr();

        public static T* Malloc<T>(int count) where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<T>() * count,
                UnsafeUtility.AlignOf<T>(),
                kAllocator);
        }

        private ThreadLocalDrawCommands* DrawCommands
        {
            [return: NoAlias] get => ThreadLocalDrawCommands.Ptr + ThreadIndex;
        }

        public ThreadLocalCollectBuffer* CollectBuffer
        {
            [return: NoAlias] get => ThreadLocalCollectBuffers.Ptr + ThreadIndex;
        }

        public void Emit(DrawCommandSettings settings, int entityQword, int entityBit, int chunkStartIndex)
        {
            // Update the cached hash code here, so all processing after this can just use the cached value
            // without recomputing the hash each time.
            settings.ComputeHashCode();

            bool newBinAdded = DrawCommands->Emit(settings, entityQword, entityBit, chunkStartIndex, ThreadIndex);
            if (newBinAdded)
            {
                BinCollector.Add(settings);
                MarkBinPresentInThread(settings, ThreadIndex);
            }
        }

        public void EmitDepthSorted(
            DrawCommandSettings settings, int entityQword, int entityBit, int chunkStartIndex,
            float4x4* chunkTransforms)
        {
            // Update the cached hash code here, so all processing after this can just use the cached value
            // without recomputing the hash each time.
            settings.ComputeHashCode();

            bool newBinAdded = DrawCommands->EmitDepthSorted(settings, entityQword, entityBit, chunkStartIndex, chunkTransforms, ThreadIndex);
            if (newBinAdded)
            {
                BinCollector.Add(settings);
                MarkBinPresentInThread(settings, ThreadIndex);
            }
        }

        [return: NoAlias]
        public long* BinPresentFilterForSettings(DrawCommandSettings settings)
        {
            uint hash = (uint) settings.GetHashCode();
            uint index = hash % (uint)kBinPresentFilterSize;
            return BinPresentFilter.Ptr + index * kNumThreadsBitfieldLength;
        }

        private void MarkBinPresentInThread(DrawCommandSettings settings, int threadIndex)
        {
            long* settingsFilter = BinPresentFilterForSettings(settings);

            uint threadQword = (uint) threadIndex / 64;
            uint threadBit = (uint) threadIndex % 64;

            AtomicHelpers.AtomicOr(
                settingsFilter,
                (int)threadQword,
                1L << (int) threadBit);
        }

        public static int FastHash<T>(T value) where T : struct
        {
            // TODO: Replace with hardware CRC32?
            return (int)xxHash3.Hash64(UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<T>()).x;
        }

        public JobHandle Dispose(JobHandle dependencies)
        {
            // First schedule a job to release all the thread local arrays, which requires
            // that the data structures are still in place so we can find them.
            var releaseChunkDrawCommandsDependency = new ReleaseChunkDrawCommandsJob
            {
                DrawCommandOutput = this,
                NumThreads = kNumReleaseThreads,
            }.Schedule(kNumReleaseThreads, 1, dependencies);

            // When those have been released, release the data structures.
            var disposeDone = new JobHandle();
            disposeDone = JobHandle.CombineDependencies(disposeDone,
                ThreadLocalDrawCommands.Dispose(releaseChunkDrawCommandsDependency));
            disposeDone = JobHandle.CombineDependencies(disposeDone,
                ThreadLocalCollectBuffers.Dispose(releaseChunkDrawCommandsDependency));
            disposeDone = JobHandle.CombineDependencies(disposeDone,
                BinPresentFilter.Dispose(releaseChunkDrawCommandsDependency));
            disposeDone = JobHandle.CombineDependencies(disposeDone,
                BinCollector.Dispose(releaseChunkDrawCommandsDependency));
            disposeDone = JobHandle.CombineDependencies(disposeDone,
                SortedBins.Dispose(releaseChunkDrawCommandsDependency));
            disposeDone = JobHandle.CombineDependencies(disposeDone,
                BinIndices.Dispose(releaseChunkDrawCommandsDependency));
            disposeDone = JobHandle.CombineDependencies(disposeDone,
                WorkItems.Dispose(releaseChunkDrawCommandsDependency));

            return disposeDone;
        }

        [BurstCompile]
        private struct ReleaseChunkDrawCommandsJob : IJobParallelFor
        {
            public ChunkDrawCommandOutput DrawCommandOutput;
            public int NumThreads;

            public void Execute(int index)
            {
                for (int i = index; i < ChunkDrawCommandOutput.NumThreads; i += NumThreads)
                {
                    DrawCommandOutput.ThreadLocalDrawCommands[i].Dispose();
                    DrawCommandOutput.ThreadLocalCollectBuffers[i].Dispose();
                }
            }
        }
    }

    [BurstCompile]
    internal unsafe struct EmitDrawCommandsJob : IJobParallelForDefer
    {
        [ReadOnly] public IndirectList<ChunkVisibilityItem> VisibilityItems;
        [ReadOnly] public ComponentTypeHandle<EntitiesGraphicsChunkInfo> EntitiesGraphicsChunkInfo;
        [ReadOnly] public ComponentTypeHandle<MaterialMeshInfo> MaterialMeshInfo;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorld;
        [ReadOnly] public ComponentTypeHandle<DepthSorted_Tag> DepthSorted;
        [ReadOnly] public ComponentTypeHandle<DeformedMeshIndex> DeformedMeshIndex;
        [ReadOnly] public SharedComponentTypeHandle<RenderMeshArray> RenderMeshArray;
        [ReadOnly] public SharedComponentTypeHandle<RendererRange> RendererRangeHandle;
        [ReadOnly] public SharedComponentTypeHandle<RenderFilterSettings> RenderFilterSettings;
        [ReadOnly] public SharedComponentTypeHandle<LightMaps> LightMaps;
        [ReadOnly] public NativeParallelHashMap<FilterSettingsIndex, BatchFilterSettings> FilterSettings;
        [ReadOnly] public NativeParallelHashMap<int, BRGRenderMeshArray> BRGRenderMeshArrays;

        public ChunkDrawCommandOutput DrawCommandOutput;

        public ulong SceneCullingMask;
        public float3 CameraPosition;
        public uint LastSystemVersion;
        public uint CullingLayerMask;

        public ProfilerMarker ProfilerEmitChunk;

#if UNITY_EDITOR
        [ReadOnly] public SharedComponentTypeHandle<EditorRenderData> EditorDataComponentHandle;
#endif

        public void Execute(int index)
        {
            var visibilityItem = VisibilityItems.ElementAt(index);

            var chunk = visibilityItem.Chunk;
            var chunkVisibility = visibilityItem.Visibility;

            FilterSettingsIndex filterIndex = new FilterSettingsIndex(chunk.GetSharedComponentIndex(RenderFilterSettings));
#if UNITY_EDITOR
            if (chunk.Has<EditorRenderData>()) {
                int editorDataIndex = chunk.GetSharedComponentIndex(EditorDataComponentHandle);
                filterIndex = filterIndex.WithEditorData(editorDataIndex);
            }
#endif
            BatchFilterSettings filterSettings = FilterSettings[filterIndex];

            if (((1 << filterSettings.layer) & CullingLayerMask) == 0) return;

            // If the chunk has a RenderMeshArray, get access to the corresponding registered
            // Material and Mesh IDs
            BRGRenderMeshArray brgRenderMeshArray = default;
            if (!BRGRenderMeshArrays.IsEmpty)
            {
                int renderMeshArrayIndex = chunk.GetSharedComponentIndex(RenderMeshArray);
                bool hasRenderMeshArray = renderMeshArrayIndex >= 0;
                if (hasRenderMeshArray)
                    BRGRenderMeshArrays.TryGetValue(renderMeshArrayIndex, out brgRenderMeshArray);
            }

            DrawCommandOutput.InitializeForEmitThread();

            {
                //using var prof = ProfilerEmitChunk.Auto();

                var entitiesGraphicsChunkInfo = chunk.GetChunkComponentData(ref EntitiesGraphicsChunkInfo);

                if (!entitiesGraphicsChunkInfo.Valid)
                    return;

                ref var chunkCullingData = ref entitiesGraphicsChunkInfo.CullingData;

                // If nothing is visible in the chunk, avoid all unnecessary work
                bool noVisibleEntities = !chunkVisibility->AnyVisible;
                if (noVisibleEntities)
                    return;

                int batchIndex = entitiesGraphicsChunkInfo.BatchIndex;

#if UNITY_EDITOR
                if (!TestSceneCullingMask(chunk))
                    return;
#endif

                var materialMeshInfos = chunk.GetNativeArray(ref MaterialMeshInfo);
                var localToWorlds = chunk.GetNativeArray(ref LocalToWorld);
                bool isDepthSorted = chunk.Has(ref DepthSorted);
                bool isLightMapped = chunk.GetSharedComponentIndex(LightMaps) >= 0;

                // Check if the chunk has statically disabled motion (i.e. never in motion pass)
                // or enabled motion (i.e. in motion pass if there was actual motion or force-to-zero).
                // We make sure to never set the motion flag if motion is statically disabled to improve batching
                // in cases where the transform is changed.
                bool hasMotion = (chunkCullingData.Flags & EntitiesGraphicsChunkCullingData.kFlagPerObjectMotion) != 0;

                if (hasMotion)
                {
                    bool orderChanged = chunk.DidOrderChange(LastSystemVersion);
                    bool transformChanged = chunk.DidChange(ref LocalToWorld, LastSystemVersion);
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
                    bool isDeformed = chunk.Has(ref DeformedMeshIndex);
#else
                    bool isDeformed = false;
#endif
                    hasMotion = orderChanged || transformChanged || isDeformed;
                }

                bool hasRendererRange = chunk.Has(RendererRangeHandle);
                RendererRange rendererRange = default;
                if (hasRendererRange)
                {
                    rendererRange = chunk.GetSharedComponent(RendererRangeHandle);
                }

                int chunkStartIndex = entitiesGraphicsChunkInfo.CullingData.ChunkOffsetInBatch;

                BatchDrawCommandFlags baseDrawCommandFlags = 0;
                if (hasMotion)
                    baseDrawCommandFlags |= BatchDrawCommandFlags.HasMotion;
                if (isLightMapped)
                    baseDrawCommandFlags |= BatchDrawCommandFlags.IsLightMapped;
                if (isDepthSorted)
                    baseDrawCommandFlags |= BatchDrawCommandFlags.HasSortingPosition;

                for (int j = 0; j < 2; j++)
                {
                    ulong visibleWord = chunkVisibility->VisibleEntities[j];

                    while (visibleWord != 0)
                    {
                        int bitIndex = math.tzcnt(visibleWord);
                        int entityIndex = (j << 6) + bitIndex;
                        ulong entityMask = 1ul << bitIndex;

                        // Clear the bit first in case we early out from the loop
                        visibleWord ^= entityMask;

                        MaterialMeshInfo materialMeshInfo = materialMeshInfos[entityIndex];
                        BatchID batchID = new BatchID { value = (uint)batchIndex };
                        ushort splitMask = chunkVisibility->SplitMasks[entityIndex];
                        bool flipWinding = (chunkCullingData.FlippedWinding[j] & entityMask) != 0;

                        BatchDrawCommandFlags drawCommandFlags = baseDrawCommandFlags;

                        if (flipWinding)
                            drawCommandFlags |= BatchDrawCommandFlags.FlipWinding;

                        if (materialMeshInfo.HasMaterialMeshIndexRange)
                        {
                            RangeInt matMeshIndexRange = materialMeshInfo.MaterialMeshIndexRange;

                            if (Hint.Likely(hasRendererRange))
                            {
                                var startIndex = matMeshIndexRange.start;
                                var startMesh = rendererRange.MaterialMeshInfos[startIndex].MeshID;

                                if (Hint.Likely(startMesh != BatchMeshID.Null)) {
                                    for (int i = startIndex; i < matMeshIndexRange.end; i++)
                                    {
                                        var renderInfo = rendererRange.MaterialMeshInfos[i];

                                        DrawCommandSettings settings = new DrawCommandSettings
                                        {
                                            FilterIndex = filterIndex,
                                            BatchID = batchID,
                                            MaterialID = renderInfo.MaterialID,
                                            MeshID = renderInfo.MeshID,
                                            SplitMask = splitMask,
                                            SubMeshIndex = (ushort)renderInfo.SubMesh,
                                            Flags = drawCommandFlags
                                        };

                                        EmitDrawCommand(settings, j, bitIndex, chunkStartIndex, localToWorlds);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = matMeshIndexRange.start; i < matMeshIndexRange.end; i++)
                                {
                                    // Drop the draw command if OOB. Errors should have been reported already so no need to log anything
                                    if (i >= brgRenderMeshArray.MaterialMeshSubMeshes.Length)
                                        continue;

                                    BatchMaterialMeshSubMesh matMeshSubMesh = brgRenderMeshArray.MaterialMeshSubMeshes[i];

                                    DrawCommandSettings settings = new DrawCommandSettings
                                    {
                                        FilterIndex = filterIndex,
                                        BatchID = batchID,
                                        MaterialID = matMeshSubMesh.Material,
                                        MeshID = matMeshSubMesh.Mesh,
                                        SplitMask = splitMask,
                                        SubMeshIndex = (ushort)matMeshSubMesh.SubMeshIndex,
                                        Flags = drawCommandFlags
                                    };

                                    EmitDrawCommand(settings, j, bitIndex, chunkStartIndex, localToWorlds);
                                }
                            }
                        }
                        else
                        {
                            BatchMeshID meshID = materialMeshInfo.IsRuntimeMesh
                                ? materialMeshInfo.MeshID
                                : brgRenderMeshArray.GetMeshID(materialMeshInfo);

                            // Invalid meshes at this point will be skipped.
                            if (Hint.Unlikely(meshID == BatchMeshID.Null))
                                continue;

                            // Null materials are handled internally by Unity using the error material if available.
                            BatchMaterialID materialID;
                            if (Hint.Likely(materialMeshInfo.IsRuntimeMaterial)) {
                                materialID = materialMeshInfo.MaterialID;
                            } else {
                                materialID = brgRenderMeshArray.GetMaterialID(materialMeshInfo);
                            }

                            var settings = new DrawCommandSettings
                            {
                                FilterIndex = filterIndex,
                                BatchID = batchID,
                                MaterialID = materialID,
                                MeshID = meshID,
                                SplitMask = splitMask,
                                SubMeshIndex = (ushort)materialMeshInfo.SubMesh,
                                Flags = drawCommandFlags
                            };

                            EmitDrawCommand(settings, j, bitIndex, chunkStartIndex, localToWorlds);
                        }
                    }
                }
            }
        }

        private void EmitDrawCommand(in DrawCommandSettings settings, int entityQword, int entityBit, int chunkStartIndex, NativeArray<LocalToWorld> localToWorlds)
        {
            // Depth sorted draws are emitted with access to entity transforms,
            // so they can also be written out for sorting
            if (settings.HasSortingPosition)
            {
                DrawCommandOutput.EmitDepthSorted(settings, entityQword, entityBit, chunkStartIndex,
                    (float4x4*)localToWorlds.GetUnsafeReadOnlyPtr());
            }
            else
            {
                DrawCommandOutput.Emit(settings, entityQword, entityBit, chunkStartIndex);
            }
        }

        private bool TestSceneCullingMask(ArchetypeChunk chunk)
        {
#if UNITY_EDITOR
            // If we can't find a culling mask, use the default
            ulong chunkSceneCullingMask = EditorSceneManager.DefaultSceneCullingMask;

            if (chunk.Has(EditorDataComponentHandle))
            {
                var editorRenderData = chunk.GetSharedComponent(EditorDataComponentHandle);
                chunkSceneCullingMask = editorRenderData.SceneCullingMask;
            }

            // Cull the chunk if the scene mask intersection is empty.
            return (SceneCullingMask & chunkSceneCullingMask) != 0;
#else
            return true;
#endif
        }
    }

    [BurstCompile]
    internal unsafe struct AllocateWorkItemsJob : IJob
    {
        public ChunkDrawCommandOutput DrawCommandOutput;

        public void Execute()
        {
            int numBins = DrawCommandOutput.UnsortedBins.Length;

            DrawCommandOutput.BinIndices.Resize(numBins, NativeArrayOptions.UninitializedMemory);

            // Each thread can have one item per bin, but likely not all threads will.
            int workItemsUpperBound = ChunkDrawCommandOutput.NumThreads * numBins;
            DrawCommandOutput.WorkItems.SetCapacity(workItemsUpperBound);
        }
    }

    [BurstCompile]
    internal unsafe struct DrawBinSort
    {
        public const int kNumSlices = 4;
        public const Allocator kAllocator = Allocator.TempJob;

        [BurstCompile]
        internal unsafe struct SortArrays
        {
            public IndirectList<int> SortedBins;
            public IndirectList<int> SortTemp;

            public int ValuesPerIndex => (SortedBins.Length + kNumSlices - 1) / kNumSlices;

            [return: NoAlias] public int* ValuesTemp(int i = 0) => SortTemp.List->Ptr + i;
            [return: NoAlias] public int* ValuesDst(int i = 0) => SortedBins.List->Ptr + i;

            public void GetBeginEnd(int index, out int begin, out int end)
            {
                begin = index * ValuesPerIndex;
                end = math.min(begin + ValuesPerIndex, SortedBins.Length);
            }
        }

        internal unsafe struct BinSortComparer : IComparer<int>
        {
            [NoAlias]
            public DrawCommandSettings* Bins;

            public BinSortComparer(IndirectList<DrawCommandSettings> bins)
            {
                Bins = bins.List->Ptr;
            }

            public int Compare(int x, int y) => Key(x).CompareTo(Key(y));

            private DrawCommandSettings Key(int bin) => Bins[bin];
        }

        [BurstCompile]
        internal unsafe struct AllocateForSortJob : IJob
        {
            public IndirectList<DrawCommandSettings> UnsortedBins;
            public SortArrays Arrays;

            public void Execute()
            {
                int numBins = UnsortedBins.Length;
                Arrays.SortedBins.Resize(numBins, NativeArrayOptions.UninitializedMemory);
                Arrays.SortTemp.Resize(numBins, NativeArrayOptions.UninitializedMemory);
            }
        }

        [BurstCompile]
        internal unsafe struct SortSlicesJob : IJobParallelFor
        {
            public SortArrays Arrays;
            public IndirectList<DrawCommandSettings> UnsortedBins;

            public void Execute(int index)
            {
                Arrays.GetBeginEnd(index, out int begin, out int end);

                var valuesFromZero = Arrays.ValuesTemp();
                int N = end - begin;

                for (int i = begin; i < end; ++i)
                    valuesFromZero[i] = i;

                NativeSortExtension.Sort(Arrays.ValuesTemp(begin), N, new BinSortComparer(UnsortedBins));
            }
        }

        [BurstCompile]
        internal unsafe struct MergeSlicesJob : IJob
        {
            public SortArrays Arrays;
            public IndirectList<DrawCommandSettings> UnsortedBins;
            public int NumSlices => kNumSlices;

            public void Execute()
            {
                var sliceRead = stackalloc int[NumSlices];
                var sliceEnd = stackalloc int[NumSlices];

                int sliceMask = 0;

                for (int i = 0; i < NumSlices; ++i)
                {
                    Arrays.GetBeginEnd(i, out sliceRead[i], out sliceEnd[i]);
                    if (sliceRead[i] < sliceEnd[i])
                        sliceMask |= 1 << i;
                }

                int N = Arrays.SortedBins.Length;
                var dst = Arrays.ValuesDst();
                var src = Arrays.ValuesTemp();
                var comparer = new BinSortComparer(UnsortedBins);

                for (int i = 0; i < N; ++i)
                {
                    int iterMask = sliceMask;
                    int firstNonEmptySlice = math.tzcnt(iterMask);

                    int bestSlice = firstNonEmptySlice;
                    int bestValue = src[sliceRead[firstNonEmptySlice]];
                    iterMask ^= 1 << firstNonEmptySlice;

                    while (iterMask != 0)
                    {
                        int slice = math.tzcnt(iterMask);
                        int value = src[sliceRead[slice]];

                        if (comparer.Compare(value, bestValue) < 0)
                        {
                            bestSlice = slice;
                            bestValue = value;
                        }

                        iterMask ^= 1 << slice;
                    }

                    dst[i] = bestValue;

                    int nextValue = sliceRead[bestSlice] + 1;
                    bool sliceExhausted = nextValue >= sliceEnd[bestSlice];
                    sliceRead[bestSlice] = nextValue;

                    int mask = 1 << bestSlice;
                    mask = sliceExhausted ? mask : 0;
                    sliceMask ^= mask;
                }

                Arrays.SortTemp.Dispose();
            }
        }

        public static JobHandle ScheduleBinSort(
            RewindableAllocator* allocator,
            IndirectList<int> sortedBins,
            IndirectList<DrawCommandSettings> unsortedBins,
            JobHandle dependency = default)
        {
            var sortArrays = new SortArrays
            {
                SortedBins = sortedBins,
                SortTemp = new IndirectList<int>(0, allocator),
            };

            var alloc = new AllocateForSortJob
            {
                Arrays = sortArrays,
                UnsortedBins = unsortedBins,
            }.Schedule(dependency);

            var sortSlices = new SortSlicesJob
            {
                Arrays = sortArrays,
                UnsortedBins = unsortedBins,
            }.Schedule(kNumSlices, 1, alloc);

            var mergeSlices = new MergeSlicesJob
            {
                Arrays = sortArrays,
                UnsortedBins = unsortedBins,
            }.Schedule(sortSlices);

            return mergeSlices;
        }
    }


    [BurstCompile]
    internal unsafe struct CollectWorkItemsJob : IJobParallelForDefer
    {
        public ChunkDrawCommandOutput DrawCommandOutput;

        public ProfilerMarker ProfileCollect;
        public ProfilerMarker ProfileWrite;

        public void Execute(int index)
        {
            var settings = DrawCommandOutput.UnsortedBins.ElementAt(index);
            bool hasSortingPosition = settings.HasSortingPosition;

            long* binPresentFilter = DrawCommandOutput.BinPresentFilterForSettings(settings);

            int maxWorkItems = 0;
            for (int qwIndex = 0; qwIndex < ChunkDrawCommandOutput.kNumThreadsBitfieldLength; ++qwIndex)
                maxWorkItems += math.countbits(binPresentFilter[qwIndex]);

            // Since we collect at most one item per thread, we will have N = thread count at most
            var workItems = DrawCommandOutput.WorkItems.List->AsParallelWriter();
            var collectBuffer = DrawCommandOutput.CollectBuffer;
            collectBuffer->EnsureCapacity(workItems, maxWorkItems);

            int numInstancesPrefixSum = 0;

            // ProfileCollect.Begin();

            for (int qwIndex = 0; qwIndex < ChunkDrawCommandOutput.kNumThreadsBitfieldLength; ++qwIndex)
            {
                // Load a filter bitfield which has a 1 bit for every thread index that might contain
                // draws for a given DrawCommandSettings. The filter is exact if there are no hash
                // collisions, but might contain false positives if hash collisions happened.
                ulong qword = (ulong) binPresentFilter[qwIndex];

                while (qword != 0)
                {
                    int bitIndex = math.tzcnt(qword);
                    ulong mask = 1ul << bitIndex;
                    qword ^= mask;

                    int i = (qwIndex << 6) + bitIndex;

                    var threadDraws = DrawCommandOutput.ThreadLocalDrawCommands[i];

                    if (!threadDraws.DrawCommandStreamIndices.IsCreated)
                        continue;

                    if (threadDraws.DrawCommandStreamIndices.TryGetValue(settings, out int streamIndex))
                    {
                        var stream = threadDraws.DrawCommands[streamIndex].Stream;

                        if (hasSortingPosition)
                        {
                            var transformStream = threadDraws.DrawCommands[streamIndex].TransformsStream;
                            collectBuffer->Add(new DrawCommandWorkItem
                            {
                                Arrays = stream.Head,
                                TransformArrays = transformStream.Head,
                                BinIndex = index,
                                PrefixSumNumInstances = numInstancesPrefixSum,
                            });
                        }
                        else
                        {
                            collectBuffer->Add(new DrawCommandWorkItem
                            {
                                Arrays = stream.Head,
                                TransformArrays = null,
                                BinIndex = index,
                                PrefixSumNumInstances = numInstancesPrefixSum,
                            });
                        }

                        numInstancesPrefixSum += stream.TotalInstanceCount;
                    }
                }
            }
            // ProfileCollect.End();
            // ProfileWrite.Begin();

            DrawCommandOutput.BinIndices.ElementAt(index) = new DrawCommandBin
            {
                NumInstances = numInstancesPrefixSum,
                InstanceOffset = 0,
                PositionOffset = hasSortingPosition ? 0 : DrawCommandBin.kNoSortingPosition,
            };

            // ProfileWrite.End();
        }
    }

    [BurstCompile]
    internal unsafe struct FlushWorkItemsJob : IJobParallelFor
    {
        public ChunkDrawCommandOutput DrawCommandOutput;

        public void Execute(int index)
        {
            var dst = DrawCommandOutput.WorkItems.List->AsParallelWriter();
            DrawCommandOutput.ThreadLocalCollectBuffers[index].Flush(dst);
        }
    }

    [BurstCompile]
    internal unsafe struct AllocateInstancesJob : IJob
    {
        public ChunkDrawCommandOutput DrawCommandOutput;

        public void Execute()
        {
            int numBins = DrawCommandOutput.BinIndices.Length;

            int instancePrefixSum = 0;
            int sortingPositionPrefixSum = 0;

            for (int i = 0; i < numBins; ++i)
            {
                ref var bin = ref DrawCommandOutput.BinIndices.ElementAt(i);
                bool hasSortingPosition = bin.HasSortingPosition;

                bin.InstanceOffset = instancePrefixSum;

                // Keep kNoSortingPosition in the PositionOffset if no sorting
                // positions, so draw command jobs can reliably check it to
                // to know whether there are positions without needing access to flags
                bin.PositionOffset = hasSortingPosition
                    ? sortingPositionPrefixSum
                    : DrawCommandBin.kNoSortingPosition;

                int numInstances = bin.NumInstances;
                int numPositions = hasSortingPosition ? numInstances : 0;

                instancePrefixSum += numInstances;
                sortingPositionPrefixSum += numPositions;
            }

            var output = DrawCommandOutput.CullingOutputDrawCommands;
            output->visibleInstanceCount = instancePrefixSum;
            output->visibleInstances = ChunkDrawCommandOutput.Malloc<int>(instancePrefixSum);

            int numSortingPositionFloats = sortingPositionPrefixSum * 3;
            output->instanceSortingPositionFloatCount = numSortingPositionFloats;
            output->instanceSortingPositions = (sortingPositionPrefixSum == 0)
                ? null
                : ChunkDrawCommandOutput.Malloc<float>(numSortingPositionFloats);
        }
    }

    [BurstCompile]
    internal unsafe struct AllocateDrawCommandsJob : IJob
    {
        public ChunkDrawCommandOutput DrawCommandOutput;

        public void Execute()
        {
            int numBins = DrawCommandOutput.SortedBins.Length;

            int drawCommandPrefixSum = 0;

            for (int i = 0; i < numBins; ++i)
            {
                var sortedBin = DrawCommandOutput.SortedBins.ElementAt(i);
                ref var bin = ref DrawCommandOutput.BinIndices.ElementAt(sortedBin);
                bin.DrawCommandOffset = drawCommandPrefixSum;

                // Bins with sorting positions will be expanded to one draw command
                // per instance, whereas other bins will be expanded to contain
                // many instances per command.
                int numDrawCommands = bin.NumDrawCommands;
                drawCommandPrefixSum += numDrawCommands;
            }

            var output = DrawCommandOutput.CullingOutputDrawCommands;

            // Draw command count is exact at this point, we can set it up front
            int drawCommandCount = drawCommandPrefixSum;

            output->drawCommandCount = drawCommandCount;
            output->drawCommands = ChunkDrawCommandOutput.Malloc<BatchDrawCommand>(drawCommandCount);
            output->drawCommandPickingInstanceIDs = null;

            // Worst case is one range per draw command, so this is an upper bound estimate.
            // The real count could be less.
            output->drawRangeCount = 0;
            output->drawRanges = ChunkDrawCommandOutput.Malloc<BatchDrawRange>(drawCommandCount);
        }
    }

    [BurstCompile]
    internal unsafe struct ExpandVisibleInstancesJob : IJobParallelForDefer
    {
        public ChunkDrawCommandOutput DrawCommandOutput;

        public void Execute(int index)
        {
            var workItem = DrawCommandOutput.WorkItems.ElementAt(index);
            var header = workItem.Arrays;
            var transformHeader = workItem.TransformArrays;
            int binIndex = workItem.BinIndex;

            var bin = DrawCommandOutput.BinIndices.ElementAt(binIndex);
            int binInstanceOffset = bin.InstanceOffset;
            int binPositionOffset = bin.PositionOffset;
            int workItemInstanceOffset = workItem.PrefixSumNumInstances;
            int headerInstanceOffset = 0;

            int* visibleInstances = DrawCommandOutput.CullingOutputDrawCommands->visibleInstances;
            float3* sortingPositions = (float3*)DrawCommandOutput.CullingOutputDrawCommands->instanceSortingPositions;

            if (transformHeader == null)
            {
                while (header != null)
                {
                    ExpandArray(
                        visibleInstances,
                        header,
                        binInstanceOffset + workItemInstanceOffset + headerInstanceOffset);

                    headerInstanceOffset += header->NumInstances;
                    header = header->Next;
                }
            }
            else
            {
                while (header != null)
                {
                    Assert.IsTrue(transformHeader != null);

                    int instanceOffset = binInstanceOffset + workItemInstanceOffset + headerInstanceOffset;
                    int positionOffset = binPositionOffset + workItemInstanceOffset + headerInstanceOffset;

                    ExpandArrayWithPositions(
                        visibleInstances,
                        sortingPositions,
                        header,
                        transformHeader,
                        instanceOffset,
                        positionOffset);

                    headerInstanceOffset += header->NumInstances;
                    header = header->Next;
                    transformHeader = transformHeader->Next;
                }
            }
        }

        private int ExpandArray(
            int* visibleInstances,
            DrawStream<DrawCommandVisibility>.Header* header,
            int instanceOffset)
        {
            int numStructs = header->NumElements;

            for (int i = 0; i < numStructs; ++i)
            {
                var visibility = *header->Element(i);
                int numInstances = ExpandVisibility(visibleInstances + instanceOffset, visibility);
                Assert.IsTrue(numInstances > 0);
                instanceOffset += numInstances;
            }

            return instanceOffset;
        }

        private int ExpandArrayWithPositions(
            int* visibleInstances,
            float3* sortingPositions,
            DrawStream<DrawCommandVisibility>.Header* header,
            DrawStream<IntPtr>.Header* transformHeader,
            int instanceOffset,
            int positionOffset)
        {
            int numStructs = header->NumElements;

            for (int i = 0; i < numStructs; ++i)
            {
                var visibility = *header->Element(i);
                var transforms = (float4x4*) (*transformHeader->Element(i));
                int numInstances = ExpandVisibilityWithPositions(
                    visibleInstances + instanceOffset,
                    sortingPositions + positionOffset,
                    visibility,
                    transforms);
                Assert.IsTrue(numInstances > 0);
                instanceOffset += numInstances;
                positionOffset += numInstances;
            }

            return instanceOffset;
        }


        private int ExpandVisibility(int* outputInstances, DrawCommandVisibility visibility)
        {
            int numInstances = 0;
            int startIndex = visibility.ChunkStartIndex;

            for (int i = 0; i < 2; ++i)
            {
                ulong qword = visibility.VisibleInstances[i];
                while (qword != 0)
                {
                    int bitIndex = math.tzcnt(qword);
                    ulong mask = 1ul << bitIndex;
                    qword ^= mask;
                    int instanceIndex = (i << 6) + bitIndex;
                    int visibilityIndex = startIndex + instanceIndex;
                    outputInstances[numInstances] = visibilityIndex;
                    ++numInstances;
                }
            }

            return numInstances;
        }

        private int ExpandVisibilityWithPositions(
            int* outputInstances,
            float3* outputSortingPosition,
            DrawCommandVisibility visibility,
            float4x4* transforms)
        {
            int numInstances = 0;
            int startIndex = visibility.ChunkStartIndex;

            for (int i = 0; i < 2; ++i)
            {
                ulong qword = visibility.VisibleInstances[i];
                while (qword != 0)
                {
                    int bitIndex = math.tzcnt(qword);
                    ulong mask = 1ul << bitIndex;
                    qword ^= mask;
                    int instanceIndex = (i << 6) + bitIndex;

                    var instanceTransform = new LocalToWorld
                    {
                        Value = transforms[instanceIndex],
                    };

                    int visibilityIndex = startIndex + instanceIndex;
                    outputInstances[numInstances] = visibilityIndex;
                    outputSortingPosition[numInstances] = instanceTransform.Position;

                    ++numInstances;
                }
            }

            return numInstances;
        }
    }

    [BurstCompile]
    internal unsafe struct GenerateDrawCommandsJob : IJobParallelForDefer
    {
        public ChunkDrawCommandOutput DrawCommandOutput;

#if UNITY_EDITOR
        [NativeDisableUnsafePtrRestriction]
        public EntitiesGraphicsPerThreadStats* Stats;
        public BatchCullingViewType ViewType;

#pragma warning disable 649
        [NativeSetThreadIndex]
        public int ThreadIndex;
#pragma warning restore 649
#endif

        public void Execute(int index)
        {
#if UNITY_EDITOR
            ref var stats = ref Stats[ThreadIndex];
#endif
            var sortedBin = DrawCommandOutput.SortedBins.ElementAt(index);
            var settings = DrawCommandOutput.UnsortedBins.ElementAt(sortedBin);
            var bin = DrawCommandOutput.BinIndices.ElementAt(sortedBin);

            bool hasSortingPosition = settings.HasSortingPosition;
            uint maxPerCommand = hasSortingPosition
                ? 1u
                : EntitiesGraphicsTuningConstants.kMaxInstancesPerDrawCommand;
            uint numInstances = (uint)bin.NumInstances;
            int numDrawCommands = bin.NumDrawCommands;

            uint drawInstanceOffset = (uint)bin.InstanceOffset;
            uint drawPositionFloatOffset = (uint)bin.PositionOffset * 3; // 3 floats per position

            var cullingOutput = DrawCommandOutput.CullingOutputDrawCommands;
            var draws = cullingOutput->drawCommands;

            for (int i = 0; i < numDrawCommands; ++i)
            {
                var draw = new BatchDrawCommand
                {
                    visibleOffset = drawInstanceOffset,
                    visibleCount = math.min(maxPerCommand, numInstances),
                    batchID = settings.BatchID,
                    materialID = settings.MaterialID,
                    meshID = settings.MeshID,
                    submeshIndex = (ushort)settings.SubMeshIndex,
                    splitVisibilityMask = settings.SplitMask,
                    flags = settings.Flags,
                    sortingPosition = hasSortingPosition
                        ? (int)drawPositionFloatOffset
                        : 0,
                };

                int drawCommandIndex = bin.DrawCommandOffset + i;
                draws[drawCommandIndex] = draw;

                drawInstanceOffset += draw.visibleCount;
                drawPositionFloatOffset += draw.visibleCount * 3;
                numInstances -= draw.visibleCount;

#if UNITY_EDITOR
                if (ViewType == BatchCullingViewType.Camera)
                {
                    // Split mask is not set and not used when rendering on the main view
                    stats.RenderedEntityCount += (int)draw.visibleCount;
                }
                else if (ViewType == BatchCullingViewType.Light)
                {
                    // Multiply by the number of visible split mask.
                    // When rendering shadow maps, each draw command will be processed once per enabled split.
                    stats.RenderedEntityCount += (int)draw.visibleCount * math.countbits((int)settings.SplitMask);
                }
#endif
            }
#if UNITY_EDITOR
            stats.DrawCommandCount += numDrawCommands;
#endif
        }
    }

    [BurstCompile]
    internal unsafe struct GenerateDrawRangesJob : IJob
    {
        public ChunkDrawCommandOutput DrawCommandOutput;

        [ReadOnly] public NativeParallelHashMap<FilterSettingsIndex, BatchFilterSettings> FilterSettings;

        private const int MaxInstances = EntitiesGraphicsTuningConstants.kMaxInstancesPerDrawRange;
        private const int MaxCommands = EntitiesGraphicsTuningConstants.kMaxDrawCommandsPerDrawRange;

        private FilterSettingsIndex m_PrevFilterIndex;
        private int m_CommandsInRange;
        private int m_InstancesInRange;

#if UNITY_EDITOR
        [NativeDisableUnsafePtrRestriction]
        public EntitiesGraphicsPerThreadStats* Stats;

#pragma warning disable 649
        [NativeSetThreadIndex]
        public int ThreadIndex;
#pragma warning restore 649
#endif

        public void Execute()
        {
#if UNITY_EDITOR
            ref var stats = ref Stats[ThreadIndex];
#endif
            int numBins = DrawCommandOutput.SortedBins.Length;
            var output = DrawCommandOutput.CullingOutputDrawCommands;

            ref int rangeCount = ref output->drawRangeCount;
            var ranges = output->drawRanges;

            rangeCount = 0;
            m_PrevFilterIndex = FilterSettingsIndex.Empty;
            m_CommandsInRange = 0;
            m_InstancesInRange = 0;

            for (int i = 0; i < numBins; ++i)
            {
                var sortedBin = DrawCommandOutput.SortedBins.ElementAt(i);
                var settings = DrawCommandOutput.UnsortedBins.ElementAt(sortedBin);
                var bin = DrawCommandOutput.BinIndices.ElementAt(sortedBin);

                int numInstances = bin.NumInstances;
                int drawCommandOffset = bin.DrawCommandOffset;
                int numDrawCommands = bin.NumDrawCommands;
                FilterSettingsIndex filterIndex = settings.FilterIndex;
                bool hasSortingPosition = settings.HasSortingPosition;

                for (int j = 0; j < numDrawCommands; ++j)
                {
                    int instancesInCommand = math.min(numInstances, DrawCommandBin.MaxInstancesPerCommand);

                    AccumulateDrawRange(
                        ref rangeCount,
                        ranges,
                        drawCommandOffset,
                        instancesInCommand,
                        filterIndex,
                        hasSortingPosition);

                    ++drawCommandOffset;
                    numInstances -= instancesInCommand;
                }
            }
#if UNITY_EDITOR
            stats.DrawRangeCount += rangeCount;
#endif

            Assert.IsTrue(rangeCount <= output->drawCommandCount);
        }

        private void AccumulateDrawRange(
            ref int rangeCount,
            BatchDrawRange* ranges,
            int drawCommandOffset,
            int numInstances,
            FilterSettingsIndex filterIndex,
            bool hasSortingPosition)
        {
            bool isFirst = rangeCount == 0;

            bool addNewCommand;

            if (isFirst)
            {
                addNewCommand = true;
            }
            else
            {
                int newInstanceCount = m_InstancesInRange + numInstances;
                int newCommandCount = m_CommandsInRange + 1;

                bool sameFilter = filterIndex.Equals(m_PrevFilterIndex);
                bool tooManyInstances = newInstanceCount > MaxInstances;
                bool tooManyCommands = newCommandCount > MaxCommands;

                addNewCommand = !sameFilter || tooManyInstances || tooManyCommands;
            }

            if (addNewCommand)
            {
                ranges[rangeCount] = new BatchDrawRange
                {
                    filterSettings = FilterSettings[filterIndex],
                    drawCommandsBegin = (uint)drawCommandOffset,
                    drawCommandsCount = 1,
                };

                ranges[rangeCount].filterSettings.allDepthSorted = hasSortingPosition;

                m_PrevFilterIndex = filterIndex;
                m_CommandsInRange = 1;
                m_InstancesInRange = numInstances;

                ++rangeCount;
            }
            else
            {
                ref var range = ref ranges[rangeCount - 1];

                ++range.drawCommandsCount;
                range.filterSettings.allDepthSorted &= hasSortingPosition;

                ++m_CommandsInRange;
                m_InstancesInRange += numInstances;
            }
        }
    }


    internal unsafe struct DebugValidateSortJob : IJob
    {
        public ChunkDrawCommandOutput DrawCommandOutput;

        public void Execute()
        {
            int N = DrawCommandOutput.UnsortedBins.Length;

            for (int i = 0; i < N; ++i)
            {
                int sorted = DrawCommandOutput.SortedBins.ElementAt(i);
                var settings = DrawCommandOutput.UnsortedBins.ElementAt(sorted);

                int next = math.min(i + 1, N - 1);
                int sortedNext = DrawCommandOutput.SortedBins.ElementAt(next);
                var settingsNext = DrawCommandOutput.UnsortedBins.ElementAt(sortedNext);

                int cmp = settings.CompareTo(settingsNext);
                int cmpRef = settings.CompareToReference(settingsNext);

                Assert.IsTrue(cmpRef <= 0, $"Draw commands not in order. CompareTo: {cmp}, CompareToReference: {cmpRef}, A: {settings}, B: {settingsNext}");
                Assert.IsTrue(cmpRef == cmp, $"CompareTo() does not match CompareToReference(). CompareTo: {cmp}, CompareToReference: {cmpRef}, A: {settings}, B: {settingsNext}");
            }
        }
    }
}
