using System;
using System.Collections.Generic;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Files;
using Awaken.Utility.Graphics;
using Awaken.Utility.Graphics.Mipmaps;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UniversalProfiling;

namespace Awaken.ECS.MedusaRenderer {
    [Il2CppEagerStaticClassConstruction]
    public class MedusaBrgRenderer : MipmapsStreamingMasterMaterials.IMipmapsFactorProvider, IMemorySnapshotProvider {
        const float RandomBigNumber = 1048576.0f;
        const uint SplitMasksAlloc = 64;

        static readonly UniversalProfilerMarker CameraBrgMarker = new UniversalProfilerMarker("MedusaRenderer.CameraBrg");
        static readonly UniversalProfilerMarker LightBrgMarker = new UniversalProfilerMarker("MedusaRenderer.LightBrg");

        static readonly int ObjectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        static readonly int WorldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        // From scene settings
        static readonly BatchFilterSettings FilteringSettings = new BatchFilterSettings() {
            layer = 3,
            renderingLayerMask = 257,
            rendererPriority = 0,
            motionMode = MotionVectorGenerationMode.Camera,
            shadowCastingMode = ShadowCastingMode.On,
            receiveShadows = true,
            staticShadowCaster = true,
            allDepthSorted = false
        };

        BatchRendererGroup _brg;
        BatchID _batchID;

        GraphicsBuffer _graphicsBuffer;
        GraphicsBufferHandle _graphicsBufferHandle;
        string _medusaBasePath;

        ForFrameValue<JobHandle> _performCullingJobHandle;

        // == Data
        // -- Per transform
        UnsafeArray<byte> _transformsBuffer;
        UnsafeArray<float>.Span _xs;
        UnsafeArray<float>.Span _ys;
        UnsafeArray<float>.Span _zs;
        UnsafeArray<float>.Span _radii;
        UnsafeArray<float>.Span _lodDistancesSq0;
        UnsafeArray<float>.Span _lodDistancesSq1;
        UnsafeArray<float>.Span _lodDistancesSq2;
        UnsafeArray<float>.Span _lodDistancesSq3;
        UnsafeArray<float>.Span _lodDistancesSq4;
        UnsafeArray<float>.Span _lodDistancesSq5;
        UnsafeArray<float>.Span _lodDistancesSq6;
        UnsafeArray<float>.Span _lodDistancesSq7;
        UnsafeArray<byte>.Span _lastLodMasks;

        UnsafeArray<byte> _lodVisibility;
        UnsafeArray<ushort> _splitVisibilityMask;
        // -- Per renderer definition
        UnsafeArray<uint> _renderDataCounts;
        UnsafeArray<byte> _lodMasks;
        UnsafeArray<UnsafeArray<UnsafeArray<BatchDrawCommand>>> _drawCommands; // First renderer index then split mask index then data per render datum
        UnsafeArray<float> _reciprocalUvDistributionsFlat;
        UnsafeArray<UnsafeArray<float>.Span> _reciprocalUvDistributions;
        UnsafeArray<UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>> _materialIndices;
        UnsafeArray<uint> _instancesCountsFlat;
        UnsafeArray<UnsafeArray<uint>.Span> _instancesCounts; // First renderer index then split mask index
        // -- Per renderer
        UnsafeArray<UnsafeArray<UnsafeArray<int>>> _visibleInstances; // First renderer index then split mask index and last is the data
        UnsafeArray<uint> _transformIndicesFlat;
        UnsafeArray<UnsafeArray<uint>.Span> _transformIndices;
#if UNITY_EDITOR
        BatchCullingOutputDebugData m_BatchCullingOutputDebugData;
        bool m_collectBatchCullingOutputDebugData;
#endif
        public MedusaBrgRenderer(int count, string sceneName) {
            _medusaBasePath = MedusaPersistence.Instance.BaseScenePath(sceneName);
            _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.LockBufferForWrite,
                count*2, PackedMatrix.Stride);
            _graphicsBufferHandle = _graphicsBuffer.bufferHandle;
            _brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
            _brg.SetEnabledViewTypes(new[] {
                BatchCullingViewType.Camera,
                BatchCullingViewType.Light,
                BatchCullingViewType.Picking,
                BatchCullingViewType.SelectionOutline,
            });

            var bounds = new Bounds(Vector3.zero, RandomBigNumber.UniformVector3());
            _brg.SetGlobalBounds(bounds);

            CreateBatch(count);

            MipmapsStreamingMasterMaterials.Instance.AddProvider(this);
#if UNITY_EDITOR
            m_BatchCullingOutputDebugData = new BatchCullingOutputDebugData(128);
#endif
        }

        public void Dispose() {
            MipmapsStreamingMasterMaterials.Instance.RemoveProvider(this);

            _brg.Dispose();
            _graphicsBuffer.Dispose();
            if (_transformsBuffer.IsCreated) {
                _transformsBuffer.Dispose();
                _lodVisibility.Dispose();
                _splitVisibilityMask.Dispose();
            }
            if (_renderDataCounts.IsCreated) {
                _renderDataCounts.Dispose();
                _lodMasks.Dispose();
                for (var i = 0u; i < _drawCommands.Length; i++) {
                    for (var j = 0u; j < _drawCommands[i].Length; j++) {
                        _drawCommands[i][j].Dispose();
                    }
                    _drawCommands[i].Dispose();
                    for (var j = 0u; j < _materialIndices[i].Length; j++) {
                        MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(_materialIndices[i][j]);
                    }
                    _materialIndices[i].Dispose();
                }
                _drawCommands.Dispose();
                _materialIndices.Dispose();
                _instancesCountsFlat.Dispose();
                _instancesCounts.Dispose();
            }
            if (_visibleInstances.IsCreated) {
                for (var i = 0u; i < _visibleInstances.Length; i++) {
                    for (var j = 0u; j < _visibleInstances[i].Length; j++) {
                        _visibleInstances[i][j].Dispose();
                    }
                    _visibleInstances[i].Dispose();
                }
                _visibleInstances.Dispose();
                _transformIndicesFlat.Dispose();
                _transformIndices.Dispose();
                _reciprocalUvDistributionsFlat.Dispose();
                _reciprocalUvDistributions.Dispose();
            }
        }

        public unsafe void SetTransforms(int count) {
            var matricesPath = MedusaPersistence.MatricesPath(_medusaBasePath);
            var transformsBufferPath = MedusaPersistence.TransformsPath(_medusaBasePath);

            var graphicsTarget = _graphicsBuffer.LockBufferForWrite<PackedMatrix>(0, count * 2);

            var matricesHandle = FileRead.ToExistingBuffer(matricesPath, 0, UnsafeUtility.SizeOf<PackedMatrix>() * count * 2, graphicsTarget.GetUnsafePtr());

            var bufferSize = (uint)((count * sizeof(float) * (3 + 1 + 8)) + (count * sizeof(byte))); // centers + radii + lodDistances
            _transformsBuffer = new UnsafeArray<byte>(bufferSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var bufferHandle = FileRead.ToExistingBuffer(transformsBufferPath, 0, bufferSize, _transformsBuffer.Ptr);
            bufferHandle.JobHandle.Complete();
            bufferHandle.Dispose();

            var uCount = (uint)count;
            var bufferPointer = (float*)_transformsBuffer.Ptr;
            _xs = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            bufferPointer += count;
            _ys = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            bufferPointer += count;
            _zs = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            bufferPointer += count;
            _radii = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            bufferPointer += count;
            _lodDistancesSq0 = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            bufferPointer += count;
            _lodDistancesSq1 = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            bufferPointer += count;
            _lodDistancesSq2 = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            bufferPointer += count;
            _lodDistancesSq3 = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            bufferPointer += count;
            _lodDistancesSq4 = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            bufferPointer += count;
            _lodDistancesSq5 = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            bufferPointer += count;
            _lodDistancesSq6 = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            bufferPointer += count;
            _lodDistancesSq7 = UnsafeArray<float>.FromExistingData(bufferPointer, uCount);
            var bytePointer = (byte*)(bufferPointer + count);
            _lastLodMasks = UnsafeArray<byte>.FromExistingData(bytePointer, uCount);

            matricesHandle.JobHandle.Complete();
            matricesHandle.Dispose();

            _graphicsBuffer.UnlockBufferAfterWrite<PackedMatrix>(count * 2);

            _splitVisibilityMask = new UnsafeArray<ushort>(uCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _lodVisibility = new UnsafeArray<byte>(uCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            AsyncReadManager.CloseCachedFileAsync(matricesPath).Complete();
            AsyncReadManager.CloseCachedFileAsync(transformsBufferPath).Complete();
        }

        public unsafe void SetRenderers(Renderer[] renderers, uint flatTransformsCount, uint flatReciprocalUvDistributionsCount) {
            var renderersPath = MedusaPersistence.RenderersPath(_medusaBasePath);
            _transformIndicesFlat = new UnsafeArray<uint>(flatTransformsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var transformIndicesFlatPtr = _transformIndicesFlat.Ptr;
            var transformsReadHandle = FileRead.ToExistingBuffer(renderersPath, 0, UnsafeUtility.SizeOf<uint>() * flatTransformsCount, transformIndicesFlatPtr);

            var reciprocalUvDistributionsPath = MedusaPersistence.ReciprocalUvDistributions(_medusaBasePath);
            _reciprocalUvDistributionsFlat = new UnsafeArray<float>(flatReciprocalUvDistributionsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var reciprocalUvDistributionsFlatPtr = _reciprocalUvDistributionsFlat.Ptr;
            var reciprocalUvDistributionsReadHandle = FileRead.ToExistingBuffer(reciprocalUvDistributionsPath, 0, UnsafeUtility.SizeOf<float>() * flatReciprocalUvDistributionsCount, reciprocalUvDistributionsFlatPtr);

            var uCount = (uint)renderers.Length;
            _renderDataCounts = new UnsafeArray<uint>(uCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _lodMasks = new UnsafeArray<byte>(uCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _drawCommands = new UnsafeArray<UnsafeArray<UnsafeArray<BatchDrawCommand>>>(uCount, Allocator.Persistent);
            _reciprocalUvDistributions = new UnsafeArray<UnsafeArray<float>.Span>(uCount, Allocator.Persistent);
            _materialIndices = new UnsafeArray<UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>>(uCount, Allocator.Persistent);

            _instancesCountsFlat = new UnsafeArray<uint>(uCount*SplitMasksAlloc, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var instancesCountsPtr = _instancesCountsFlat.Ptr;
            _instancesCounts = new UnsafeArray<UnsafeArray<uint>.Span>(uCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            _visibleInstances = new UnsafeArray<UnsafeArray<UnsafeArray<int>>>(uCount, Allocator.Persistent);
            _transformIndices = new UnsafeArray<UnsafeArray<uint>.Span>(uCount, Allocator.Persistent);

            transformsReadHandle.JobHandle.Complete();
            reciprocalUvDistributionsReadHandle.JobHandle.Complete();
            transformsReadHandle.Dispose();
            reciprocalUvDistributionsReadHandle.Dispose();

            var closeRenderers = AsyncReadManager.CloseCachedFileAsync(renderersPath);
            var closeReciprocalUvDistributions = AsyncReadManager.CloseCachedFileAsync(reciprocalUvDistributionsPath, closeRenderers);

            for (var i = 0u; i < uCount; i++) {
                var renderer = renderers[i];
                _renderDataCounts[i] = (uint)renderer.renderData.Count;
                _lodMasks[i] = renderer.lodMask;
                _drawCommands[i] = new UnsafeArray<UnsafeArray<BatchDrawCommand>>(SplitMasksAlloc, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                _materialIndices[i] = new UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>(_renderDataCounts[i], Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                for (var s = 0u; s < SplitMasksAlloc; s++) {
                    _drawCommands[i][s] = new UnsafeArray<BatchDrawCommand>(_renderDataCounts[i], Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    for (uint j = 0; j < _renderDataCounts[i]; j++) {
                        var renderDatum = renderer.renderData[(int)j];
                        _drawCommands[i][s][j] = new BatchDrawCommand {
                            batchID = _batchID,
                            materialID = RegisterMaterial(renderDatum.material),
                            meshID = RegisterMesh(renderDatum.mesh),
                            submeshIndex = renderDatum.subMeshIndex,
                            splitVisibilityMask = 0xff,
                            flags = BatchDrawCommandFlags.None,
                        };
                    }
                }
                for (uint j = 0; j < _renderDataCounts[i]; j++) {
                    var renderDatum = renderer.renderData[(int)j];
                    _materialIndices[i][j] = MipmapsStreamingMasterMaterials.Instance.AddMaterial(renderDatum.material);
                }

                _instancesCounts[i] = UnsafeArray<uint>.FromExistingData(instancesCountsPtr, SplitMasksAlloc);
                instancesCountsPtr += SplitMasksAlloc;

                _visibleInstances[i] = new UnsafeArray<UnsafeArray<int>>(SplitMasksAlloc, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                for (var s = 0u; s < SplitMasksAlloc; s++) {
                    _visibleInstances[i][s] = new UnsafeArray<int>(renderer.instancesCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                }
                _transformIndices[i] = UnsafeArray<uint>.FromExistingData(transformIndicesFlatPtr, renderer.instancesCount);
                transformIndicesFlatPtr += renderer.instancesCount;

                var uvDistributionCount = renderer.instancesCount * (uint)renderer.renderData.Count;
                _reciprocalUvDistributions[i] = UnsafeArray<float>.FromExistingData(reciprocalUvDistributionsFlatPtr, uvDistributionCount);
                reciprocalUvDistributionsFlatPtr += uvDistributionCount;
            }

            closeReciprocalUvDistributions.Complete();
        }

        BatchMaterialID RegisterMaterial(Material material) {
            return _brg.RegisterMaterial(material);
        }

        BatchMeshID RegisterMesh(Mesh mesh) {
            return _brg.RegisterMesh(mesh);
        }

        void CreateBatch(int count) {
            var batchMetadata = new NativeArray<MetadataValue>(2, ARAlloc.Temp, NativeArrayOptions.UninitializedMemory);

            batchMetadata[0] = CreateMetadataValue(ObjectToWorldID, 0, true); // matrices
            batchMetadata[1] = CreateMetadataValue(WorldToObjectID, count * PackedMatrix.Stride, true); // inverse matrices

            _batchID = _brg.AddBatch(batchMetadata, _graphicsBufferHandle);
            batchMetadata.Dispose();
        }

        static MetadataValue CreateMetadataValue(int nameID, int gpuOffset, bool isPerInstance) {
            const uint IsPerInstanceBit = 0x80000000;
            return new MetadataValue {
                NameID = nameID,
                Value = (uint)gpuOffset | (isPerInstance ? IsPerInstanceBit : 0),
            };
        }

        JobHandle OnPerformCulling(BatchRendererGroup brg, BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput, IntPtr userContext) {
#if UNITY_EDITOR
            m_BatchCullingOutputDebugData.materialMeshRefToVisibleCountMap.Clear();
            if (cullingContext.viewType is BatchCullingViewType.Picking or BatchCullingViewType.SelectionOutline) {
                return default;
            }
#endif
            if ((cullingContext.cullingLayerMask & (1 << FilteringSettings.layer)) == 0) {
                return default;
            }
            if ((cullingContext.sceneCullingMask & FilteringSettings.sceneCullingMask) == 0) {
                return default;
            }

            var performCullingJobHandle = _performCullingJobHandle.Value;
            if (cullingContext.viewType == BatchCullingViewType.Camera) {
                CameraBrgMarker.Begin();
                var lodParams = LODGroupExtensions.CalculateLODParams(cullingContext.lodParameters);

                var frustumJobHandle = new CameraFrustumLodJob {
                    cameraPosition = cullingContext.lodParameters.cameraPosition,
                    distanceScaleSq = lodParams.distanceScale * lodParams.distanceScale,
                    planes = CameraFrustumCullingPlanes(cullingContext.cullingPlanes), // Job will deallocate
                    xs = _xs.AsUnsafeArray(),
                    ys = _ys.AsUnsafeArray(),
                    zs = _zs.AsUnsafeArray(),
                    radii = _radii.AsUnsafeArray(),
                    lodDistancesSq0 = _lodDistancesSq0.AsUnsafeArray(),
                    lodDistancesSq1 = _lodDistancesSq1.AsUnsafeArray(),
                    lodDistancesSq2 = _lodDistancesSq2.AsUnsafeArray(),
                    lodDistancesSq3 = _lodDistancesSq3.AsUnsafeArray(),
                    lodDistancesSq4 = _lodDistancesSq4.AsUnsafeArray(),
                    lodDistancesSq5 = _lodDistancesSq5.AsUnsafeArray(),
                    lodDistancesSq6 = _lodDistancesSq6.AsUnsafeArray(),
                    lodDistancesSq7 = _lodDistancesSq7.AsUnsafeArray(),
                    splitMaskVisibility = _splitVisibilityMask,
                    lodVisibility = _lodVisibility,
                }.Schedule(_radii.LengthInt, 256, performCullingJobHandle);

                var memClearJob = new UnsafeArrayMemClearJob<uint> {
                    array = _instancesCountsFlat,
                }.Schedule(performCullingJobHandle);

                var partOneHandle = JobHandle.CombineDependencies(frustumJobHandle, memClearJob);

                var rendererHandle = new PrepareDrawCommandsPerRenderer {
                    renderDataCounts = _renderDataCounts,
                    lodMasks = _lodMasks,
                    transformIndices = _transformIndices,
                    splitMaskVisibility = _splitVisibilityMask,
                    lodVisibility = _lodVisibility,
                    drawCommands = _drawCommands,
                    visibleInstances = _visibleInstances,
                    instancesCounts = _instancesCounts,
                }.ScheduleParallel(_renderDataCounts.LengthInt, 1, partOneHandle);

                NativeArray<BatchCullingOutputDrawCommands> drawCommand = cullingOutput.drawCommands;
                performCullingJobHandle = new EmitDrawCommands {
                    filterSettings = FilteringSettings,
                    drawCommands = _drawCommands,
                    visibleInstances = _visibleInstances,
                    instancesCount = _instancesCounts,
                    cullingOutputArray = drawCommand,
                }.Schedule(rendererHandle);
                CameraBrgMarker.End();
            } else if (cullingContext.viewType == BatchCullingViewType.Light) {
                LightBrgMarker.Begin();
                CullingUtils.LightCullingSetup(cullingContext, out var receiverSphereCuller, out var frustumPlanes,
                    out var frustumSplits, out var receivers, out var lightFacingFrustumPlanes);

                var frustumJobHandle = new LightFrustumJob {
                    cullingPlanes = frustumPlanes, // Job will deallocate
                    frustumSplits = frustumSplits, // Job will deallocate
                    receiversPlanes = receivers, // Job will deallocate
                    lightFacingFrustumPlanes = lightFacingFrustumPlanes, // Job will deallocate
                    spheresSplitInfos = receiverSphereCuller.splitInfos, // Job will deallocate
                    worldToLightSpaceRotation = receiverSphereCuller.worldToLightSpaceRotation,
                    xs = _xs.AsUnsafeArray(),
                    ys = _ys.AsUnsafeArray(),
                    zs = _zs.AsUnsafeArray(),
                    radii = _radii.AsUnsafeArray(),
                    splitMaskVisibility = _splitVisibilityMask,
                }.Schedule(_radii.LengthInt, 256, performCullingJobHandle);

                var memClearJob = new UnsafeArrayMemClearJob<uint> {
                    array = _instancesCountsFlat,
                }.Schedule(performCullingJobHandle);

                var partOneHandle = JobHandle.CombineDependencies(frustumJobHandle, memClearJob);

                var rendererHandle = new PrepareDrawCommandsPerRenderer {
                    renderDataCounts = _renderDataCounts,
                    lodMasks = _lodMasks,
                    transformIndices = _transformIndices,
                    splitMaskVisibility = _splitVisibilityMask,
                    lodVisibility = _lodVisibility,
                    drawCommands = _drawCommands,
                    visibleInstances = _visibleInstances,
                    instancesCounts = _instancesCounts,
                }.ScheduleParallel(_renderDataCounts.LengthInt, 1, partOneHandle);

                NativeArray<BatchCullingOutputDrawCommands> drawCommand = cullingOutput.drawCommands;
                performCullingJobHandle = new EmitDrawCommands {
                    filterSettings = FilteringSettings,
                    drawCommands = _drawCommands,
                    visibleInstances = _visibleInstances,
                    instancesCount = _instancesCounts,
                    cullingOutputArray = drawCommand,
                }.Schedule(rendererHandle);
                LightBrgMarker.End();
            }
#if UNITY_EDITOR
            if (m_collectBatchCullingOutputDebugData) {
                m_BatchCullingOutputDebugData.FillBatchCullingDebugData(cullingOutput, _brg, performCullingJobHandle);
            }
#endif
            _performCullingJobHandle.Value = performCullingJobHandle;

            return performCullingJobHandle;
        }

        void MipmapsStreamingMasterMaterials.IMipmapsFactorProvider.ProvideMipmapsFactors(in CameraData cameraData, in MipmapsStreamingMasterMaterials.ParallelWriter writer) {
            var mipmapsJobHandle = new MedusaMipmapsFactorJob {
                cameraData = cameraData,
                xs = _xs.AsUnsafeArray(),
                ys = _ys.AsUnsafeArray(),
                zs = _zs.AsUnsafeArray(),
                radii = _radii.AsUnsafeArray(),

                transformIndices = _transformIndices,
                reciprocalUvDistributions = _reciprocalUvDistributions,
                materialIndices = _materialIndices,

                mipmapsStreamingWriter = writer,
            }.ScheduleParallel(_transformIndices.LengthInt, 1, default);
            writer.Dispose(mipmapsJobHandle);
        }

        NativeArray<float4> CameraFrustumCullingPlanes(NativeArray<Plane> cullingPlanes) {
            const int PlanesCount = 6;
            var outputPlanes = new NativeArray<float4>(PlanesCount, Allocator.TempJob);
            for (int i = 0; i < PlanesCount; i++) {
                outputPlanes[i] = new float4(cullingPlanes[i].normal, cullingPlanes[i].distance);
            }
            return outputPlanes;
        }

        [BurstCompile]
        struct CameraFrustumLodJob : IJobParallelForBatch {
            [ReadOnly] public float3 cameraPosition;
            [ReadOnly] public float distanceScaleSq;
            [ReadOnly, DeallocateOnJobCompletion] public NativeArray<float4> planes;

            [ReadOnly] public UnsafeArray<float> xs;
            [ReadOnly] public UnsafeArray<float> ys;
            [ReadOnly] public UnsafeArray<float> zs;
            [ReadOnly] public UnsafeArray<float> radii;
            [ReadOnly] public UnsafeArray<float> lodDistancesSq0;
            [ReadOnly] public UnsafeArray<float> lodDistancesSq1;
            [ReadOnly] public UnsafeArray<float> lodDistancesSq2;
            [ReadOnly] public UnsafeArray<float> lodDistancesSq3;
            [ReadOnly] public UnsafeArray<float> lodDistancesSq4;
            [ReadOnly] public UnsafeArray<float> lodDistancesSq5;
            [ReadOnly] public UnsafeArray<float> lodDistancesSq6;
            [ReadOnly] public UnsafeArray<float> lodDistancesSq7;

            [WriteOnly] public UnsafeArray<ushort> splitMaskVisibility;
            [WriteOnly] public UnsafeArray<byte> lodVisibility;

            public void Execute(int startIndex, int count) {
                var uStartIndex = (uint)startIndex;
                var p0 = planes[0];
                var p1 = planes[1];
                var p2 = planes[2];
                var p3 = planes[3];
                var p4 = planes[4];
                var p5 = planes[5];

                var ones = new int4(1);
                var zeros = new int4(0);

                for (uint i = 0; count - i >= 4; i += 4) {
                    var fullIndex = uStartIndex + i;
                    var simdXs = xs.ReinterpretLoad<float4>(fullIndex);
                    var simdYs = ys.ReinterpretLoad<float4>(fullIndex);
                    var simdZs = zs.ReinterpretLoad<float4>(fullIndex);
                    var simdRadii = radii.ReinterpretLoad<float4>(fullIndex);

                    bool4 frustumMask =
                        p0.x * simdXs + p0.y * simdYs + p0.z * simdZs + p0.w + simdRadii > 0.0f &
                        p1.x * simdXs + p1.y * simdYs + p1.z * simdZs + p1.w + simdRadii > 0.0f &
                        p2.x * simdXs + p2.y * simdYs + p2.z * simdZs + p2.w + simdRadii > 0.0f &
                        p3.x * simdXs + p3.y * simdYs + p3.z * simdZs + p3.w + simdRadii > 0.0f &
                        p4.x * simdXs + p4.y * simdYs + p4.z * simdZs + p4.w + simdRadii > 0.0f &
                        p5.x * simdXs + p5.y * simdYs + p5.z * simdZs + p5.w + simdRadii > 0.0f;

                    var bigSplits = math.select(uint4.zero, new uint4(1), frustumMask);
                    ushort4 splits = default;
                    splits.x = (ushort)bigSplits.x;
                    splits.y = (ushort)bigSplits.y;
                    splits.z = (ushort)bigSplits.z;
                    splits.w = (ushort)bigSplits.w;
                    splitMaskVisibility.ReinterpretStore(fullIndex, splits);

                    var xDiffs = simdXs - cameraPosition.x;
                    var yDiffs = simdYs - cameraPosition.y;
                    var zDiffs = simdZs - cameraPosition.z;

                    xDiffs = xDiffs * xDiffs;
                    yDiffs = yDiffs * yDiffs;
                    zDiffs = zDiffs * zDiffs;

                    var distancesSq = xDiffs + yDiffs + zDiffs;
                    distancesSq *= distanceScaleSq;

                    int4 lodMask = 0;
                    lodMask |= math.select(zeros, ones, distancesSq < lodDistancesSq0.ReinterpretLoad<float4>(fullIndex)) << 0;
                    lodMask |= math.select(zeros, ones, distancesSq < lodDistancesSq1.ReinterpretLoad<float4>(fullIndex)) << 1;
                    lodMask |= math.select(zeros, ones, distancesSq < lodDistancesSq2.ReinterpretLoad<float4>(fullIndex)) << 2;
                    lodMask |= math.select(zeros, ones, distancesSq < lodDistancesSq3.ReinterpretLoad<float4>(fullIndex)) << 3;
                    lodMask |= math.select(zeros, ones, distancesSq < lodDistancesSq4.ReinterpretLoad<float4>(fullIndex)) << 4;
                    lodMask |= math.select(zeros, ones, distancesSq < lodDistancesSq5.ReinterpretLoad<float4>(fullIndex)) << 5;
                    lodMask |= math.select(zeros, ones, distancesSq < lodDistancesSq6.ReinterpretLoad<float4>(fullIndex)) << 6;
                    lodMask |= math.select(zeros, ones, distancesSq < lodDistancesSq7.ReinterpretLoad<float4>(fullIndex)) << 7;

                    var lodFirstOnes = math.tzcnt(lodMask);
                    byte4 finalLodMask = default;
                    finalLodMask.x = (byte)(1 << lodFirstOnes.x);
                    finalLodMask.y = (byte)(1 << lodFirstOnes.y);
                    finalLodMask.z = (byte)(1 << lodFirstOnes.z);
                    finalLodMask.w = (byte)(1 << lodFirstOnes.w);

                    lodVisibility.ReinterpretStore(fullIndex, finalLodMask);
                }

                for (uint i = (uint)count.SimdTrailing(); i < count; ++i) {
                    var fullIndex = uStartIndex + i;
                    var position = new float3(xs[fullIndex], ys[fullIndex], zs[fullIndex]);
                    var r = radii[fullIndex];
                    var frustumVisible =
                        math.dot(p0.xyz, position) + p0.w + r > 0.0f &&
                        math.dot(p1.xyz, position) + p1.w + r > 0.0f &&
                        math.dot(p2.xyz, position) + p2.w + r > 0.0f &&
                        math.dot(p3.xyz, position) + p3.w + r > 0.0f &&
                        math.dot(p4.xyz, position) + p4.w + r > 0.0f &&
                        math.dot(p5.xyz, position) + p5.w + r > 0.0f;

                    splitMaskVisibility[fullIndex] = (ushort)math.select(0, 1, frustumVisible);

                    var distanceSq = math.distancesq(position, cameraPosition) * distanceScaleSq;
                    int lod = 0;
                    lod |= math.select(0, 1, distanceSq < lodDistancesSq0[fullIndex]) << 0;
                    lod |= math.select(0, 1, distanceSq < lodDistancesSq1[fullIndex]) << 1;
                    lod |= math.select(0, 1, distanceSq < lodDistancesSq2[fullIndex]) << 2;
                    lod |= math.select(0, 1, distanceSq < lodDistancesSq3[fullIndex]) << 3;
                    lod |= math.select(0, 1, distanceSq < lodDistancesSq4[fullIndex]) << 4;
                    lod |= math.select(0, 1, distanceSq < lodDistancesSq5[fullIndex]) << 5;
                    lod |= math.select(0, 1, distanceSq < lodDistancesSq6[fullIndex]) << 6;
                    lod |= math.select(0, 1, distanceSq < lodDistancesSq7[fullIndex]) << 7;

                    lod = 1 << math.tzcnt(lod);

                    lodVisibility[fullIndex] = (byte)lod;
                }
            }
        }

        [BurstCompile]
        struct LightFrustumJob : IJobParallelForBatch {
            [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float4> cullingPlanes;
            [DeallocateOnJobCompletion, ReadOnly] public NativeArray<int> frustumSplits;
            [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float4> receiversPlanes;

            [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float4> lightFacingFrustumPlanes;
            [DeallocateOnJobCompletion, ReadOnly] public NativeArray<SphereSplitInfo> spheresSplitInfos;
            [ReadOnly] public float3x3 worldToLightSpaceRotation;

            [ReadOnly] public UnsafeArray<float> xs;
            [ReadOnly] public UnsafeArray<float> ys;
            [ReadOnly] public UnsafeArray<float> zs;
            [ReadOnly] public UnsafeArray<float> radii;

            [WriteOnly] public UnsafeArray<ushort> splitMaskVisibility;

            public void Execute(int startIndex, int count) {
                var uStartIndex = (uint)startIndex;

                for (uint i = 0; count - i >= 4; i += 4) {
                    var fullIndex = uStartIndex + i;
                    var simdXs = xs.ReinterpretLoad<float4>(fullIndex);
                    var simdYs = ys.ReinterpretLoad<float4>(fullIndex);
                    var simdZs = zs.ReinterpretLoad<float4>(fullIndex);
                    var simdRadii = radii.ReinterpretLoad<float4>(fullIndex);

                    CullingUtils.LightSimdCulling(receiversPlanes, frustumSplits, cullingPlanes,
                        worldToLightSpaceRotation, spheresSplitInfos, lightFacingFrustumPlanes,
                        simdXs, simdYs, simdZs, simdRadii,
                        out var mask);

                    ushort4 splits = default;
                    splits.x = (ushort)mask.x;
                    splits.y = (ushort)mask.y;
                    splits.z = (ushort)mask.z;
                    splits.w = (ushort)mask.w;

                    splitMaskVisibility.ReinterpretStore(fullIndex, splits);
                }

                for (uint i = (uint)count.SimdTrailing(); i < count; ++i) {
                    var fullIndex = uStartIndex + i;

                    var position = new float3(xs[fullIndex], ys[fullIndex], zs[fullIndex]);
                    var r = radii[fullIndex];

                    CullingUtils.LightCulling(receiversPlanes, frustumSplits, cullingPlanes,
                        worldToLightSpaceRotation, spheresSplitInfos, lightFacingFrustumPlanes,
                        position, r, out var mask);

                    splitMaskVisibility[fullIndex] = (ushort)mask;
                }
            }
        }

        [BurstCompile]
        struct PrepareDrawCommandsPerRenderer : IJobFor {
            [ReadOnly] public UnsafeArray<uint> renderDataCounts;
            [ReadOnly] public UnsafeArray<byte> lodMasks;
            [ReadOnly] public UnsafeArray<UnsafeArray<uint>.Span> transformIndices;

            [ReadOnly] public UnsafeArray<ushort> splitMaskVisibility;
            [ReadOnly] public UnsafeArray<byte> lodVisibility;

            [WriteOnly] public UnsafeArray<UnsafeArray<UnsafeArray<BatchDrawCommand>>> drawCommands;
            [WriteOnly] public UnsafeArray<UnsafeArray<UnsafeArray<int>>> visibleInstances;
            [WriteOnly] public UnsafeArray<UnsafeArray<uint>.Span> instancesCounts;

            public void Execute(int index) {
                var uIndex = (uint)index;
                var renderDataCount = renderDataCounts[uIndex];
                var lodMask = lodMasks[uIndex];
                var subTransformIndices = transformIndices[uIndex];
                ref var subDrawCommands = ref drawCommands[uIndex];
                ref var subVisibleInstances = ref visibleInstances[uIndex];
                ref var instancesCount = ref instancesCounts[uIndex];

                uint leftoversSplitsMask = 0;

                for (uint i = 0; i < subTransformIndices.Length; i++) {
                    var transformIndex = subTransformIndices[i];
                    var isLodVisible = (lodVisibility[transformIndex] & lodMask) != 0;
                    var splitMask = splitMaskVisibility[transformIndex];
                    var isFrustumVisible = splitMask != 0;
                    var isVisible = isLodVisible & isFrustumVisible;
                    if (!isVisible) {
                        continue;
                    }

                    var splitMaskIndex = math.select(splitMask, 0u, splitMask >= SplitMasksAlloc);
                    if (splitMaskIndex == 0) {
                        leftoversSplitsMask |= splitMask;
                    }

                    ref var targetInstancesCount = ref instancesCount[splitMaskIndex];
                    ref var targetVisibleInstances = ref subVisibleInstances[splitMaskIndex];

                    targetVisibleInstances[targetInstancesCount] = (int)transformIndex;
                    targetInstancesCount += 1;
                }

                for (ushort s = 0; s < SplitMasksAlloc; s++) {
                    var targetInstancesCount = instancesCount[s];
                    ref var targetDrawCommands = ref subDrawCommands[s];
                    for (uint i = 0; i < renderDataCount; i++) {
                        ref var command = ref targetDrawCommands[i];
                        command.visibleCount = targetInstancesCount;
                        command.splitVisibilityMask = (ushort)math.select(s, leftoversSplitsMask, s == 0);
                    }
                }
            }
        }

        [BurstCompile]
        unsafe struct EmitDrawCommands : IJob {
            public BatchFilterSettings filterSettings;
            // -- first length is the same and it is a number of renderers
            [ReadOnly] public UnsafeArray<UnsafeArray<UnsafeArray<BatchDrawCommand>>> drawCommands;
            [ReadOnly] public UnsafeArray<UnsafeArray<UnsafeArray<int>>> visibleInstances;
            [ReadOnly] public UnsafeArray<UnsafeArray<uint>.Span> instancesCount;

            [WriteOnly]
            public NativeArray<BatchCullingOutputDrawCommands> cullingOutputArray;

            public void Execute() {
                var cullingOutput = default(BatchCullingOutputDrawCommands);

                var allInstancesCount = 0u;
                var allDrawCommandsCount = 0u;
                var renderersCount = instancesCount.Length;
                var offsets = stackalloc uint[(int)(renderersCount*SplitMasksAlloc)];

                for (uint i = 0; i < renderersCount; i++) {
                    var rendererInstancesCount = instancesCount[i];
                    for (var s = 0u; s < SplitMasksAlloc; s++) {
                        offsets[i * SplitMasksAlloc + s] = allInstancesCount;
                        allInstancesCount += rendererInstancesCount[s];
                        if (rendererInstancesCount[s] > 0) {
                            allDrawCommandsCount += drawCommands[i][s].Length;
                        }
                    }
                }
                cullingOutput.visibleInstanceCount = (int)allInstancesCount;
                if (allInstancesCount == 0) {
                    return;
                }

                var visibleInstancesOutput = MallocDrawMemory<int>(allInstancesCount);
                var visibleInstancesCopyTarget = visibleInstancesOutput;
                for (var i = 0u; i < renderersCount; i++) {
                    var rendererInstancesCount = instancesCount[i];
                    for (var s = 0u; s < SplitMasksAlloc; s++) {
                        if (rendererInstancesCount[s] == 0) {
                            continue;
                        }
                        var rendererVisibleInstances = visibleInstances[i][s];
                        UnsafeUtility.MemCpy(visibleInstancesCopyTarget, rendererVisibleInstances.Ptr, rendererInstancesCount[s] * sizeof(int));
                        visibleInstancesCopyTarget += rendererInstancesCount[s];
                    }
                }
                cullingOutput.visibleInstances = visibleInstancesOutput;
                cullingOutput.drawCommandCount = (int)allDrawCommandsCount;

                var drawCommandsOutput = MallocDrawMemory<BatchDrawCommand>(allDrawCommandsCount);
                var drawCommandsCopyTarget = drawCommandsOutput;
                for (uint i = 0; i < renderersCount; i++) {
                    var rendererInstancesCount = instancesCount[i];
                    for (var s = 0u; s < SplitMasksAlloc; s++) {
                        if (rendererInstancesCount[s] == 0) {
                            continue;
                        }
                        var rendererDrawCommands = drawCommands[i][s];
                        for (uint j = 0; j < rendererDrawCommands.Length; j++) {
                            var drawCommand = rendererDrawCommands[j];
                            drawCommand.visibleOffset = offsets[i * SplitMasksAlloc + s];
                            drawCommand.visibleCount = rendererInstancesCount[s];
                            *drawCommandsCopyTarget = drawCommand;
                            drawCommandsCopyTarget++;
                        }
                    }
                }
                cullingOutput.drawCommands = drawCommandsOutput;

                cullingOutput.drawRangeCount = 1;

                var drawRanges = MallocDrawMemory<BatchDrawRange>(1);
                *drawRanges = new BatchDrawRange {
                    drawCommandsBegin = 0,
                    drawCommandsCount = allDrawCommandsCount,
                    filterSettings = filterSettings,
                };
                cullingOutput.drawRanges = drawRanges;

                cullingOutput.instanceSortingPositionFloatCount = 0;
                cullingOutput.instanceSortingPositions = null;

                cullingOutputArray[0] = cullingOutput;
            }

            static T* MallocDrawMemory<T>(uint count) where T : unmanaged {
                return (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * count, UnsafeUtility.AlignOf<T>(), Allocator.TempJob);
            }
        }

        [BurstCompile]
        struct MedusaMipmapsFactorJob : IJobFor {
            public CameraData cameraData;
            [ReadOnly] public UnsafeArray<float> xs;
            [ReadOnly] public UnsafeArray<float> ys;
            [ReadOnly] public UnsafeArray<float> zs;
            [ReadOnly] public UnsafeArray<float> radii;

            [ReadOnly] public UnsafeArray<UnsafeArray<uint>.Span> transformIndices;

            [ReadOnly] public UnsafeArray<UnsafeArray<float>.Span> reciprocalUvDistributions;
            [ReadOnly] public UnsafeArray<UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>> materialIndices;

            public MipmapsStreamingMasterMaterials.ParallelWriter mipmapsStreamingWriter;

            public void Execute(int index) {
                var uIndex = (uint)index;
                var subTransformIndices = transformIndices[uIndex];
                var subReciprocalUvDistributions = reciprocalUvDistributions[uIndex].AsUnsafeArray();
                var subMaterialIndices = materialIndices[uIndex];

                var uvDistributionIndex = 0u;
                for (var i = 0u; subTransformIndices.Length - i >= 4; i += 4) {
                    var transformIndex = subTransformIndices[i];
                    var simdXs = xs.ReinterpretLoad<float4>(transformIndex);
                    var simdYs = ys.ReinterpretLoad<float4>(transformIndex);
                    var simdZs = zs.ReinterpretLoad<float4>(transformIndex);
                    var simdRadii = radii.ReinterpretLoad<float4>(transformIndex);

                    var factorFactor = MipmapsStreamingUtils.CalculateMipmapFactorFactorSimd(cameraData,
                        simdXs, simdYs, simdZs, simdRadii);

                    for (uint j = 0; j < subMaterialIndices.Length; j++) {
                        var simdUvDist = subReciprocalUvDistributions.ReinterpretLoad<float4>(uvDistributionIndex);
                        uvDistributionIndex += 4;
                        var factor = math.cmin(factorFactor * simdUvDist);
                        mipmapsStreamingWriter.UpdateMipFactor(subMaterialIndices[j], factor);
                    }
                }

                for (var i = subTransformIndices.Length.SimdTrailing(); i < subTransformIndices.Length; ++i) {
                    var transformIndex = subTransformIndices[i];
                    var position = new float3(xs[transformIndex], ys[transformIndex], zs[transformIndex]);
                    var radius = radii[transformIndex];

                    var factorFactor = MipmapsStreamingUtils.CalculateMipmapFactorFactor(cameraData, position, radius);

                    for (uint j = 0; j < subMaterialIndices.Length; j++) {
                        var factor = factorFactor * subReciprocalUvDistributions[uvDistributionIndex++];
                        mipmapsStreamingWriter.UpdateMipFactor(subMaterialIndices[j], factor);
                    }
                }
            }
        }

        // === MemorySnapshot
        public unsafe int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 6;
            ownPlace.Span[0] = new MemorySnapshot("BRG", 0, 0, memoryBuffer[..childrenCount]);

            var transformsSize = _transformsBuffer.Length * sizeof(byte);
            memoryBuffer.Span[0] = new MemorySnapshot("Transforms", transformsSize, default);

            var gpuSize = (_graphicsBuffer?.count ?? 0) * (_graphicsBuffer?.stride ?? 0);
            memoryBuffer.Span[1] = new MemorySnapshot("GPUBuffer", gpuSize, default);

            var lodVisibilitySize = _lodVisibility.Length * sizeof(byte);
            var splitVisibilityMaskSize = _splitVisibilityMask.Length * sizeof(ushort);
            var visibilitySize = lodVisibilitySize + splitVisibilityMaskSize;
            memoryBuffer.Span[2] = new MemorySnapshot("Visibility", visibilitySize, default);

            var renderDataCountsSize = _renderDataCounts.Length * sizeof(uint);
            var lodMasksSize = _lodMasks.Length * sizeof(byte);
            var drawCommandsSize = 0L;
            foreach (var drawCommandsRenderer in _drawCommands) {
                foreach (var drawCommands in drawCommandsRenderer) {
                    drawCommandsSize += drawCommands.Length * sizeof(BatchDrawCommand);
                }
            }
            var reciprocalUvDistributionsSize = _reciprocalUvDistributionsFlat.Length * sizeof(float);
            var materialIndicesSize = 0L;
            foreach (var materialIndices in _materialIndices) {
                materialIndicesSize += materialIndices.Length * sizeof(MipmapsStreamingMasterMaterials.MaterialId);
            }
            var instancesCountsSize = _instancesCountsFlat.Length * sizeof(uint);
            var rendererDefinitionSize = renderDataCountsSize +
                                         lodMasksSize +
                                         drawCommandsSize +
                                         reciprocalUvDistributionsSize +
                                         materialIndicesSize +
                                         instancesCountsSize;
            memoryBuffer.Span[3] = new MemorySnapshot("RendererDefinition", rendererDefinitionSize, default);

            var visibleInstancesSize = 0L;
            foreach (var visibleInstances in _visibleInstances) {
                foreach (var visibleInstancesSplit in visibleInstances) {
                    visibleInstancesSize += visibleInstancesSplit.Length * sizeof(int);
                }
            }
            memoryBuffer.Span[4] = new MemorySnapshot("Renderers visible", visibleInstancesSize, default);

            var transformIndicesSize = _transformIndicesFlat.Length * sizeof(uint);
            memoryBuffer.Span[5] = new MemorySnapshot("Renderers transforms", transformIndicesSize, default);

            return childrenCount;
        }

        public readonly struct EditorAccess {
            readonly MedusaBrgRenderer _renderer;
            public bool IsNull => _renderer == null;
            public bool IsNotNull => _renderer != null;
            public bool IsValid => _renderer?._brg != null;
            public UnsafeArray<float>.Span Xs => _renderer._xs;
            public UnsafeArray<float>.Span Ys => _renderer._ys;
            public UnsafeArray<float>.Span Zs => _renderer._zs;
            public UnsafeArray<float>.Span Radii => _renderer._radii;
            public UnsafeArray<byte>.Span LastLodMasks => _renderer._lastLodMasks;
            public UnsafeArray<byte> LodVisibility => _renderer._lodVisibility;
            public UnsafeArray<ushort> SplitVisibilityMask => _renderer._splitVisibilityMask;
            public string BasePath => _renderer._medusaBasePath;

#if UNITY_EDITOR
            public BatchCullingOutputDebugData BatchCullingOutputDebugData => _renderer.m_BatchCullingOutputDebugData;
            public bool CollectBatchCullingOutputDebugData {
                get => _renderer.m_collectBatchCullingOutputDebugData;
                set => _renderer.m_collectBatchCullingOutputDebugData = value;
            }
#endif
            public EditorAccess(MedusaBrgRenderer renderer) {
                _renderer = renderer;
            }

            public float LodDistanceSq(uint transform, byte lod) {
                return lod switch {
                    0 => _renderer._lodDistancesSq0[transform],
                    1 => _renderer._lodDistancesSq1[transform],
                    2 => _renderer._lodDistancesSq2[transform],
                    3 => _renderer._lodDistancesSq3[transform],
                    4 => _renderer._lodDistancesSq4[transform],
                    5 => _renderer._lodDistancesSq5[transform],
                    6 => _renderer._lodDistancesSq6[transform],
                    7 => _renderer._lodDistancesSq7[transform],
                    _ => 0
                };
            }

            public UnsafeArray<uint>.Span TransformIndices(uint renderer) {
                return _renderer._transformIndices[renderer];
            }
        }
    }

    [Serializable]
    public struct Renderer : IEquatable<Renderer> {
        // It's array but in order to reduce allocations for comparing it's better to use List
        // For more context see MedusaRendererManagerBaker
        public List<RenderDatum> renderData;
        public byte lodMask;
        // Should be moved out to another struct
        public uint instancesCount;

        public bool Equals(Renderer other) {
            return Equals(renderData, other.renderData) && lodMask == other.lodMask;
        }

        public override bool Equals(object obj) {
            return obj is Renderer other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = lodMask.GetHashCode();
                if (renderData != null) {
                    foreach (var datum in renderData) {
                        hashCode = (hashCode * 397) ^ datum.GetHashCode();
                    }
                }
                return hashCode;
            }
        }

        public static bool operator ==(Renderer left, Renderer right) {
            return left.Equals(right);
        }

        public static bool operator !=(Renderer left, Renderer right) {
            return !left.Equals(right);
        }

        static bool Equals(List<RenderDatum> l, List<RenderDatum> r) {
            if (l.Count != r.Count) {
                return false;
            }
            for (int i = 0; i < l.Count; i++) {
                if (l[i] != r[i]) {
                    return false;
                }
            }
            return true;
        }
    }

    [Serializable]
    public struct RenderDatum : IEquatable<RenderDatum> {
        public Mesh mesh;
        public Material material;
        public ushort subMeshIndex;

        public bool Equals(RenderDatum other) {
            return Equals(mesh, other.mesh) && Equals(material, other.material) && subMeshIndex == other.subMeshIndex;
        }
        public override bool Equals(object obj) {
            return obj is RenderDatum other && Equals(other);
        }
        public override int GetHashCode() {
            unchecked {
                int hashCode = (mesh != null ? mesh.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (material != null ? material.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ subMeshIndex.GetHashCode();
                return hashCode;
            }
        }
        public static bool operator ==(RenderDatum left, RenderDatum right) {
            return left.Equals(right);
        }
        public static bool operator !=(RenderDatum left, RenderDatum right) {
            return !left.Equals(right);
        }
    }
}
