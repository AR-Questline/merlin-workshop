#if UNITY_EDITOR && !DISABLE_HYBRID_RENDERER_PICKING
#define ENABLE_PICKING
#endif
//#define DISABLE_BATCHING

using System;
using System.Text;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Graphics;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UniversalProfiling;

namespace Awaken.Kandra.Managers {
    public unsafe class SkinnedBatchRenderGroup : IMemorySnapshotProvider {
        const ushort MaxSplits = 6;
        const ushort MaxSplitMasksCount = (1 << MaxSplits);
        const float RandomBigNumber = 1048576.0f;

        static readonly int UintInstanceDataSize = sizeof(InstanceData) / sizeof(uint);
        static readonly int InstancesDataStart = (sizeof(PackedMatrix) / sizeof(uint)) * 2;
        static readonly UniversalProfilerMarker CameraPerformCullingMarker = new UniversalProfilerMarker("SkinnedBatchRenderGroup.CameraPerformCulling");
        static readonly UniversalProfilerMarker LightPerformCullingMarker = new UniversalProfilerMarker("SkinnedBatchRenderGroup.LightPerformCulling");
#if ENABLE_PICKING
        static readonly ProfilerMarker PickingPerformCullingMarker = new ProfilerMarker("SkinnedBatchRenderGroup.PickingPerformCulling");
#endif

        static readonly int ObjectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        static readonly int ObjectToWorldPreviousID = Shader.PropertyToID("unity_MatrixPreviousM");
        static readonly int WorldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        static readonly int InstanceDataID = Shader.PropertyToID("_InstanceData");

        public UnsafeArray<ushort> cameraSplitMaskVisibility;
        public UnsafeArray<ushort> lightsSplitMaskVisibility;
        public UnsafeArray<ushort> lightsAggregatedSplitMaskVisibility;

        public bool enabled = true;

        VisibilityCullingManager _visibilityCullingManager;

        BatchRendererGroup _brg;
        GraphicsBuffer _instanceBuffer;
        BatchID _batchID;

        ForFrameValue<JobHandle> _performCullingJobHandle;

        UnsafeArray<UnsafeArray<ushort>> _rendererBySlot;
        ARUnsafeList<RendererInstancesData> _instances;
        ARUnsafeList<SubmeshData> _renderers;
        ARUnsafeList<FilterSettings> _filterSettings;
        
        public SkinnedBatchRenderGroup(VisibilityCullingManager visibilityCullingManager) {
            var maxRenderers = (uint)KandraRendererManager.FinalRenderersCapacity;

            _visibilityCullingManager = visibilityCullingManager;

            _brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
            var bounds = new Bounds(Vector3.zero, RandomBigNumber.UniformVector3());
            _brg.SetGlobalBounds(bounds);
            _brg.SetEnabledViewTypes(new[] {
                BatchCullingViewType.Camera,
                BatchCullingViewType.Light,
#if ENABLE_PICKING
                BatchCullingViewType.Picking,
                BatchCullingViewType.SelectionOutline,
#endif
            });

            var matricesUintSize = InstancesDataStart;
            var instancesUintSize = (sizeof(InstanceData) / sizeof(uint)) * maxRenderers;
            var instancesBufferSize = matricesUintSize + instancesUintSize;
            _instanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)instancesBufferSize, sizeof(uint));

            var metadata = new NativeArray<MetadataValue>(4, ARAlloc.Temp);
            var offset = 0;
            metadata[0] = CreateMetadataValue(ObjectToWorldID, offset, false);
            metadata[1] = CreateMetadataValue(ObjectToWorldPreviousID, offset, false);
            offset += sizeof(PackedMatrix);
            metadata[2] = CreateMetadataValue(WorldToObjectID, offset, false);
            offset += sizeof(PackedMatrix);
            metadata[3] = CreateMetadataValue(InstanceDataID, offset, true);

            _batchID = _brg.AddBatch(metadata, _instanceBuffer.bufferHandle);

            metadata.Dispose();

            var matrix = Matrix4x4.identity;
            var inverse = matrix.inverse;
            var packed = new PackedMatrix(matrix);
            var packedInverse = new PackedMatrix(inverse);

            _instanceBuffer.SetData(new[] { packed, packedInverse });

            _rendererBySlot = new UnsafeArray<UnsafeArray<ushort>>(maxRenderers, ARAlloc.Persistent);
            _instances = new ARUnsafeList<RendererInstancesData>((int)maxRenderers, ARAlloc.Persistent);
            _renderers = new ARUnsafeList<SubmeshData>((int)maxRenderers, ARAlloc.Persistent);
            _filterSettings = new ARUnsafeList<FilterSettings>(8, ARAlloc.Persistent);

            cameraSplitMaskVisibility = new UnsafeArray<ushort>(maxRenderers, ARAlloc.Persistent);
            lightsSplitMaskVisibility = new UnsafeArray<ushort>(maxRenderers, ARAlloc.Persistent);
            lightsAggregatedSplitMaskVisibility = new UnsafeArray<ushort>(maxRenderers, ARAlloc.Persistent);
        }

        public void Dispose() {
            _brg.Dispose();
            _instanceBuffer.Dispose();

            _rendererBySlot.Dispose();
            foreach (var instances in _instances) {
                instances.Dispose();
            }
            _instances.Dispose();

            _renderers.Dispose();

            _filterSettings.Dispose();
            cameraSplitMaskVisibility.Dispose();
            lightsSplitMaskVisibility.Dispose();
            lightsAggregatedSplitMaskVisibility.Dispose();
        }

        public void Register(uint slot, KandraRenderingMesh renderingMesh, Material[] materials, uint instanceStartVertex, uint sharedStartVertex, in FilterSettings filterSettings) {
            var renderingCount = math.min((uint)materials.Length, renderingMesh.submeshes.Length);
            var rendererIndices = new UnsafeArray<ushort>(renderingCount, ARAlloc.Persistent);
            for (var i = 0u; i < renderingCount; i++) {
                var material = materials[i];
                var isTransparent = ShadersUtils.IsMaterialTransparent(material);
                rendererIndices[i] = RegisterRenderer(slot, renderingMesh, (byte)i, material, isTransparent, filterSettings);
            }

            _rendererBySlot[slot] = rendererIndices;

            var verticesStartArray = new NativeArray<InstanceData>(1, ARAlloc.Temp, NativeArrayOptions.UninitializedMemory);
            verticesStartArray[0] = new InstanceData {
                instanceStartVertex = instanceStartVertex,
                sharedStartVertex = sharedStartVertex,
            };
            var uArray = verticesStartArray.Reinterpret<uint>(sizeof(InstanceData));
            _instanceBuffer.SetData(uArray, 0, (int)(InstancesDataStart + (slot * UintInstanceDataSize)), uArray.Length);
            verticesStartArray.Dispose();
        }

        public void Unregister(uint slot) {
            ref var rendererIndices = ref _rendererBySlot[slot];
            if (!rendererIndices.IsCreated) {
                Log.Important?.Error($"Kandra Renderer: Trying to unregister slot {slot} that is not registered");
                return;
            }

            for (var i = 0u; i < rendererIndices.Length; i++) {
                UnregisterRenderer(slot, rendererIndices[i]);
            }

            rendererIndices.Dispose();
            _rendererBySlot[slot] = default;
        }

        public void UpdateSubmeshIndices(uint slot, KandraRenderingMesh renderingMesh) {
            var rendererIndices = _rendererBySlot[slot];
            if (!rendererIndices.IsCreated) {
                Log.Important?.Error($"Kandra Renderer: Trying to update mesh for slot {slot} that is not registered");
                return;
            }

            if (rendererIndices.Length != renderingMesh.submeshes.Length) {
                Log.Important?.Error($"Kandra Renderer: Trying to update mesh for slot {slot} with different submesh count");
                return;
            }

            for (var i = 0u; i < rendererIndices.Length; ++i) {
                var rendererIndex = rendererIndices[i];
                var submesh = _renderers[rendererIndex];
                submesh = submesh.WithMesh(renderingMesh);

                rendererIndex = RendererUpdated(slot, rendererIndex, submesh);
                rendererIndices[i] = rendererIndex;
            }
        }

        public void UpdateMaterials(uint slot, Material[] materials, KandraRenderingMesh renderingMesh, in FilterSettings filterSettings) {
            ref var rendererIndices = ref _rendererBySlot[slot];
            if (!rendererIndices.IsCreated) {
                Log.Important?.Error($"Kandra Renderer: Trying to update materials for slot {slot} that is not registered");
                return;
            }

            var oldRenderingCount = rendererIndices.Length;
            var renderingCount = math.min((uint)materials.Length, renderingMesh.submeshes.Length);
            for (var i = renderingCount; i < oldRenderingCount; i++) {
                UnregisterRenderer(slot, rendererIndices[i]);
            }

            if (renderingCount != oldRenderingCount) {
                rendererIndices.Resize(renderingCount);
            }

            for (var i = 0u; i < oldRenderingCount; i++) {
                var rendererIndex = rendererIndices[i];
                ref readonly var submesh = ref _renderers.Ptr[rendererIndex];

#if UNITY_EDITOR
                if (_brg.GetRegisteredMaterial(submesh.materialID))
#endif
                {
                    _brg.UnregisterMaterial(submesh.materialID);
                }

                var material = materials[i];
                var isTransparent = ShadersUtils.IsMaterialTransparent(material);

                var filterSettingsId = submesh.filterSettingsId;
                var oldFilterSettings = _filterSettings[filterSettingsId];
                var newFilterSettings = filterSettings.WithTransparency(isTransparent);
                if (oldFilterSettings.Equals(newFilterSettings) == false) {
                    var findFilterSettingsId = _filterSettings.FindIndexOf(newFilterSettings);
                    if (findFilterSettingsId == -1) {
                        filterSettingsId = (ushort)_filterSettings.Length;
                        _filterSettings.Add(newFilterSettings);
                    } else {
                        filterSettingsId = (ushort)findFilterSettingsId;
                    }
                }

                var materialId = _brg.RegisterMaterial(material);
                var newSubmesh = submesh.WithMaterial(materialId, filterSettingsId);
                rendererIndex = RendererUpdated(slot, rendererIndex, newSubmesh);
                rendererIndices[i] = rendererIndex;
            }

            for (var i = oldRenderingCount; i < renderingCount; i++) {
                var material = materials[i];
                var isTransparent = ShadersUtils.IsMaterialTransparent(material);
                rendererIndices[i] = RegisterRenderer(slot, renderingMesh, (byte)i, material, isTransparent, filterSettings);
            }

            _rendererBySlot[slot] = rendererIndices;
        }

        public void UpdateFilterSettings(uint slot, in FilterSettings filterSettings) {
            ref var rendererIndices = ref _rendererBySlot[slot];
            if (!rendererIndices.IsCreated) {
                Log.Important?.Error($"Kandra Renderer: Trying to update filter settings for slot {slot} that is not registered");
                return;
            }

            for (var i = 0u; i < rendererIndices.Length; ++i) {
                var rendererIndex = rendererIndices[i];
                ref var submesh = ref _renderers.Ptr[rendererIndex];
                var oldFilterSettings = _filterSettings[submesh.filterSettingsId];
                var newFilterSettings = filterSettings.WithTransparency(oldFilterSettings.hasTransparency);
                if (oldFilterSettings.Equals(newFilterSettings)) {
                    continue;
                }

                var filterSettingsId = _filterSettings.FindIndexOf(newFilterSettings);
                if (filterSettingsId == -1) {
                    filterSettingsId = _filterSettings.Length;
                    _filterSettings.Add(newFilterSettings);
                }

                submesh = submesh.WithFilterSettingsId((ushort)filterSettingsId);

                rendererIndex = RendererUpdated(slot, rendererIndex, submesh);
                rendererIndices[i] = rendererIndex;
            }
        }

        ushort RegisterRenderer(uint slot, KandraRenderingMesh renderingMesh, byte submeshIndex, Material material, bool isTransparent, in FilterSettings filterSettings) {
            var targetFilterSettings = filterSettings.WithTransparency(isTransparent);
            var filterSettingsId = _filterSettings.FindIndexOf(targetFilterSettings);
            if (filterSettingsId == -1) {
                filterSettingsId = _filterSettings.Length;
                _filterSettings.Add(targetFilterSettings);
            }

            var materialId = _brg.RegisterMaterial(material);
            var indexStart = renderingMesh.IndexStart(submeshIndex);
            var indexCount = renderingMesh.IndexCount(submeshIndex);
            var submesh = new SubmeshData(indexCount, indexStart, materialId, (ushort)filterSettingsId, submeshIndex);

            var rendererIndex = _renderers.FindIndexOf(submesh);

            if (rendererIndex == -1) {
                var instancesData = new RendererInstancesData(slot, ARAlloc.Persistent);
                rendererIndex = _renderers.FindIndexOf(default(SubmeshData));
                if (rendererIndex == -1) {
                    rendererIndex = _renderers.Length;
                    var renderer = submesh.WithInstancesIndex((ushort)rendererIndex);
                    _renderers.Add(renderer);
                    _instances.Add(instancesData);
                } else {
                    var renderer = submesh.WithInstancesIndex((ushort)rendererIndex);
                    _renderers[rendererIndex] = renderer;
                    _instances[rendererIndex] = instancesData;
                }
            } else {
                ref var instancesData = ref _instances.Ptr[rendererIndex];
                instancesData.Add(slot);
            }

            return (ushort)rendererIndex;
        }

        void UnregisterRenderer(uint slot, ushort rendererIndex) {
            var renderer = _renderers[rendererIndex];

#if UNITY_EDITOR
            if (_brg.GetRegisteredMaterial(renderer.materialID))
#endif
            {
                _brg.UnregisterMaterial(renderer.materialID);
            }

            ref var instancesData = ref _instances.Ptr[renderer.instancesIndex];
            ref var slots = ref instancesData.takenSlots;
            var slotIndex = slots.IndexOf(slot);
            slots.RemoveAt(slotIndex);
            if (slots.Length == 0) {
                instancesData.Dispose();
                _instances[renderer.instancesIndex] = default;
                _renderers[rendererIndex] = default;
            }
        }

        ushort RendererUpdated(uint slot, ushort rendererIndex, in SubmeshData submesh) {
            ref var instancesData = ref _instances.Ptr[rendererIndex];
            if (instancesData.RefCount == 1) { // Last renderer from that archetype
                _renderers[rendererIndex] = submesh;
            } else { // Multiple renderers from that archetype
                ref var slots = ref instancesData.takenSlots;
                var slotIndex = slots.IndexOf(slot);
                slots.RemoveAt(slotIndex);

                var archetypeIndex = _renderers.FindIndexOf(submesh);
                if (archetypeIndex == -1) { // New renderer is brand new
                    var localSubmesh = submesh;
                    var freeIndex = _renderers.FindIndexOf(default(SubmeshData));
                    if (freeIndex == -1) { // There is hole in list
                        rendererIndex = (ushort)_renderers.Length;
                        localSubmesh = localSubmesh.WithInstancesIndex(rendererIndex);
                        _renderers.Add(localSubmesh);
                        var newInstancesData = new RendererInstancesData(slot, ARAlloc.Persistent);
                        _instances.Add(newInstancesData);
                    } else {
                        rendererIndex = (ushort)freeIndex;
                        localSubmesh = localSubmesh.WithInstancesIndex(rendererIndex);
                        _renderers[rendererIndex] = localSubmesh;
                        _instances.Ptr[rendererIndex] = new RendererInstancesData(slot, ARAlloc.Persistent);
                    }
                } else { // New renderer archetype already exists
                    rendererIndex = (ushort)archetypeIndex;
                    _instances.Ptr[rendererIndex].Add(slot);
                }
            }
            return rendererIndex;
        }

        // === BRG
        JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext) {
            if (enabled == false) {
                return default;
            }
            
            if ((cullingContext.cullingLayerMask & _visibilityCullingManager.PossibleLayers) == 0) {
                return default;
            }

#if UNITY_EDITOR
            if ((cullingContext.sceneCullingMask & _visibilityCullingManager.PossibleSceneCullingLayers) == 0) {
                return default;
            }
#endif

            var performCullingJobHandle = _performCullingJobHandle.Value;
            var takenLength = KandraRendererManager.Instance.FullyRegisteredRenderersLength;

            if (cullingContext.viewType is BatchCullingViewType.Camera) {
                if (takenLength > 0) {
                    CameraPerformCullingMarker.Begin();
                    performCullingJobHandle = JobHandle.CombineDependencies(performCullingJobHandle, _visibilityCullingManager.collectCullingDataJobHandle);
#if UNITY_EDITOR
                    //if (UnityEditor.SceneView.currentDrawingSceneView == null)
#endif
                    {
                        performCullingJobHandle = new CameraFrustumJob {
                            planes = CameraFrustumCullingPlanes(cullingContext.cullingPlanes), // Job will deallocate
                            renderingLayerMask = cullingContext.cullingLayerMask,
#if UNITY_EDITOR
                            sceneCullingMask = cullingContext.sceneCullingMask,
#endif

                            takenSlots = KandraRendererManager.Instance.FullyRegisteredSlots,
                            toUnregisterSlots = KandraRendererManager.Instance.ToUnregister,

                            xs = _visibilityCullingManager.xs,
                            ys = _visibilityCullingManager.ys,
                            zs = _visibilityCullingManager.zs,
                            radii = _visibilityCullingManager.radii,
                            layersMasks = _visibilityCullingManager.layerMasks,
#if UNITY_EDITOR
                            sceneCullingMasks = _visibilityCullingManager.sceneCullingMasks,
#endif

                            outSplitMaskVisibility = cameraSplitMaskVisibility,
                        }.Schedule(takenLength, 256, performCullingJobHandle);
                    }

                    var triagedRenderers = new NativeList<SubmeshWithSplit>(_renderers.Length, ARAlloc.TempJob);
                    var triagedInstances = new NativeList<RendererInstancesData>(_instances.Length, ARAlloc.TempJob);
                    // TODO: Just filter so we can make faster implementation of triage
                    performCullingJobHandle = new TriageRenderersJob {
                        splitMaskVisibility = cameraSplitMaskVisibility,
                        submeshes = _renderers.AsUnsafeSpan(),
                        instances = _instances.AsUnsafeSpan(),

                        outInstances = triagedInstances,
                        outRegisteredRenderers = triagedRenderers,
                    }.Schedule(_renderers.Length, 8, performCullingJobHandle);

                    performCullingJobHandle = new EmitDrawCommandsJob {
                        batchID = _batchID,
                        indexBuffer = KandraRendererManager.Instance.MeshBroker.IndicesBufferHandle,
                        filterSettings = _filterSettings.AsUnsafeSpan(),
                        submeshesWithSplits = triagedRenderers,
                        instances = triagedInstances,
                        xs = _visibilityCullingManager.xs,
                        ys = _visibilityCullingManager.ys,
                        zs = _visibilityCullingManager.zs,

                        outDrawCommandsOutput = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr(),
                    }.Schedule(performCullingJobHandle);

                    var disposeRenderersHandle = triagedRenderers.Dispose(performCullingJobHandle);
                    var disposeInstancesHandle = triagedInstances.Dispose(performCullingJobHandle);

                    performCullingJobHandle = JobHandle.CombineDependencies(disposeRenderersHandle, disposeInstancesHandle);

                    CameraPerformCullingMarker.End();
                } else {
                    _visibilityCullingManager.collectCullingDataJobHandle.Complete();
                }
            } else if (cullingContext.viewType is BatchCullingViewType.Light) {
                if (takenLength > 0) {
                    LightPerformCullingMarker.Begin();

                    CullingUtils.LightCullingSetup(cullingContext, out var receiverSphereCuller, out var frustumPlanes,
                        out var frustumSplits, out var receivers, out var lightFacingFrustumPlanes);

                    performCullingJobHandle = new LightFrustumJob {
                        renderingLayerMask = cullingContext.cullingLayerMask,
#if UNITY_EDITOR
                        sceneCullingMask = cullingContext.sceneCullingMask,
#endif

                        takenSlots = KandraRendererManager.Instance.FullyRegisteredSlots,
                        toUnregisterSlots = KandraRendererManager.Instance.ToUnregister,

                        cullingPlanes = frustumPlanes, // Job will deallocate
                        frustumSplits = frustumSplits, // Job will deallocate
                        receiversPlanes = receivers, // Job will deallocate
                        lightFacingFrustumPlanes = lightFacingFrustumPlanes, // Job will deallocate
                        spheresSplitInfos = receiverSphereCuller.splitInfos, // Job will deallocate
                        worldToLightSpaceRotation = receiverSphereCuller.worldToLightSpaceRotation,
                        xs = _visibilityCullingManager.xs,
                        ys = _visibilityCullingManager.ys,
                        zs = _visibilityCullingManager.zs,
                        radii = _visibilityCullingManager.radii,
                        layersMasks = _visibilityCullingManager.layerMasks,
#if UNITY_EDITOR
                        sceneCullingMasks = _visibilityCullingManager.sceneCullingMasks,
#endif

                        outSplitMaskVisibility = lightsSplitMaskVisibility,
                        outAggregatedSplitMaskVisibility = lightsAggregatedSplitMaskVisibility,
                    }.Schedule(takenLength, 256, performCullingJobHandle);

                    var triagedRenderers = new NativeList<SubmeshWithSplit>(_renderers.Length*MaxSplitMasksCount, ARAlloc.TempJob);
                    var triagedInstances = new NativeList<RendererInstancesData>(_instances.Length*MaxSplitMasksCount, ARAlloc.TempJob);

                    performCullingJobHandle = new TriageRenderersJob {
                        splitMaskVisibility = lightsSplitMaskVisibility,
                        submeshes = _renderers.AsUnsafeSpan(),
                        instances = _instances.AsUnsafeSpan(),

                        outInstances = triagedInstances,
                        outRegisteredRenderers = triagedRenderers,
                    }.Schedule(_renderers.Length, 16, performCullingJobHandle);

                    performCullingJobHandle = new EmitDrawCommandsJob {
                        batchID = _batchID,
                        indexBuffer = KandraRendererManager.Instance.MeshBroker.IndicesBufferHandle,
                        filterSettings = _filterSettings.AsUnsafeSpan(),
                        submeshesWithSplits = triagedRenderers,
                        instances = triagedInstances,
                        xs = _visibilityCullingManager.xs,
                        ys = _visibilityCullingManager.ys,
                        zs = _visibilityCullingManager.zs,

                        outDrawCommandsOutput = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr(),
                    }.Schedule(performCullingJobHandle);

                    var disposeRenderersHandle = triagedRenderers.Dispose(performCullingJobHandle);
                    var disposeInstancesHandle = triagedInstances.Dispose(performCullingJobHandle);

                    performCullingJobHandle = JobHandle.CombineDependencies(disposeRenderersHandle, disposeInstancesHandle);

                    LightPerformCullingMarker.End();
                }
            } 
#if ENABLE_PICKING
            else if (cullingContext.viewType is BatchCullingViewType.Picking or BatchCullingViewType.SelectionOutline) {
                if (takenLength > 0) {
                    PickingPerformCullingMarker.Begin();

                    var drawData = new NativeList<PickingDrawData>((int)KandraRendererManager.Instance.RegisteredRenderers, Allocator.TempJob);
                    var drawCommandsCount = Malloc<uint>(1);
                    *drawCommandsCount = 0;
                    
                    performCullingJobHandle = new TriageForPickingJob {
                        splitMaskVisibility = cameraSplitMaskVisibility,
                        submeshes = _renderers.AsUnsafeSpan(),
                        instances = _instances.AsUnsafeSpan(),
                        rendererInstanceIDs = KandraRendererManager.Instance.EditorRendererInstanceIds,
                        outDrawData = drawData,
                        outDrawCommandsCount = drawCommandsCount,
                    }.Schedule(performCullingJobHandle);

                    performCullingJobHandle = new EmmitDrawCommandsForPicking {
                        batchID = _batchID,
                        indexBuffer = KandraRendererManager.Instance.MeshBroker.IndicesBufferHandle,
                        drawData = drawData,
                        submeshes = _renderers.AsUnsafeSpan(),
                        drawCommandsCount = drawCommandsCount,
                        outDrawCommandsOutput = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr(),
                    }.Schedule(performCullingJobHandle);

                    var disposeDrawDataHandle = drawData.Dispose(performCullingJobHandle);
                    var freeDrawCommandsCountHandle = MemFreeJob.Schedule(drawCommandsCount, Allocator.TempJob, performCullingJobHandle);
                    
                    performCullingJobHandle = JobHandle.CombineDependencies(disposeDrawDataHandle, freeDrawCommandsCountHandle);
                    
                    PickingPerformCullingMarker.End();
                }
            }
#endif

            _performCullingJobHandle.Value = performCullingJobHandle;

            return performCullingJobHandle;
        }

        NativeArray<float4> CameraFrustumCullingPlanes(NativeArray<Plane> cullingPlanes) {
            const int PlanesCount = 6;
            var outputPlanes = new NativeArray<float4>(PlanesCount, ARAlloc.TempJob);
            for (int i = 0; i < PlanesCount; i++) {
                outputPlanes[i] = new float4(cullingPlanes[i].normal, cullingPlanes[i].distance);
            }
            return outputPlanes;
        }

        static MetadataValue CreateMetadataValue(int nameID, int gpuOffset, bool isPerInstance) {
            const uint IsPerInstanceBit = 0x80000000;
            return new MetadataValue {
                NameID = nameID,
                Value = (uint)gpuOffset | (isPerInstance ? (IsPerInstanceBit) : 0),
            };
        }

        static T* Malloc<T>(uint count) where T : unmanaged {
            return (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * count, UnsafeUtility.AlignOf<T>(), Allocator.TempJob);
        }

        // -- Jobs
        [BurstCompile]
        struct CameraFrustumJob : IJobParallelForBatch {
            [ReadOnly, DeallocateOnJobCompletion] public NativeArray<float4> planes;
            public uint renderingLayerMask;
#if UNITY_EDITOR
            public ulong sceneCullingMask;
#endif

            [ReadOnly] public UnsafeBitmask takenSlots;
            [ReadOnly] public UnsafeBitmask toUnregisterSlots;

            [ReadOnly] public UnsafeArray<float> xs;
            [ReadOnly] public UnsafeArray<float> ys;
            [ReadOnly] public UnsafeArray<float> zs;
            [ReadOnly] public UnsafeArray<float> radii;
            [ReadOnly] public UnsafeArray<uint> layersMasks;
#if UNITY_EDITOR
            [ReadOnly] public UnsafeArray<ulong> sceneCullingMasks;
#endif

            [WriteOnly] public UnsafeArray<ushort> outSplitMaskVisibility;

            public void Execute(int startIndex, int count) {
                var uStartIndex = (uint)startIndex;
                var uCount = (uint)count;

                var p0 = planes[0];
                var p1 = planes[1];
                var p2 = planes[2];
                var p3 = planes[3];
                var p4 = planes[4];
                var p5 = planes[5];

                for (var i = 0u; uCount - i >= 4; i += 4) {
                    var fullIndex = uStartIndex + i;
                    var simdXs = xs.ReinterpretLoad<float4>(fullIndex);
                    var simdYs = ys.ReinterpretLoad<float4>(fullIndex);
                    var simdZs = zs.ReinterpretLoad<float4>(fullIndex);
                    var simdRadii = radii.ReinterpretLoad<float4>(fullIndex);

                    var frustumMask =
                        p0.x * simdXs + p0.y * simdYs + p0.z * simdZs + p0.w + simdRadii > 0.0f &
                        p1.x * simdXs + p1.y * simdYs + p1.z * simdZs + p1.w + simdRadii > 0.0f &
                        p2.x * simdXs + p2.y * simdYs + p2.z * simdZs + p2.w + simdRadii > 0.0f &
                        p3.x * simdXs + p3.y * simdYs + p3.z * simdZs + p3.w + simdRadii > 0.0f &
                        p4.x * simdXs + p4.y * simdYs + p4.z * simdZs + p4.w + simdRadii > 0.0f &
                        p5.x * simdXs + p5.y * simdYs + p5.z * simdZs + p5.w + simdRadii > 0.0f;

                    var simdRenderingLayersMasks = layersMasks.ReinterpretLoad<uint4>(fullIndex);
                    var masksMask = (simdRenderingLayersMasks & renderingLayerMask) != 0;
#if UNITY_EDITOR
                    var simdSceneCullingMasks = sceneCullingMasks.ReinterpretLoad<ulong4>(fullIndex);
                    masksMask &= (simdSceneCullingMasks & sceneCullingMask) != 0;
#endif

                    var slotsMask = takenSlots.LoadSIMD(fullIndex) & !toUnregisterSlots.LoadSIMD(fullIndex);

                    var bigSplits = math.select(uint4.zero, new uint4(1), frustumMask & masksMask & slotsMask);
                    ushort4 splits = default;
                    splits.x = (ushort)bigSplits.x;
                    splits.y = (ushort)bigSplits.y;
                    splits.z = (ushort)bigSplits.z;
                    splits.w = (ushort)bigSplits.w;
                    outSplitMaskVisibility.ReinterpretStore(fullIndex, splits);
                }

                for (var i = uCount.SimdTrailing(); i < uCount; ++i) {
                    var fullIndex = uStartIndex + i;
                    var position = new float3(xs[fullIndex], ys[fullIndex], zs[fullIndex]);
                    var r = radii[fullIndex];
                    var frustumVisible =
                        math.dot(p0.xyz, position) + p0.w + r > 0.0f &
                        math.dot(p1.xyz, position) + p1.w + r > 0.0f &
                        math.dot(p2.xyz, position) + p2.w + r > 0.0f &
                        math.dot(p3.xyz, position) + p3.w + r > 0.0f &
                        math.dot(p4.xyz, position) + p4.w + r > 0.0f &
                        math.dot(p5.xyz, position) + p5.w + r > 0.0f;

                    var maskMask = (layersMasks[fullIndex] & renderingLayerMask) != 0;
#if UNITY_EDITOR
                    maskMask &= (sceneCullingMasks[fullIndex] & sceneCullingMask) != 0;
#endif

                    var slotsMask = takenSlots[fullIndex] & !toUnregisterSlots[fullIndex];

                    outSplitMaskVisibility[fullIndex] = (ushort)math.select(0, 1, frustumVisible & maskMask & slotsMask);
                }
            }
        }

        [BurstCompile]
        struct LightFrustumJob : IJobParallelForBatch {
            public uint renderingLayerMask;
#if UNITY_EDITOR
            public ulong sceneCullingMask;
#endif

            [ReadOnly] public UnsafeBitmask takenSlots;
            [ReadOnly] public UnsafeBitmask toUnregisterSlots;
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
            [ReadOnly] public UnsafeArray<uint> layersMasks;
#if UNITY_EDITOR
            [ReadOnly] public UnsafeArray<ulong> sceneCullingMasks;
#endif

            [WriteOnly] public UnsafeArray<ushort> outSplitMaskVisibility;
            public UnsafeArray<ushort> outAggregatedSplitMaskVisibility;

            public void Execute(int startIndex, int count) {
                var uStartIndex = (uint)startIndex;
                var uCount = (uint)count;

                for (var i = 0u; uCount - i >= 4; i += 4) {
                    var fullIndex = uStartIndex + i;
                    var simdXs = xs.ReinterpretLoad<float4>(fullIndex);
                    var simdYs = ys.ReinterpretLoad<float4>(fullIndex);
                    var simdZs = zs.ReinterpretLoad<float4>(fullIndex);
                    var simdRadii = radii.ReinterpretLoad<float4>(fullIndex);

                    CullingUtils.LightSimdCulling(receiversPlanes, frustumSplits, cullingPlanes,
                        worldToLightSpaceRotation, spheresSplitInfos, lightFacingFrustumPlanes,
                        simdXs, simdYs, simdZs, simdRadii,
                        out var mask);

                    var simdRenderingLayersMasks = layersMasks.ReinterpretLoad<uint4>(fullIndex);
                    var masksMask = (simdRenderingLayersMasks & renderingLayerMask) != 0;
#if UNITY_EDITOR
                    var simdSceneCullingMasks = sceneCullingMasks.ReinterpretLoad<ulong4>(fullIndex);
                    masksMask &= (simdSceneCullingMasks & sceneCullingMask) != 0;
#endif

                    var slotsMask = takenSlots.LoadSIMD(fullIndex) & !toUnregisterSlots.LoadSIMD(fullIndex);

                    mask = math.select(uint4.zero, mask, masksMask & slotsMask);

                    ushort4 splits = default;
                    splits.x = (ushort)mask.x;
                    splits.y = (ushort)mask.y;
                    splits.z = (ushort)mask.z;
                    splits.w = (ushort)mask.w;

                    outSplitMaskVisibility.ReinterpretStore(fullIndex, splits);

                    var aggregatedSplits = outAggregatedSplitMaskVisibility.ReinterpretLoad<ushort4>(fullIndex);
                    aggregatedSplits |= splits;
                    outAggregatedSplitMaskVisibility.ReinterpretStore(fullIndex, aggregatedSplits);
                }

                for (var i = uCount.SimdTrailing(); i < uCount; ++i) {
                    var fullIndex = uStartIndex + i;

                    var position = new float3(xs[fullIndex], ys[fullIndex], zs[fullIndex]);
                    var r = radii[fullIndex];

                    CullingUtils.LightCulling(receiversPlanes, frustumSplits, cullingPlanes,
                        worldToLightSpaceRotation, spheresSplitInfos, lightFacingFrustumPlanes,
                        position, r, out var mask);

                    var maskMask = (layersMasks[fullIndex] & renderingLayerMask) != 0;
#if UNITY_EDITOR
                    maskMask &= (sceneCullingMasks[fullIndex] & sceneCullingMask) != 0;
#endif

                    var slotsMask = takenSlots[fullIndex] & !toUnregisterSlots[fullIndex];

                    mask = math.select(0, mask, maskMask & slotsMask);

                    outSplitMaskVisibility[fullIndex] = (ushort)mask;
                    outAggregatedSplitMaskVisibility[fullIndex] |= (ushort)mask;
                }
            }
        }

        [BurstCompile]
        struct EmitDrawCommandsJob : IJob {
            [ReadOnly] public BatchID batchID;
            [ReadOnly] public GraphicsBufferHandle indexBuffer;

            [ReadOnly] public UnsafeArray<FilterSettings>.Span filterSettings;
            [ReadOnly] public NativeList<SubmeshWithSplit> submeshesWithSplits;
            [ReadOnly] public NativeList<RendererInstancesData> instances;

            [ReadOnly] public UnsafeArray<float> xs;
            [ReadOnly] public UnsafeArray<float> ys;
            [ReadOnly] public UnsafeArray<float> zs;

            [WriteOnly, NativeDisableUnsafePtrRestriction]
            public BatchCullingOutputDrawCommands* outDrawCommandsOutput;

            public void Execute() {
                var commandsCount = 0u;
                var instancesCount = 0u;
                var instancesForSortingCount = 0u;
                for (var i = 0; i < submeshesWithSplits.Length; ++i) {
                    var submesh = submeshesWithSplits[i].submesh;
                    var subInstances = instances[submesh.instancesIndex];
                    var rendererInstances = subInstances.Length;
                    var hasTransparency = filterSettings[submesh.filterSettingsId].hasTransparency;

#if DISABLE_BATCHING
                    commandsCount += rendererInstances;
#else
                    commandsCount += hasTransparency ? rendererInstances : 1;
#endif

                    instancesCount += rendererInstances;
                    instancesForSortingCount += hasTransparency ? rendererInstances : 0u;
                }

                var drawCommands = Malloc<BatchDrawCommandProcedural>(commandsCount);
                var visibleInstances = Malloc<int>(instancesCount);
                outDrawCommandsOutput->proceduralDrawCommands = drawCommands;
                outDrawCommandsOutput->visibleInstances = visibleInstances;
                outDrawCommandsOutput->drawRanges = Malloc<BatchDrawRange>((uint)submeshesWithSplits.Length);

                outDrawCommandsOutput->drawCommandPickingInstanceIDs = null;

                var sportingPositions = Malloc<float3>(instancesForSortingCount);
                outDrawCommandsOutput->instanceSortingPositions = (float*)sportingPositions;
                outDrawCommandsOutput->instanceSortingPositionFloatCount = (int)(instancesForSortingCount * 3);

                var drawCommandIndex = 0;
                var visibleInstancesIndex = 0;
                var drawRangesIndex = 0;
                var sortingPositionIndex = 0;

                for (var i = 0; i < submeshesWithSplits.Length; ++i) {
                    var submesh = submeshesWithSplits[i].submesh;
                    var hasTransparency = filterSettings[submesh.filterSettingsId].hasTransparency;
                    var commandFlags = BatchDrawCommandFlags.HasMotion;
                    if (hasTransparency) {
                        commandFlags |= BatchDrawCommandFlags.HasSortingPosition;
                    }

                    var instancesStart = (uint)visibleInstancesIndex;
                    var sortingStart = sortingPositionIndex;

                    var subInstances = instances[submesh.instancesIndex].takenSlots;
                    if (hasTransparency) {
                        for (var j = 0; j < subInstances.Length; j++) {
                            var uInstance = subInstances[j];
                            var instance = (int)uInstance;
                            visibleInstances[visibleInstancesIndex] = instance;
                            ++visibleInstancesIndex;
                            sportingPositions[sortingPositionIndex++] = new float3(xs[uInstance], ys[uInstance], zs[uInstance]);
                        }
                    } else {
                        for (var j = 0; j < subInstances.Length; j++) {
                            var uInstance = subInstances[j];
                            var instance = (int)uInstance;
                            visibleInstances[visibleInstancesIndex] = instance;
                            ++visibleInstancesIndex;
                        }
                    }

                    subInstances.Dispose();

                    var splitMask = submeshesWithSplits[i].splitMask;

                    var drawCommandIndexStart = (uint)drawCommandIndex;

                    if (hasTransparency) {
                        int iter = 0;
                        for (uint inst = instancesStart; inst < visibleInstancesIndex; ++inst, ++iter) {
                            drawCommands[drawCommandIndex] = new BatchDrawCommandProcedural {
                                flags = commandFlags,
                                batchID = batchID,
                                materialID = submesh.materialID,
                                splitVisibilityMask = splitMask,
                                lightmapIndex = 0xFFFF,
                                sortingPosition = (sortingStart+iter)*3,
                                visibleOffset = inst,
                                visibleCount = 1u,
                                topology = MeshTopology.Triangles,
                                indexBufferHandle = indexBuffer,
                                baseVertex = 0,
                                elementCount = submesh.indicesCount,
                                indexOffsetBytes = submesh.indexBytesOffset,
                            };
                            ++drawCommandIndex;
                        }
                    } else {
                        drawCommands[drawCommandIndex] = new BatchDrawCommandProcedural {
                            flags = commandFlags,
                            batchID = batchID,
                            materialID = submesh.materialID,
                            splitVisibilityMask = splitMask,
                            lightmapIndex = 0xFFFF,
                            sortingPosition = 0,
                            visibleOffset = instancesStart,
                            visibleCount = (uint)(visibleInstancesIndex - instancesStart),
                            topology = MeshTopology.Triangles,
                            indexBufferHandle = indexBuffer,
                            baseVertex = 0,
                            elementCount = submesh.indicesCount,
                            indexOffsetBytes = submesh.indexBytesOffset,
                        };
                        ++drawCommandIndex;
                    }

                    outDrawCommandsOutput->drawRanges[drawRangesIndex++] = new BatchDrawRange {
                        drawCommandsType = BatchDrawCommandType.Procedural,
                        drawCommandsBegin = drawCommandIndexStart,
                        drawCommandsCount = (uint)drawCommandIndex - drawCommandIndexStart,
                        filterSettings = CreateBatchFilterSettings(filterSettings[submesh.filterSettingsId]),
                    };
                }

                outDrawCommandsOutput->proceduralDrawCommandCount = drawCommandIndex;
                outDrawCommandsOutput->visibleInstanceCount = visibleInstancesIndex;
                outDrawCommandsOutput->drawRangeCount = drawRangesIndex;
            }

            BatchFilterSettings CreateBatchFilterSettings(FilterSettings filterSettings) {
                return new BatchFilterSettings {
                    layer = filterSettings.layer,
                    renderingLayerMask = filterSettings.renderingLayerMask,
#if UNITY_EDITOR
                    sceneCullingMask = filterSettings.sceneCullingMask,
#endif
                    shadowCastingMode = filterSettings.castShadows,
                    allDepthSorted = filterSettings.hasTransparency,

                    receiveShadows = true,
                    motionMode = MotionVectorGenerationMode.Object,
                };
            }
        }

        [BurstCompile]
        struct TriageRenderersJob : IJobParallelFor {
            [ReadOnly] public UnsafeArray<ushort> splitMaskVisibility;
            [ReadOnly] public UnsafeArray<SubmeshData>.Span submeshes;
            [ReadOnly] public UnsafeArray<RendererInstancesData>.Span instances;

            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeList<RendererInstancesData> outInstances;
            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeList<SubmeshWithSplit> outRegisteredRenderers;

            public void Execute(int index) {
                var uIndex = (uint)index;
                var submesh = submeshes[uIndex];
                if (submesh.Equals(default)) {
                    return;
                }

                var rendererInstances = instances[submesh.instancesIndex].takenSlots;

                var forSplits = new UnsafeArray<RendererInstancesData>(MaxSplitMasksCount, ARAlloc.InJobTemp);

                for (var i = 0; i < rendererInstances.Length; i++) {
                    var instance = rendererInstances[i];
                    var splitMask = splitMaskVisibility[instance];
                    if (splitMask == 0) {
                        continue;
                    }
                    ref var forSplitInstances = ref forSplits[splitMask];
                    if (forSplitInstances.takenSlots.IsCreated) {
                        forSplitInstances.Add(instance);
                    } else {
                        forSplitInstances = new RendererInstancesData(instance, ARAlloc.TempJob);
                    }
                }

                for (var i = 1u; i < MaxSplitMasksCount; ++i) {
                    if (!forSplits[i].IsCreated) {
                        continue;
                    }

                    var instancesId = (ushort)outInstances.ThreadSafeAddNoResize(forSplits[i]);

                    var rendererWithSplit = new SubmeshWithSplit(submesh.WithInstancesIndex(instancesId), (ushort)i);
                    outRegisteredRenderers.ThreadSafeAddNoResize(rendererWithSplit);
                }

                forSplits.Dispose();
            }
        }

#if ENABLE_PICKING
        [BurstCompile]
        struct TriageForPickingJob : IJob {
            [ReadOnly] public UnsafeArray<ushort> splitMaskVisibility;
            [ReadOnly] public UnsafeArray<SubmeshData>.Span submeshes;
            [ReadOnly] public UnsafeArray<RendererInstancesData>.Span instances;
            [ReadOnly] public UnsafeArray<int> rendererInstanceIDs;

            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeList<PickingDrawData> outDrawData;

            [NativeDisableUnsafePtrRestriction] public uint* outDrawCommandsCount;
            
            public void Execute() {
                for (uint index = 0; index < submeshes.Length; index++) {
                    var submesh = submeshes[index];
                    if (submesh.Equals(default)) {
                        return;
                    }

                    var rendererInstances = instances[submesh.instancesIndex].takenSlots;
                    for (var i = 0; i < rendererInstances.Length; i++) {
                        var instance = rendererInstances[i];
                        var splitMask = splitMaskVisibility[instance];
                        if (splitMask == 0) {
                            continue;
                        }

                        outDrawData.Add(new PickingDrawData {
                            renderer = index,
                            slot = instance,
                            instanceID = rendererInstanceIDs[instance],
                        });

                        *outDrawCommandsCount += 1;
                    }
                }
            }
        }

        [BurstCompile]
        struct EmmitDrawCommandsForPicking : IJob {
            [ReadOnly] public BatchID batchID;
            [ReadOnly] public GraphicsBufferHandle indexBuffer;
            [ReadOnly] public NativeList<PickingDrawData> drawData;
            [ReadOnly] public UnsafeArray<SubmeshData>.Span submeshes;
            
            [NativeDisableUnsafePtrRestriction] public uint* drawCommandsCount;

            [WriteOnly, NativeDisableUnsafePtrRestriction]
            public BatchCullingOutputDrawCommands* outDrawCommandsOutput;
            
            public void Execute() {
                var drawRanges = Malloc<BatchDrawRange>(1);
                var drawCommands = Malloc<BatchDrawCommandProcedural>(*drawCommandsCount);
                var drawCommandPickingInstanceIDs = Malloc<int>(*drawCommandsCount);
                var visibleInstances = Malloc<int>((uint)drawData.Length);

                drawRanges[0] = new BatchDrawRange {
                    drawCommandsType = BatchDrawCommandType.Procedural,
                    drawCommandsBegin = 0,
                    drawCommandsCount = *drawCommandsCount,
                    filterSettings = new BatchFilterSettings {
                        layer = 3,
                        batchLayer = 29,
                        renderingLayerMask = 0xFF,
                        rendererPriority = 0,
                        allDepthSorted = false
                    }
                };
                
                outDrawCommandsOutput->drawRanges = drawRanges;
                outDrawCommandsOutput->drawRangeCount = 1;
                outDrawCommandsOutput->proceduralDrawCommands = drawCommands;
                outDrawCommandsOutput->drawCommandPickingInstanceIDs = drawCommandPickingInstanceIDs;
                outDrawCommandsOutput->proceduralDrawCommandCount = (int)*drawCommandsCount;
                outDrawCommandsOutput->visibleInstances = visibleInstances;
                outDrawCommandsOutput->visibleInstanceCount = drawData.Length;

                int index = 0;
                for (var i = 0; i < drawData.Length; ++i) {
                    var data = drawData[i];
                    var submesh = this.submeshes[data.renderer];
                    drawCommands[index] = new BatchDrawCommandProcedural {
                        flags = BatchDrawCommandFlags.None,
                        batchID = batchID,
                        materialID = submesh.materialID,
                        splitVisibilityMask = 0x1,
                        lightmapIndex = 0xFFFF,
                        sortingPosition = 0,
                        visibleOffset = (uint)i,
                        visibleCount = 1u,
                        topology = MeshTopology.Triangles,
                        indexBufferHandle = indexBuffer,
                        baseVertex = 0,
                        elementCount = submesh.indicesCount,
                        indexOffsetBytes = submesh.indexBytesOffset,
                    };
                    drawCommandPickingInstanceIDs[index] = data.instanceID;
                    index++;
                    visibleInstances[i] = (int)data.slot;
                }
            }
        }
#endif

        // === Data
        public readonly struct FilterSettings : IEquatable<FilterSettings> {
#if UNITY_EDITOR
            public readonly ulong sceneCullingMask;
#endif
            public readonly uint renderingLayerMask;
            public readonly ShadowCastingMode castShadows; //Should be a byte but Unity
            public readonly byte layer;
            public readonly bool hasTransparency;

            public FilterSettings(uint renderingLayerMask, byte layer,
#if UNITY_EDITOR
                ulong sceneCullingMask,
#endif
                ShadowCastingMode castShadows, bool hasTransparency) {
#if UNITY_EDITOR
                this.sceneCullingMask = sceneCullingMask;
#endif
                this.renderingLayerMask = renderingLayerMask;
                this.layer = layer;
                this.castShadows = castShadows;
                this.hasTransparency = hasTransparency;
            }

            public FilterSettings(KandraRenderer.RendererFilteringSettings rendererFilterSettings, KandraRenderer renderer) {
#if UNITY_EDITOR
                this.sceneCullingMask = renderer.gameObject.sceneCullingMask;
#endif
                this.renderingLayerMask = rendererFilterSettings.renderingLayersMask;
                this.layer = (byte)renderer.gameObject.layer;
                this.castShadows = rendererFilterSettings.shadowCastingMode;
                this.hasTransparency = false;
            }

            public FilterSettings WithTransparency(bool hasTransparency) {
                return new FilterSettings(renderingLayerMask, layer,
#if UNITY_EDITOR
                    sceneCullingMask,
#endif
                    castShadows, hasTransparency);
            }

            public bool Equals(FilterSettings other) {
                return renderingLayerMask == other.renderingLayerMask &&
                       layer == other.layer &&
#if UNITY_EDITOR
                       sceneCullingMask == other.sceneCullingMask &&
#endif
                       castShadows == other.castShadows &&
                       hasTransparency == other.hasTransparency;
            }

            public override int GetHashCode() {
                unchecked {
                    int hashCode = (int)renderingLayerMask;
                    hashCode = (hashCode * 397) ^ layer.GetHashCode();
#if UNITY_EDITOR
                    hashCode = (hashCode * 397) ^ sceneCullingMask.GetHashCode();
#endif
                    hashCode = (hashCode * 397) ^ castShadows.GetHashCode();
                    hashCode = (hashCode * 397) ^ hasTransparency.GetHashCode();
                    return hashCode;
                }
            }
        }

        readonly struct SubmeshData : IEquatable<SubmeshData> {
            public readonly uint indicesCount;
            public readonly uint indexBytesOffset;
            public readonly BatchMaterialID materialID;
            public readonly ushort filterSettingsId;
            public readonly ushort instancesIndex; // Not Equatable
            public readonly byte index;

            public SubmeshData(uint indicesCount, uint indexElementOffset, BatchMaterialID materialID, ushort filterSettingsId, byte index) {
                this.indicesCount = indicesCount;
                this.indexBytesOffset = indexElementOffset * 2;
                this.materialID = materialID;
                this.index = index;
                this.filterSettingsId = filterSettingsId;
                this.instancesIndex = ushort.MaxValue;
            }

            public SubmeshData(uint indicesCount, uint indexElementOffset, BatchMaterialID materialID, ushort filterSettingsId, ushort instancesIndex, byte index) {
                this.indicesCount = indicesCount;
                this.indexBytesOffset = indexElementOffset * 2;
                this.materialID = materialID;
                this.index = index;
                this.filterSettingsId = filterSettingsId;
                this.instancesIndex = instancesIndex;
            }

            public SubmeshData WithInstancesIndex(ushort newInstancesIndex) {
                return new(indicesCount, indexBytesOffset / 2, materialID, filterSettingsId, newInstancesIndex, index);
            }

            public SubmeshData WithFilterSettingsId(ushort newFilterSettingsId) {
                return new(indicesCount, indexBytesOffset / 2, materialID, newFilterSettingsId, instancesIndex, index);
            }

            public SubmeshData WithMesh(in KandraRenderingMesh renderingMesh) {
                var newIndicesCount = renderingMesh.IndexCount(index);
                var indexElementOffset = renderingMesh.IndexStart(index);
                return new(newIndicesCount, indexElementOffset, materialID, filterSettingsId, instancesIndex, index);
            }

            public SubmeshData WithMaterial(BatchMaterialID newMaterialId, ushort newFilterSettingsId) {
                return new(indicesCount, indexBytesOffset / 2, newMaterialId, newFilterSettingsId, instancesIndex, index);
            }

            public bool Equals(SubmeshData other) {
                return indicesCount == other.indicesCount &
                       indexBytesOffset == other.indexBytesOffset &
                       materialID.Equals(other.materialID) &
                       filterSettingsId == other.filterSettingsId &
                       index == other.index;
            }

            public override int GetHashCode() {
                unchecked {
                    var hashCode = (int)indicesCount;
                    hashCode = (hashCode * 397) ^ (int)indexBytesOffset;
                    hashCode = (hashCode * 397) ^ materialID.GetHashCode();
                    hashCode = (hashCode * 397) ^ filterSettingsId.GetHashCode();
                    hashCode = (hashCode * 397) ^ index.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(SubmeshData left, SubmeshData right) {
                return left.Equals(right);
            }

            public static bool operator !=(SubmeshData left, SubmeshData right) {
                return !left.Equals(right);
            }
        }

        readonly struct SubmeshWithSplit {
            public readonly SubmeshData submesh;
            public readonly ushort splitMask;

            public SubmeshWithSplit(SubmeshData submesh, ushort splitMask) {
                this.submesh = submesh;
                this.splitMask = splitMask;
            }
        }

        struct RendererInstancesData {
            public UnsafeList<uint> takenSlots;

            public int RefCount => takenSlots.Length;
            public bool IsCreated => takenSlots.IsCreated;
            public uint Length => (uint)takenSlots.Length;

            public RendererInstancesData(uint slot, Allocator allocator) {
                takenSlots = new UnsafeList<uint>(4, allocator);
                takenSlots.Add(slot);
            }

            public void Add(uint slot) {
                takenSlots.Add(slot);
            }

            public void Dispose() {
                if (takenSlots.IsCreated) {
                    takenSlots.Dispose();
                }
            }

            [UnityEngine.Scripting.Preserve]
            public RendererInstancesData Copy() {
                var takenCopy = new UnsafeList<uint>(takenSlots.Ptr, takenSlots.Length);
                return new RendererInstancesData {
                    takenSlots = takenCopy,
                };
            }
        }

        [UnityEngine.Scripting.Preserve]
        struct RendererInstancesDataNull : IEquatable<RendererInstancesData> {
            public bool Equals(RendererInstancesData other) {
                return other.takenSlots.IsCreated == false;
            }

            public override int GetHashCode() {
                return 0;
            }
        }

        public struct InstanceData {
            public uint instanceStartVertex;
            public uint sharedStartVertex;
        }

        // === Memory Snapshot
        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 1;

            var registeredRenderers = KandraRendererManager.Instance.RegisteredRenderers;
            var usedInstancesBufferBytes = registeredRenderers * sizeof(InstanceData) + 2 * sizeof(PackedMatrix);
            var totalInstancesBufferBytes = _instanceBuffer.count * _instanceBuffer.stride;

            memoryBuffer.Slice(0, 1).Span[0] = new("InstanceBuffer", totalInstancesBufferBytes, usedInstancesBufferBytes);

            // TODO: Implement the rest of the memory snapshot
            var selfMasksSize = cameraSplitMaskVisibility.Length * sizeof(ushort) * 3;
            var usedMasksSize = registeredRenderers * sizeof(ushort) * 3;

            var selfRendererBySlot = _rendererBySlot.Length * sizeof(ushort);
            var usedRendererBySlot = registeredRenderers * sizeof(ushort);

            var selfInstancesSize = (ulong)(_instances.Capacity * sizeof(RendererInstancesData));
            var usedInstancesSize = 0ul;
            for (var i = 0; i < _instances.Length; i++) {
                if (_instances[i].IsCreated) {
                    var takenSlotsSize = (ulong)(_instances[i].takenSlots.Length * sizeof(uint));
                    selfInstancesSize += takenSlotsSize;
                    usedInstancesSize += takenSlotsSize + (ulong)sizeof(RendererInstancesData);
                }
            }

            var selfRenderersSize = (ulong)(_renderers.Capacity * sizeof(SubmeshData));
            var usedRenderersSize = 0ul;
            for (var i = 0; i < _renderers.Length; i++) {
                if (_renderers[i].Equals(default)) {
                    continue;
                }
                var submeshesSize = (ulong)sizeof(SubmeshData);
                selfRenderersSize += submeshesSize;
                usedRenderersSize += submeshesSize;
            }

            var selfFilterSettingsSize = (ulong)(_filterSettings.Capacity * sizeof(FilterSettings));
            var usedFilterSettingsSize = (ulong)(_filterSettings.Length * sizeof(FilterSettings));

            var selfSize = selfMasksSize + selfRendererBySlot + selfInstancesSize + selfRenderersSize + selfFilterSettingsSize;
            var usedSize = usedMasksSize + usedRendererBySlot + usedInstancesSize + usedRenderersSize + usedFilterSettingsSize;

            ownPlace.Span[0] = new(nameof(SkinnedBatchRenderGroup), selfSize, usedSize, memoryBuffer[..childrenCount]);

            return childrenCount;
        }
        
        struct PickingDrawData {
            public uint renderer;
            public uint slot;
            public int instanceID;
        }
    }
}