using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Graphics;
using Awaken.Utility.Graphics.Mipmaps;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UniversalProfiling;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.LeshyRenderer {
    // Needs to be a class because otherwise OnPerformCulling will be called on invalid target
    [Il2CppEagerStaticClassConstruction]
    public unsafe class LeshyRendering : IMemorySnapshotProvider, MipmapsStreamingMasterMaterials.IMipmapsFactorProvider {
        // In editor we can move way faster so max fill value is higher
        const float EditorFillValueMultiplier = 2;
        const int Object2WorldOffset = 0;
        const float RandomBigNumber = 1048576.0f;
        const int MaxRemapsPerFrameCount = 512;
        // Empirically found at campaign map
        const int PredictedMaxSingularRemaps = 16;
        const float MaxPerFrameTransformsMultiplier = 0.15f;

        const int RandomPreallocSmallSize = 16;
        const int RandomPreallocMediumSize = 128;
        const int RandomPreallocBigSize = 512;
        const int RandomPreallocLargeSize = 1024;

        static readonly int ObjectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        static readonly int ObjectToWorldPreviousID = Shader.PropertyToID("unity_MatrixPreviousM");
        static readonly int WorldToObjectID = Shader.PropertyToID("unity_WorldToObject");

        static readonly UniversalProfilerMarker PrepareCallMarker = new UniversalProfilerMarker("Leshy.BatchRendererGroup.PrepareCall");
        static readonly UniversalProfilerMarker AllocateRangesMarker = new UniversalProfilerMarker("Leshy.AllocateRanges");
        static readonly UniversalProfilerMarker FillGPUMarker = new UniversalProfilerMarker("Leshy.FillGPU");
        static readonly UniversalProfilerMarker ConsolidateAfterRemovalsMarker = new UniversalProfilerMarker("Leshy.ConsolidateAfterRemovals");
        static readonly UniversalProfilerMarker ConsolidateAfterAdditionsMarker = new UniversalProfilerMarker("Leshy.ConsolidateAfterAdditions");

        // ReSharper disable InconsistentNaming
        static readonly int TransformsCSId = Shader.PropertyToID("_Transforms");
        static readonly int TransformsLengthCSId = Shader.PropertyToID("_TransformsLength");
        static readonly int RemapsCSId = Shader.PropertyToID("_Remaps");
        static readonly int RemapsLengthCSId = Shader.PropertyToID("_RemapsLength");
        static readonly int InverseStartIndexCSId = Shader.PropertyToID("_InverseStartIndex");
        static readonly int OutputCSId = Shader.PropertyToID("_Output");
        // ReSharper restore InconsistentNaming

        LeshyManager _manager;

        BatchRendererGroup _brg;

        GraphicsBuffer _graphicsBuffer;
        GraphicsBufferHandle _graphicsBufferHandle;

        GraphicsBuffer _transformsBuffer;
        GraphicsBuffer _remaps;
        ComputeShader _fillGPUShader;
        int _fillGPUBufferKernel;

        int _maxInstances;
        int _maxTransformsPerFrameCount;

        BatchID _batchID;

        NativeHashMap<BatchMeshID, int> _meshToReciprocalUVDistributionIndex;
        NativeList<float> _reciprocalUVDistributionMetrics;
        NativeList<RendererDefinition> _renderers;
        NativeList<FilterSettings> _filterSettings;

        ARUnsafeList<InstancesRange> _freeRanges;
        ARUnsafeList<InstancesRange> _takenRanges;
        UnsafeInfiniteBitmask _takenRangesBitmask;

        NativeList<RangeRendererPair> _rangeRendererPairs;
        NativeList<FilerSettingsRendererPair> _filterSettingsRendererPairs;

        int _possibleLayersMask;

        int _transformsRegistered;
        int _remapsRegistered;

        NativeArray<SmallTransformWithPadding> _gpuTransformsTarget;
        NativeArray<Remap> _gpuRemapsTarget;
        CommandBuffer _commandBuffer;

        // === Jobs
        ForFrameValue<JobHandle> _performCullingJobHandle;
        AllocateRangesJob _allocateRangesJob;
        OnPerformCullingJob _onPerformCullingJob;
        JobHandle _dumpMipmapsJobHandle;

        public ARUnsafeList<InstancesRange>.ReadOnly TakenRanges => _takenRanges.AsReadOnly();
        public ARUnsafeList<InstancesRange>.ReadOnly FreeRanges => _freeRanges.AsReadOnly();

        public bool Enabled { get; set; } = true;

        public void Init(uint allInstancesCount, int maxInstancesInCell, float maxFillValue, LeshyManager manager,
            ComputeShader fillGPUBuffer) {
            _manager = manager;

            _fillGPUShader = fillGPUBuffer;
            _fillGPUBufferKernel = _fillGPUShader.FindKernel("CSMain");

            _maxInstances = (int)math.ceil(allInstancesCount * math.min(1f, Application.isPlaying ? maxFillValue : maxFillValue*EditorFillValueMultiplier));
            _maxTransformsPerFrameCount = (int)math.ceil(allInstancesCount * MaxPerFrameTransformsMultiplier);
            _maxTransformsPerFrameCount = math.max(_maxTransformsPerFrameCount, maxInstancesInCell);

            _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, _maxInstances*2, PackedMatrix.Stride);
            _graphicsBufferHandle = _graphicsBuffer.bufferHandle;
            _transformsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, _maxTransformsPerFrameCount, UnsafeUtility.SizeOf<SmallTransformWithPadding>());
            _gpuTransformsTarget = new NativeArray<SmallTransformWithPadding>(_maxTransformsPerFrameCount, Allocator.Persistent);
            _remaps = new GraphicsBuffer(GraphicsBuffer.Target.Raw, MaxRemapsPerFrameCount, UnsafeUtility.SizeOf<Remap>());
            _gpuRemapsTarget = new NativeArray<Remap>(MaxRemapsPerFrameCount, Allocator.Persistent);

            // Prealloc list's sizes are just arbitrary numbers
            _freeRanges = new ARUnsafeList<InstancesRange>(RandomPreallocMediumSize, Allocator.Persistent);
            _takenRanges = new ARUnsafeList<InstancesRange>(RandomPreallocBigSize, Allocator.Persistent);
            _takenRangesBitmask = new UnsafeInfiniteBitmask(RandomPreallocBigSize);
            _freeRanges.Add(new InstancesRange {
                gpuStartIndex = 0,
                count = (uint)_maxInstances
            });

            _meshToReciprocalUVDistributionIndex = new NativeHashMap<BatchMeshID, int>(RandomPreallocMediumSize*2, Allocator.Persistent);
            _reciprocalUVDistributionMetrics = new NativeList<float>(RandomPreallocMediumSize, Allocator.Persistent);
            _renderers = new NativeList<RendererDefinition>(RandomPreallocMediumSize, Allocator.Persistent);
            _filterSettings = new NativeList<FilterSettings>(RandomPreallocSmallSize, Allocator.Persistent);

            _rangeRendererPairs = new NativeList<RangeRendererPair>(RandomPreallocLargeSize, Allocator.Persistent);
            _filterSettingsRendererPairs = new NativeList<FilerSettingsRendererPair>(RandomPreallocMediumSize, Allocator.Persistent);

            _brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
            _brg.SetEnabledViewTypes(new[] {
                BatchCullingViewType.Camera,
                BatchCullingViewType.Light,
                BatchCullingViewType.Picking,
                BatchCullingViewType.SelectionOutline,
            });

            Bounds bounds = new Bounds(Vector3.zero, RandomBigNumber.UniformVector3());
            _brg.SetGlobalBounds(bounds);

            CreateBatch();

            MipmapsStreamingMasterMaterials.Instance.AddProvider(this);

            _commandBuffer = new CommandBuffer {
                name = "Leshy",
            };
        }

        public void Dispose() {
            _dumpMipmapsJobHandle.Complete();
            _dumpMipmapsJobHandle = default;

            MipmapsStreamingMasterMaterials.Instance.RemoveProvider(this);

            _commandBuffer?.Dispose();

            _brg.Dispose();
            _brg = null;

            _graphicsBuffer.Dispose();
            _graphicsBuffer = null;
            _transformsBuffer.Dispose();
            _transformsBuffer = null;
            _remaps.Dispose();
            _remaps = null;

            _gpuTransformsTarget.Dispose();
            _gpuRemapsTarget.Dispose();

            _freeRanges.Dispose();
            _takenRanges.Dispose();
            _takenRangesBitmask.Dispose();
            for (int i = 0; i < _renderers.Length; i++) {
                _renderers[i].materialsID.Dispose();
                for (uint j = 0; j < _renderers[i].mipmapsID.Length; j++) {
                    MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(_renderers[i].mipmapsID[j]);
                }
                _renderers[i].mipmapsID.Dispose();
            }
            _renderers.Dispose();

            _meshToReciprocalUVDistributionIndex.Dispose();
            _reciprocalUVDistributionMetrics.Dispose();

            _filterSettings.Dispose();

            _rangeRendererPairs.Dispose();
            _filterSettingsRendererPairs.Dispose();
            _possibleLayersMask = 0;
        }

        void CreateBatch() {
            var batchMetadata = new NativeArray<MetadataValue>(3, ARAlloc.Temp, NativeArrayOptions.UninitializedMemory);

            batchMetadata[0] = CreateMetadataValue(ObjectToWorldID, Object2WorldOffset, true); // matrices
            batchMetadata[1] = CreateMetadataValue(WorldToObjectID, _maxInstances * PackedMatrix.Stride, true); // inverse matrices
            batchMetadata[2] = CreateMetadataValue(ObjectToWorldPreviousID, Object2WorldOffset, true); // previous matrices

            _batchID = _brg.AddBatch(batchMetadata, _graphicsBufferHandle);
            _onPerformCullingJob.batchID = _batchID;

            batchMetadata.Dispose();
        }

        // === Register assets
        public (BatchMaterialID, MipmapsStreamingMasterMaterials.MaterialId) RegisterMaterial(Material material) {
            var batchId = _brg.RegisterMaterial(material);
            var mipmapsId = MipmapsStreamingMasterMaterials.Instance.AddMaterial(material);
            return (batchId, mipmapsId);
        }

        public (BatchMeshID, int) RegisterMesh(Mesh mesh) {
            var meshId = _brg.RegisterMesh(mesh);
            if (!_meshToReciprocalUVDistributionIndex.TryGetValue(meshId, out var reciprocalIndex)) {
                reciprocalIndex = _reciprocalUVDistributionMetrics.Length;
                var uvDistributionMetric = math.min(mesh.GetUVDistributionMetric(0), 15);
                var reciprocalUVDistributionMetric = math.rcp(uvDistributionMetric);
                _reciprocalUVDistributionMetrics.Add(reciprocalUVDistributionMetric);
                _meshToReciprocalUVDistributionIndex.TryAdd(meshId, reciprocalIndex);
            }
            return (meshId, reciprocalIndex);
        }

        public FilterSettingsId RegisterFilterSettings(FilterSettings filterSettings) {
            var index = _filterSettings.IndexOf(filterSettings);
            if (index == -1) {
                index = _filterSettings.Length;
                _filterSettings.Add(filterSettings);
                _onPerformCullingJob.filterSettings = _filterSettings.AsArray();
                _possibleLayersMask |= 1 << filterSettings.layer;
            }
            return new FilterSettingsId { value = (ushort)index };
        }

        public RendererId RegisterRenderer(RendererDefinition rendererDefinition) {
            var index = _renderers.IndexOf(rendererDefinition);
            if (index == -1) {
                index = _renderers.Length;
                _renderers.Add(rendererDefinition);
                _filterSettingsRendererPairs.Add(new FilerSettingsRendererPair {
                    filterSettingsIndex = rendererDefinition.filterSettingsId,
                    rendererId = (ushort)index,
                });
                _filterSettingsRendererPairs.Sort(new FilerSettingsRendererPair.SettingsIndexComparer());
                _onPerformCullingJob.renderers = _renderers.AsArray();
                _onPerformCullingJob.filterSettingsRendererPairs = _filterSettingsRendererPairs.AsArray();
            } else {
                rendererDefinition.materialsID.Dispose();
                for (uint i = 0; i < rendererDefinition.mipmapsID.Length; i++) {
                    MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(rendererDefinition.mipmapsID[i]);
                }
                rendererDefinition.mipmapsID.Dispose();
            }
            return new RendererId { value = (ushort)index };
        }

        // === Instances
        public RenderingInstancesHandle AddInstances(UnsafeArray<SmallTransform>.Span transforms, UnsafeArray<RendererId> renderers) {
            if (_remapsRegistered + PredictedMaxSingularRemaps > _remaps.count) {
                return default;
            }
            if (_transformsRegistered + transforms.Length > _transformsBuffer.count) {
                return default;
            }

            AllocateRangesMarker.Begin();
            var allocatedRangesRef = new NativeReference<UnsafeArray<RangeId>>(ARAlloc.TempJob);
            var cellLodVisibilities = new UnsafeArray<byte>(transforms.Length, ARAlloc.Persistent);
            var instanceVisibilities = new UnsafeBitArray((int)transforms.Length, ARAlloc.Persistent);
            var mipmapsFactor = new UnsafeArray<float>(transforms.Length, ARAlloc.Persistent);

            fixed (ARUnsafeList<InstancesRange>* freeRangesRef = &_freeRanges)
            fixed (ARUnsafeList<InstancesRange>* takenRangesRef = &_takenRanges)
            fixed (UnsafeInfiniteBitmask* takenRangesBitmaskRef = &_takenRangesBitmask) {
                _allocateRangesJob.instancesCount = transforms.Length;
                _allocateRangesJob.renderers = renderers;
                _allocateRangesJob.freeRangesRef = freeRangesRef;
                _allocateRangesJob.takenRangesBitmaskRef = takenRangesBitmaskRef;
                _allocateRangesJob.takenRangesRef = takenRangesRef;
                _allocateRangesJob.rangeRendererPairs = _rangeRendererPairs;
                _allocateRangesJob.cellLodVisibilities = cellLodVisibilities;
                _allocateRangesJob.instancesVisibilities = instanceVisibilities;
                _allocateRangesJob.mipmapsFactors = mipmapsFactor;
                _allocateRangesJob.allocatedRangesRef = allocatedRangesRef;
                _allocateRangesJob.RunByRef();
            }
            AllocateRangesMarker.End();

            var allocatedRanges = allocatedRangesRef.Value;
            allocatedRangesRef.Dispose();
            // === Fill buffers
            FillGPUMarker.Begin();

            var remapsCount = (int)allocatedRanges.Length;
            var remapDestination = ((Remap*)_gpuRemapsTarget.GetUnsafePtr()) + _remapsRegistered;

            var cpuStart = (uint)_transformsRegistered;
            for (var i = 0u; i < remapsCount; i++) {
                var takenRangeIndex = allocatedRanges[i];
                ref var takenRange = ref _takenRanges[takenRangeIndex];
                var remap = new Remap {
                    cpuStart = cpuStart,
                    cpuEnd = cpuStart+takenRange.count,
                    gpuStart = takenRange.gpuStartIndex,
                };
                remapDestination[i] = remap;
                cpuStart += takenRange.count;
            }
            _remapsRegistered += remapsCount;

            var transformsLength = (int)transforms.Length;
            var transformsDestination = ((SmallTransformWithPadding*)_gpuTransformsTarget.GetUnsafePtr()) + _transformsRegistered;
            UnsafeUtility.MemCpyStride(
                transformsDestination,
                UnsafeUtility.SizeOf<SmallTransformWithPadding>(),
                transforms.Ptr,
                UnsafeUtility.SizeOf<SmallTransform>(),
                UnsafeUtility.SizeOf<SmallTransform>(),
                transformsLength
                );
            _transformsRegistered += transformsLength;

            FillGPUMarker.End();

            return new RenderingInstancesHandle {
                instancesSelectedLod = cellLodVisibilities,
                instanceVisibilities = instanceVisibilities,
                rangeIds = allocatedRanges,
                mipmapsFactors = mipmapsFactor,
            };
        }

        public void BeginAdditions() {
            _transformsRegistered = 0;
            _remapsRegistered = 0;

            _dumpMipmapsJobHandle.Complete();
            _dumpMipmapsJobHandle = default;
        }

        public void EndAdditions() {
            if (_transformsRegistered == 0) {
                return;
            }

            _commandBuffer.Clear();
            _commandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

            _commandBuffer.SetBufferData(_transformsBuffer, _gpuTransformsTarget, 0, 0, _transformsRegistered);
            _commandBuffer.SetBufferData(_remaps, _gpuRemapsTarget, 0, 0, _remapsRegistered);

            _commandBuffer.SetComputeBufferParam(_fillGPUShader, _fillGPUBufferKernel, TransformsCSId, _transformsBuffer);
            _commandBuffer.SetComputeIntParam(_fillGPUShader, TransformsLengthCSId, _transformsRegistered);

            _commandBuffer.SetComputeBufferParam(_fillGPUShader, _fillGPUBufferKernel, RemapsCSId, _remaps);
            _commandBuffer.SetComputeIntParam(_fillGPUShader, RemapsLengthCSId, _remapsRegistered);

            _commandBuffer.SetComputeBufferParam(_fillGPUShader, _fillGPUBufferKernel, OutputCSId, _graphicsBuffer);
            _commandBuffer.SetComputeIntParam(_fillGPUShader, InverseStartIndexCSId, _maxInstances);


            _commandBuffer.DispatchCompute(_fillGPUShader, _fillGPUBufferKernel, Mathf.CeilToInt(_transformsRegistered / 64f), 1, 1);
            UnityEngine.Graphics.ExecuteCommandBuffer(_commandBuffer);
        }

        public void ConsolidateAfterAdditions() {
            ConsolidateAfterAdditionsMarker.Begin();
            _rangeRendererPairs.Sort(new RangeRendererPair.RenderComparer());

            _onPerformCullingJob.rangeRendererPairs = *_rangeRendererPairs.GetUnsafeList();
            _onPerformCullingJob.takenRanges = _takenRanges.AsUnsafeSpan();
            ConsolidateAfterAdditionsMarker.End();
        }

        public void RemoveInstances(in RenderingInstancesHandle rangesHandle) {
            var toRemove = rangesHandle.rangeIds;
            for (uint i = 0; i < toRemove.Length; i++) {
                var rangeId = toRemove[i];
                ref var taken = ref _takenRanges[rangeId];
                _freeRanges.Add(taken);
                _takenRangesBitmask.DownNoChecks(rangeId);
            }
        }

        public void ConsolidateAfterRemovals() {
            ConsolidateAfterRemovalsMarker.Begin();

            _dumpMipmapsJobHandle.Complete();
            _dumpMipmapsJobHandle = default;

            for (int i = _rangeRendererPairs.Length - 1; i >= 0; i--) {
                if (!_takenRangesBitmask[_rangeRendererPairs[i].rangeId]) {
                    _rangeRendererPairs.RemoveAtSwapBack(i);
                }
            }
            _rangeRendererPairs.Sort(new RangeRendererPair.RenderComparer());

            for (var i = _takenRanges.Length - 1; i >= 0; i--) {
                if (!_takenRangesBitmask[(uint)i]) {
                    _takenRanges.Length--;
                } else {
                    break;
                }
            }

            UnsafeSort.Sort(_freeRanges, new InstancesRange.StartIndexComparer());
            for (var i = _freeRanges.Length - 1; i >= 1; i--) {
                ref var prev = ref _freeRanges[i - 1];
                var curr = _freeRanges[i];
                if (prev.gpuStartIndex + prev.count == curr.gpuStartIndex) {
                    prev.count += curr.count;
                    _freeRanges.RemoveAtSwapBack(i);
                }
            }

            _onPerformCullingJob.rangeRendererPairs = *_rangeRendererPairs.GetUnsafeList();
            _onPerformCullingJob.takenRanges = _takenRanges.AsUnsafeSpan();
            ConsolidateAfterRemovalsMarker.End();
        }

        public void RunMipmapsDumpJob(JobHandle dependency, in MipmapsStreamingMasterMaterials.ParallelWriter mipmapsStreamingWriter) {
            dependency = new DumpMipmapsJob {
                rangeRendererPairs = _rangeRendererPairs.AsArray(),
                rendererDefinitions = _renderers.AsArray(),
                reciprocalUVDistributions = _reciprocalUVDistributionMetrics.AsArray(),

                mipmapsWriter = mipmapsStreamingWriter,
            }.ScheduleParallel(_rangeRendererPairs.Length, 128, JobHandle.CombineDependencies(dependency, _dumpMipmapsJobHandle));
            _dumpMipmapsJobHandle = mipmapsStreamingWriter.Dispose(dependency);
        }

        // === Emit commands

        // === Helpers
        JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput, IntPtr userContext) {
            if (!Enabled || _rangeRendererPairs.Length == 0) {
                return default;
            }

            // Nothing to render
            if ((_possibleLayersMask & cullingContext.cullingLayerMask) == 0) {
                return default;
            }

#if UNITY_EDITOR
            if ((_manager.gameObject.sceneCullingMask & cullingContext.sceneCullingMask) == 0) {
                return default;
            }
            if (cullingContext.viewType is BatchCullingViewType.Picking or BatchCullingViewType.SelectionOutline) {
                return default;
            }
#endif

            var performCullingJobHandle = (JobHandle)_performCullingJobHandle;

            if (cullingContext.viewType == BatchCullingViewType.Camera) {
                performCullingJobHandle = _manager.CullCameraInstances(cullingContext, performCullingJobHandle);
            } else if (cullingContext.viewType == BatchCullingViewType.Light) {
                performCullingJobHandle = _manager.CullLightInstances(cullingContext, performCullingJobHandle);
            } else {
                Log.Important?.Error($"Leshy BRG: Unknown view type: {cullingContext.viewType}");
                return default;
            }

            var unsafeRangeRenderingPairs = *_rangeRendererPairs.GetUnsafeList();

            PrepareCallMarker.Begin();
            performCullingJobHandle = new CalculateVisibleInstancesJob {
                rangeRendererPairs = unsafeRangeRenderingPairs,
                renderers = _renderers.AsArray(),
                takenRanges = _takenRanges.AsUnsafeSpan(),
            }.ScheduleParallel(unsafeRangeRenderingPairs.Length, 1, performCullingJobHandle);

            var drawCommandsAndInstancesCount = new NativeArray<uint>(2, ARAlloc.TempJob, NativeArrayOptions.UninitializedMemory);

            performCullingJobHandle = new CalculateDrawCommandAndInstancesCountJob {
                drawCommandsAndInstancesCount = drawCommandsAndInstancesCount,
                rangeRendererPairs = unsafeRangeRenderingPairs,
                renderers = _renderers.AsArray(),
            }.Schedule(performCullingJobHandle);

            var onPerformCullingJob = _onPerformCullingJob;
            onPerformCullingJob.drawCommandsAndInstancesCount = drawCommandsAndInstancesCount;
            onPerformCullingJob.cullingOutputDrawCommands = cullingOutput.drawCommands;
            performCullingJobHandle = onPerformCullingJob.Schedule(performCullingJobHandle);

            performCullingJobHandle = drawCommandsAndInstancesCount.Dispose(performCullingJobHandle);
            PrepareCallMarker.End();

            _performCullingJobHandle.Value = performCullingJobHandle;
            return performCullingJobHandle;
        }

        public void ProvideMipmapsFactors(in CameraData cameraData, in MipmapsStreamingMasterMaterials.ParallelWriter writer) {
            _manager.RunMipmapsStreaming(cameraData, writer);
        }

        static MetadataValue CreateMetadataValue(int nameID, int gpuOffset, bool isPerInstance) {
            const uint IsPerInstanceBit = 0x80000000;
            return new MetadataValue {
                NameID = nameID,
                Value = (uint)gpuOffset | (isPerInstance ? IsPerInstanceBit : 0),
            };
        }

        public struct InstancesRange {
            [Sirenix.OdinInspector.ShowInInspector] public uint count;
            [Sirenix.OdinInspector.ShowInInspector] public uint gpuStartIndex;

            public struct StartIndexComparer : IComparer<InstancesRange> {
                public int Compare(InstancesRange x, InstancesRange y) {
                    return x.gpuStartIndex.CompareTo(y.gpuStartIndex);
                }
            }
        }

        struct RangeRendererPair {
            public RangeId rangeId;
            public RendererId rendererId;

            public uint rendererInstancesStartIndex;

            public UnsafeArray<byte> instancesLodVisibility; // We dont own this
            public UnsafeBitArray instancesVisibilities; // We dont own this
            public UnsafeArray<float> mipmapsFactors; // We dont own this

            public uint visibleInstancesCount;

            public readonly struct FindWithRendererId : IEquatable<RangeRendererPair> {
                public readonly RendererId prefabId;

                public FindWithRendererId(RendererId prefabId) {
                    this.prefabId = prefabId;
                }

                public bool Equals(RangeRendererPair other) {
                    return prefabId == other.rendererId;
                }
            }

            public struct RenderComparer : IComparer<RangeRendererPair> {
                public int Compare(RangeRendererPair x, RangeRendererPair y) {
                    var prefabsComparison = x.rendererId.CompareTo(y.rendererId);
                    if (prefabsComparison == 0) {
                        return x.rangeId.CompareTo(y.rangeId);
                    }
                    return prefabsComparison;
                }
            }
        }

        struct FilerSettingsRendererPair {
            public FilterSettingsId filterSettingsIndex;
            public RendererId rendererId;

            public struct SettingsIndexComparer : IComparer<FilerSettingsRendererPair> {
                public int Compare(FilerSettingsRendererPair x, FilerSettingsRendererPair y) {
                    var settingsComparison = x.filterSettingsIndex.CompareTo(y.filterSettingsIndex);
                    if (settingsComparison == 0) {
                        return x.rendererId.CompareTo(y.rendererId);
                    }
                    return settingsComparison;
                }
            }
        }

        // === Jobs
        [BurstCompile]
        struct AllocateRangesJob : IJob {
            public uint instancesCount;
            [ReadOnly] public UnsafeArray<RendererId> renderers;
            [ReadOnly] public UnsafeArray<byte> cellLodVisibilities;
            [ReadOnly] public UnsafeBitArray instancesVisibilities;
            [ReadOnly] public UnsafeArray<float> mipmapsFactors;

            [NativeDisableUnsafePtrRestriction] public ARUnsafeList<InstancesRange>* freeRangesRef;
            [NativeDisableUnsafePtrRestriction] public UnsafeInfiniteBitmask* takenRangesBitmaskRef;
            [NativeDisableUnsafePtrRestriction] public ARUnsafeList<InstancesRange>* takenRangesRef;
            [WriteOnly] public NativeList<RangeRendererPair> rangeRendererPairs;

            [WriteOnly] public NativeReference<UnsafeArray<RangeId>> allocatedRangesRef;

            public void Execute() {
                var remainingInstances = instancesCount;

                ref var freeRanges = ref *freeRangesRef;
                ref var takenRangesBitmask = ref *takenRangesBitmaskRef;
                ref var takenRanges = ref *takenRangesRef;

                var allocatedRanges = PredictTakenRanges(remainingInstances, freeRanges.AsReadOnly());

                allocatedRangesRef.Value = allocatedRanges;

                takenRangesBitmask.EnsureCapacity((uint)(takenRanges.Length+allocatedRanges.Length));

                uint instancesStartIndex = 0;
                for (uint r = 0; r < allocatedRanges.Length; ++r) {
                    var rangeIndex = allocatedRanges[r];

                    var range = freeRanges[rangeIndex];
                    var count = math.min(remainingInstances, range.count);
                    remainingInstances -= count;

                    if (count == range.count) {
                        // take whole
                        freeRanges.RemoveAt(rangeIndex);
                    } else {
                        // take part
                        var leftRange = new InstancesRange {
                            gpuStartIndex = range.gpuStartIndex + count,
                            count = range.count - count,
                        };
                        freeRanges[rangeIndex] = leftRange;
                        range.count = count;
                    }

                    var takenIndex = takenRangesBitmask.FirstZero();
                    if (takenIndex == -1 || takenIndex >= takenRanges.Length) {
                        takenIndex = takenRanges.Length;
                        takenRanges.Add(new InstancesRange());
                    }

                    takenRanges[takenIndex] = range;
                    takenRangesBitmask.UpNoChecks((uint)takenIndex);

                    for (uint i = 0; i < renderers.Length; i++) {
                        rangeRendererPairs.Add(new() {
                            rangeId = (ushort)takenIndex,
                            rendererId = renderers[i],

                            rendererInstancesStartIndex = instancesStartIndex,
                            instancesLodVisibility = cellLodVisibilities,

                            instancesVisibilities = instancesVisibilities,
                            mipmapsFactors = mipmapsFactors,
                        });
                    }
                    allocatedRanges[r] = (ushort)takenIndex;
                    instancesStartIndex += range.count;
                }
            }

            static UnsafeArray<RangeId> PredictTakenRanges(uint remainingInstances, ARUnsafeList<InstancesRange>.ReadOnly freeRanges) {
                if (freeRanges.Length == 0) {
                    return new UnsafeArray<RangeId>(0, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
                }
                if (freeRanges.Length == 1) {
                    var range = new UnsafeArray<RangeId>(1, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
                    range[0] = 0;
                    return range;
                }

                var largeEnoughRangeIndex = freeRanges.Length;
                var largeEnoughRangeCount = uint.MaxValue;
                for (var r = freeRanges.Length - 1; r >= 0; r--) {
                    var range = freeRanges[r];
                    if (range.count >= remainingInstances) {
                        if (range.count < largeEnoughRangeCount) {
                            largeEnoughRangeIndex = r;
                            largeEnoughRangeCount = range.count;
                        }
                    }
                }

                if (largeEnoughRangeIndex < freeRanges.Length) {
                    var range = new UnsafeArray<RangeId>(1, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
                    range[0] = (ushort)largeEnoughRangeIndex;
                    return range;
                }

                var potentialRanges = new UnsafeList<ushort>(4, ARAlloc.Temp);

                // predictRemainingInstances can be negative
                var predictRemainingInstances = (long)remainingInstances;
                bool anyAdded;
                do {
                    anyAdded = false;
                    var closestRangeIndex = freeRanges.Length;
                    var closestRangeDiff = uint.MaxValue;
                    for (var r = freeRanges.Length - 1; r >= 0; --r) {
                        if (potentialRanges.Contains((ushort)r)) {
                            continue;

                        }
                        var range = freeRanges[r];
                        var diff = (int)range.count - (int)predictRemainingInstances;
                        if (diff < 0) {
                            diff = -diff + 1_000_000_000; // about half of max int
                        }
                        if (diff < closestRangeDiff) {
                            closestRangeIndex = r;
                            closestRangeDiff = (uint)diff;
                        }
                    }
                    if (closestRangeIndex < freeRanges.Length) {
                        potentialRanges.Add((ushort)closestRangeIndex);
                        predictRemainingInstances -= freeRanges[closestRangeIndex].count;
                        anyAdded = true;
                    }
                }
                while (predictRemainingInstances > 0 && anyAdded);

                var ranges = new UnsafeArray<RangeId>((uint)potentialRanges.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                for (var r = 0; r < potentialRanges.Length; ++r) {
                    ranges[(uint)r] = potentialRanges[r];
                    var currentRange = potentialRanges[r];

                    for (var i = r+1; i < potentialRanges.Length; ++i) {
                        if (currentRange < potentialRanges[i]) {
                            potentialRanges[i] = (ushort)(potentialRanges[i]-1);
                        }
                    }
                }
                potentialRanges.Dispose();

                return ranges;
            }
        }

        [BurstCompile]
        struct OnPerformCullingJob : IJob {
            public BatchID batchID;
            [WriteOnly] public NativeArray<BatchCullingOutputDrawCommands> cullingOutputDrawCommands;

            [ReadOnly] public NativeArray<uint> drawCommandsAndInstancesCount;
            [ReadOnly] public NativeArray<FilterSettings> filterSettings;
            [ReadOnly] public NativeArray<FilerSettingsRendererPair> filterSettingsRendererPairs;
            [ReadOnly] public UnsafeList<RangeRendererPair> rangeRendererPairs;
            [ReadOnly] public NativeArray<RendererDefinition> renderers;
            [ReadOnly] public UnsafeArray<InstancesRange>.Span takenRanges;

            // Slightly overallocate to prevent some weird bug when we have our count is wrong
            uint MaxDrawCommandsCount => drawCommandsAndInstancesCount[0]+5;
            uint MaxInstancesCount => drawCommandsAndInstancesCount[1]+100;

            public void Execute() {
                var drawCommands = default(BatchCullingOutputDrawCommands);
                drawCommands.instanceSortingPositions = null;
                drawCommands.instanceSortingPositionFloatCount = 0;

                drawCommands.visibleInstances = MallocDrawMemory<int>(MaxInstancesCount);

                var drawRangesCount = filterSettings.Length;

                drawCommands.drawCommands = MallocDrawMemory<BatchDrawCommand>(MaxDrawCommandsCount);
                drawCommands.drawRanges = MallocDrawMemory<BatchDrawRange>((uint)drawRangesCount);

                uint visibleInstancesIndex = 0;
                uint drawCommandIndex = 0;
                uint drawRangeIndex = 0;
                var settingsRendererPairIndex = 0;

                for (int i = 0; i < filterSettings.Length; i++) {
                    EmitDrawsForFilterSettings(drawCommands, i,
                        ref drawCommandIndex, ref settingsRendererPairIndex, ref visibleInstancesIndex, ref drawRangeIndex);
                }

                drawCommands.drawCommandCount = (int)drawCommandIndex;
                drawCommands.drawRangeCount = (int)drawRangeIndex;

                cullingOutputDrawCommands[0] = drawCommands;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void EmitDrawsForFilterSettings(BatchCullingOutputDrawCommands drawCommands, int filterSettingsIndex,
                ref uint drawCommandIndex, ref int settingsRendererPairIndex, ref uint visibleInstancesIndex,
                ref uint drawRangeIndex) {

                var filerSettings = filterSettings[filterSettingsIndex];
                var drawRange = new BatchDrawRange {
                    drawCommandsType = BatchDrawCommandType.Direct,
                    drawCommandsBegin = drawCommandIndex,
                    filterSettings = new BatchFilterSettings {
                        // Per renderer
                        renderingLayerMask = filerSettings.renderingLayerMask,
                        layer = filerSettings.layer,
                        shadowCastingMode = filerSettings.shadowCastingMode,
                        motionMode = filerSettings.motionVectorGenerationMode,
                        // Static
                        receiveShadows = true,
                        staticShadowCaster = true,
                        allDepthSorted = false
                    }
                };

                // go to first pair for chosen filter settings
                while (settingsRendererPairIndex < filterSettingsRendererPairs.Length &&
                       filterSettingsRendererPairs[settingsRendererPairIndex].filterSettingsIndex < filterSettingsIndex) {
                    settingsRendererPairIndex++;
                }
                // go through all pairs for chosen filter settings
                var rangeRendererPairIndex = 0;
                while (settingsRendererPairIndex < filterSettingsRendererPairs.Length &&
                       filterSettingsRendererPairs[settingsRendererPairIndex].filterSettingsIndex == filterSettingsIndex) {

                    var rendererId = filterSettingsRendererPairs[settingsRendererPairIndex].rendererId;
                    var rangeRendererPairStartIndex = rangeRendererPairs
                        .FindIndexOf(new RangeRendererPair.FindWithRendererId(rendererId), rangeRendererPairIndex);
                    // skip unused renderer
                    if (rangeRendererPairStartIndex == -1) {
                        settingsRendererPairIndex++;
                        continue;
                    }
                    rangeRendererPairIndex = rangeRendererPairStartIndex;

                    var renderer = renderers[rendererId];
                    var lodMask = renderer.lodMask;
                    var rendererStartVisibility = visibleInstancesIndex;

                    // go through all ranges-renderer pairs for given renderer
                    while (rangeRendererPairIndex < rangeRendererPairs.Length &&
                           rangeRendererPairs[rangeRendererPairIndex].rendererId == rendererId) {
                        FillVisibleInstances(rangeRendererPairIndex, lodMask, drawCommands.visibleInstances,
                            ref visibleInstancesIndex);

                        rangeRendererPairIndex++;
                    }

                    // skip renderer with no visible instances
                    if (rendererStartVisibility != visibleInstancesIndex) {
                        EmitDrawCommandsForRenderer(drawCommands.drawCommands, renderer,
                            ref drawCommandIndex, visibleInstancesIndex, rendererStartVisibility);
                    }

                    settingsRendererPairIndex++;
                }

                drawRange.drawCommandsCount = drawCommandIndex - drawRange.drawCommandsBegin;
                if (drawRange.drawCommandsCount > 0) {
                    drawCommands.drawRanges[drawRangeIndex++] = drawRange;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void FillVisibleInstances(int pairsIndex, byte lodMask, int* visibleInstances, ref uint visibleInstancesIndex) {
                var rangeRendererPair = rangeRendererPairs[pairsIndex];

                if (rangeRendererPair.visibleInstancesCount <= 0) {
                    return;
                }

                var instanceVisibility = rangeRendererPair.instancesVisibilities;
                var takenRange = takenRanges[rangeRendererPair.rangeId];
                var instanceIndex = rangeRendererPair.rendererInstancesStartIndex;
                for (int i = 0; i < takenRange.count; ++i, ++instanceIndex) {
                    if (instanceVisibility.IsSet((int)instanceIndex) &&
                        (rangeRendererPair.instancesLodVisibility[instanceIndex] & lodMask) != 0) {
                        visibleInstances[visibleInstancesIndex++] = (int)takenRange.gpuStartIndex + i;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void EmitDrawCommandsForRenderer(BatchDrawCommand* drawCommands, RendererDefinition renderer,
                ref uint drawCommandIndex, uint visibleInstancesIndex, uint rendererStartVisibility) {
                // emit draw command per material per renderer
                for (ushort i = 0; i < renderer.materialsID.Length; i++) {
                    drawCommands[drawCommandIndex++] = new BatchDrawCommand {
                        flags = BatchDrawCommandFlags.HasMotion,
                        batchID = batchID,
                        materialID = renderer.materialsID[i],
                        meshID = renderer.meshID,
                        splitVisibilityMask = 0xFF,
                        visibleOffset = rendererStartVisibility,
                        visibleCount = visibleInstancesIndex - rendererStartVisibility,
                        submeshIndex = i,
                    };
                }
            }

            static T* MallocDrawMemory<T>(uint count) where T : unmanaged {
                return (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * count, UnsafeUtility.AlignOf<T>(), Allocator.TempJob);
            }
        }

        [BurstCompile]
        struct CalculateVisibleInstancesJob : IJobFor {
            [ReadOnly] public UnsafeArray<InstancesRange>.Span takenRanges;
            [ReadOnly] public NativeArray<RendererDefinition> renderers;
            public UnsafeList<RangeRendererPair> rangeRendererPairs;

            public void Execute(int index) {
                ref var pair = ref rangeRendererPairs.ElementAt(index);
                var count = takenRanges[pair.rangeId].count;
                var instancesStartIndex = pair.rendererInstancesStartIndex;
                var allVisibleInstancesCount = pair.instancesVisibilities.CountBits((int)instancesStartIndex, (int)count);
                if (allVisibleInstancesCount < 1) {
                    pair.visibleInstancesCount = 0;
                    return;
                }
                var renderer = renderers[pair.rendererId];
                var lodMask = renderer.lodMask;
                uint visibleInstancesCount = 0;
                // Should be merge into single bit mask and then count bits
                for (uint i = 0; i < count; i++) {
                    var instanceIndex = instancesStartIndex + i;
                    if (pair.instancesVisibilities.IsSet((int)instanceIndex) &&
                        (pair.instancesLodVisibility[instanceIndex] & lodMask) != 0) {
                        visibleInstancesCount++;
                    }
                }
                pair.visibleInstancesCount = visibleInstancesCount;
            }
        }

        [BurstCompile]
        struct CalculateDrawCommandAndInstancesCountJob : IJob {
            [ReadOnly] public UnsafeList<RangeRendererPair> rangeRendererPairs;
            [ReadOnly] public NativeArray<RendererDefinition> renderers;

            [WriteOnly] public NativeArray<uint> drawCommandsAndInstancesCount;

            public void Execute() {
                uint drawCommandsCount = 0;
                uint instancesCount = 0;
                RendererId nextRendererToAdd = 0;
                for (int i = 0; i < rangeRendererPairs.Length; i++) {
                    var pair = rangeRendererPairs[i];
                    var visibleInstancesCount = pair.visibleInstancesCount;
                    if (visibleInstancesCount < 1) {
                        continue;
                    }
                    instancesCount += visibleInstancesCount;
                    if (nextRendererToAdd <= pair.rendererId) {
                        var prefab = renderers[pair.rendererId];
                        drawCommandsCount += prefab.materialsID.Length;
                        nextRendererToAdd = (ushort)(pair.rendererId + 1);
                    }
                }
                drawCommandsAndInstancesCount[0] = drawCommandsCount;
                drawCommandsAndInstancesCount[1] = instancesCount;
            }
        }

        [BurstCompile]
        public struct CalculateMipmapsJob : IJobFor {
            public CameraData cameraData;
            [ReadOnly] public UnsafeArray<uint> spawnedCellsIndices;

            [ReadOnly] public UnsafeArray<float> aabbCenterXs;
            [ReadOnly] public UnsafeArray<float> aabbCenterYs;
            [ReadOnly] public UnsafeArray<float> aabbCenterZs;
            [ReadOnly] public UnsafeArray<float> radii;
            [ReadOnly] public UnsafeArray<UnsafeArray<SmallTransform>.Span> cellTransforms;

            [WriteOnly] public UnsafeArray<UnsafeArray<float>.Span> mipmapsFactors;

            public void Execute(int index) {
                var cellIndex = spawnedCellsIndices[(uint)index];

                var transforms = cellTransforms[cellIndex];
                var radius = radii[cellIndex];
                var aabbCenter = new float3(aabbCenterXs[cellIndex], aabbCenterYs[cellIndex], aabbCenterZs[cellIndex]);
                ref var subMipmapsFactors = ref mipmapsFactors[cellIndex];

                for (uint i = 0; i < transforms.Length; i++) {
                    var maxScaleComponent = math.cmax(transforms[i].scale);
                    var worldRadius = radius * maxScaleComponent;
                    var worldCenter = math.transform(transforms[i].ToFloat4x4(), aabbCenter);
                    var scaleSq = math.square(maxScaleComponent);
                    var factor = MipmapsStreamingUtils.CalculateMipmapFactorFactor(in cameraData, worldCenter, worldRadius, scaleSq);
                    subMipmapsFactors[i] = factor;
                }
            }
        }

        [BurstCompile]
        struct DumpMipmapsJob : IJobFor {
            [ReadOnly] public NativeArray<RangeRendererPair> rangeRendererPairs;
            [ReadOnly] public NativeArray<RendererDefinition> rendererDefinitions;
            [ReadOnly] public NativeArray<float> reciprocalUVDistributions;

            public MipmapsStreamingMasterMaterials.ParallelWriter mipmapsWriter;

            public void Execute(int index) {
                var pair = rangeRendererPairs[index];
                var renderer = rendererDefinitions[pair.rendererId];
                var mipmapsFactors = pair.mipmapsFactors;

                var reciprocalUVDistribution = reciprocalUVDistributions[renderer.reciprocalUVDistributionID];

                for (var i = 0u; i < mipmapsFactors.Length; i++) {
                    var factor = mipmapsFactors[i];
                    for (var j = 0u; j < renderer.mipmapsID.Length; j++) {
                        var mipmapsID = renderer.mipmapsID[j];
                        var finalFactor = factor * reciprocalUVDistribution;
                        mipmapsWriter.UpdateMipFactor(mipmapsID, finalFactor);
                    }
                }
            }
        }

        // Reflect data on GPU
        struct Remap {
            [UsedImplicitly, UnityEngine.Scripting.Preserve] public uint cpuStart;
            [UsedImplicitly, UnityEngine.Scripting.Preserve] public uint cpuEnd;
            [UsedImplicitly, UnityEngine.Scripting.Preserve] public uint gpuStart;
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 6;

            long renderersSize = _renderers.SafeCapacity() * (sizeof(BatchID) + sizeof(FilterSettingsId) + sizeof(byte) + sizeof(UnsafeArray<BatchMaterialID>));
            for (int i = 0; i < _renderers.SafeLength(); i++) {
                if (_renderers[i].materialsID.IsCreated) {
                    renderersSize += _renderers[i].materialsID.Length * sizeof(BatchMaterialID);
                    renderersSize += _renderers[i].mipmapsID.Length * sizeof(int);
                }
            }
            var filterSettingsSize = _filterSettings.SafeCapacity() * sizeof(FilterSettingsId);
            var freeRangesSize = _freeRanges.Capacity * sizeof(InstancesRange);
            var takenRangesSize = _takenRanges.SafeCapacity() * sizeof(InstancesRange);
            var rangeRendererPairsSize = _rangeRendererPairs.SafeCapacity() * sizeof(RangeRendererPair);
            var filterSettingsRendererPairsSize = _filterSettingsRendererPairs.SafeCapacity() * sizeof(FilerSettingsRendererPair);

            var ownSize = renderersSize + filterSettingsSize + freeRangesSize + takenRangesSize + rangeRendererPairsSize + filterSettingsRendererPairsSize;
            ownPlace.Span[0] = new MemorySnapshot("LeshyRendering", ownSize, ownSize, memoryBuffer[..childrenCount]);

            var usedGraphicsBuffer = 0u;
            foreach (var takenRange in _takenRanges) {
                usedGraphicsBuffer += takenRange.count;
            }
            MemorySnapshotUtils.TakeSnapshot("GPU Buffer", _graphicsBuffer, usedGraphicsBuffer, memoryBuffer.Slice(0, 1));
            MemorySnapshotUtils.TakeSnapshot("GPU Transforms", _transformsBuffer, 0, memoryBuffer.Slice(1, 1));
            MemorySnapshotUtils.TakeSnapshot("GPU Remaps", _remaps, 0, memoryBuffer.Slice(2, 1));
            MemorySnapshotUtils.TakeSnapshot("CPU Transforms", _gpuTransformsTarget, memoryBuffer.Slice(3, 1));
            MemorySnapshotUtils.TakeSnapshot("CPU Remaps", _gpuRemapsTarget, memoryBuffer.Slice(4, 1));

            _takenRangesBitmask.GetMemorySnapshot(memoryBuffer[childrenCount..], memoryBuffer.Slice(5, 1));

            return childrenCount;
        }
    }

    [Serializable]
    public struct FilterSettings : IEquatable<FilterSettings> {
        public uint renderingLayerMask;
        public byte layer;
        public ShadowCastingMode shadowCastingMode;
        public MotionVectorGenerationMode motionVectorGenerationMode;

        public bool Equals(FilterSettings other) {
            return renderingLayerMask == other.renderingLayerMask &&
                   layer == other.layer &&
                   shadowCastingMode == other.shadowCastingMode &&
                   motionVectorGenerationMode == other.motionVectorGenerationMode;
        }

        public override bool Equals(object obj) {
            return obj is FilterSettings other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (int)renderingLayerMask;
                hashCode = (hashCode * 397) ^ layer.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)shadowCastingMode;
                hashCode = (hashCode * 397) ^ (int)motionVectorGenerationMode;
                return hashCode;
            }
        }

        public static bool operator ==(FilterSettings left, FilterSettings right) {
            return left.Equals(right);
        }

        public static bool operator !=(FilterSettings left, FilterSettings right) {
            return !left.Equals(right);
        }
    }

    [DebuggerDisplay("{value}")]
    public struct RangeId : IEquatable<RangeId>, IComparable<RangeId> {
        public ushort value;

        public static implicit operator RangeId(ushort value) => new() { value = value };
        public static implicit operator ushort(RangeId value) => value.value;

        public bool Equals(RangeId other) {
            return value == other.value;
        }
        public int CompareTo(RangeId other) {
            return value.CompareTo(other.value);
        }
    }

    [DebuggerDisplay("{value}")]
    public struct FilterSettingsId : IEquatable<FilterSettingsId>, IComparable<FilterSettingsId> {
        public ushort value;

        public static implicit operator FilterSettingsId(ushort value) => new() { value = value };
        public static implicit operator ushort(FilterSettingsId value) => value.value;

        public bool Equals(FilterSettingsId other) {
            return value == other.value;
        }
        public int CompareTo(FilterSettingsId other) {
            return value.CompareTo(other.value);
        }
    }

    [DebuggerDisplay("{value}")]
    public struct RendererId : IEquatable<RendererId>, IComparable<RendererId> {
        public ushort value;

        public static implicit operator RendererId(ushort value) => new() { value = value };
        public static implicit operator ushort(RendererId value) => value.value;

        public bool Equals(RendererId other) {
            return value == other.value;
        }
        public int CompareTo(RendererId other) {
            return value.CompareTo(other.value);
        }
    }

    public struct RendererDefinition : IEquatable<RendererDefinition> {
        public BatchMeshID meshID;
        public int reciprocalUVDistributionID;
        public UnsafeArray<BatchMaterialID> materialsID;
        public UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId> mipmapsID;
        public FilterSettingsId filterSettingsId;
        public byte lodMask;

        public bool Equals(RendererDefinition other) {
            return meshID.Equals(other.meshID) &&
                   filterSettingsId.Equals(other.filterSettingsId) &&
                   lodMask == other.lodMask &&
                   materialsID.Length == other.materialsID.Length &&
                   ArraysEqual(materialsID, other.materialsID);
        }

        public override bool Equals(object obj) {
            return obj is RendererDefinition other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = meshID.GetHashCode();
                hashCode = (hashCode * 397) ^ lodMask;
                hashCode = (hashCode * 397) ^ (int)materialsID.Length;
                for (uint i = 0; i < materialsID.Length; i++) {
                    hashCode = (hashCode * 397) ^ materialsID[i].GetHashCode();
                }
                return hashCode;
            }
        }

        public static bool operator ==(RendererDefinition left, RendererDefinition right) {
            return left.Equals(right);
        }

        public static bool operator !=(RendererDefinition left, RendererDefinition right) {
            return !left.Equals(right);
        }

        bool ArraysEqual(UnsafeArray<BatchMaterialID> batchMaterialID, UnsafeArray<BatchMaterialID> otherMaterialsID) {
            for (uint i = 0; i < batchMaterialID.Length; i++) {
                if (!batchMaterialID[i].Equals(otherMaterialsID[i])) {
                    return false;
                }
            }
            return true;
        }
    }

    public struct RenderingInstancesHandle {
        public UnsafeBitArray instanceVisibilities;
        public UnsafeArray<byte> instancesSelectedLod;
        public UnsafeArray<RangeId> rangeIds;
        public UnsafeArray<float> mipmapsFactors;

        public bool IsCreated => rangeIds.IsCreated;

        public void Dispose() {
            Assert.AreEqual(rangeIds.IsCreated, instancesSelectedLod.IsCreated);
            Assert.AreEqual(rangeIds.IsCreated, instanceVisibilities.IsCreated);
            if (IsCreated) {
                instancesSelectedLod.Dispose();
                rangeIds.Dispose();
                instanceVisibilities.Dispose();
                mipmapsFactors.Dispose();
            }
        }
    }
}
