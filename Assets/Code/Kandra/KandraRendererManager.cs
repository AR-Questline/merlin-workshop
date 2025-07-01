using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Awaken.Kandra.AnimationPostProcessing;
using Awaken.Kandra.Managers;
using Awaken.Kandra.VFXs;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Graphics.Mipmaps;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using MagicaCloth2;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using UniversalProfiling;

namespace Awaken.Kandra {
    [Il2CppEagerStaticClassConstruction]
    public class KandraRendererManager : MipmapsStreamingMasterMaterials.IMipmapsFactorProvider, IMainMemorySnapshotProvider {
        const float EditModeBufferSizeMultiplier = 15f;
        const float PlayModeBufferSizeMultiplier = 1f;
        
#if !UNITY_EDITOR
        const float RuntimeBufferSizeMultiplier = 1f;
#endif

        public const int RenderersCapacity = 1_000;
        public const int RigBonesCapacity = 6_400; // Bones rig
        public const int UniqueMeshesCapacity = 50;
        public const int UniqueBindposesCapacity = 2_500; // Bindposes memory
        public const int UniqueVerticesCapacity = 750_000; // Verts memory
        public const int IndicesCapacity = 3_850_000; // Indices memo
        public const int SkinBonesCapacity = 13_000; // Skin bones
        public const int BlendshapesCapacity = 2_500;
        public const int BlendshapesDeltasCapacity = 2_750_000; // Blendshape deltas
        public const int SkinnedVerticesCapacity = 2_750_000; // Verts skinned

        public const uint InvalidBitmask = 1u << 31;
        public const uint WaitingBitmask = 1u << 30;
        public const uint UnregisterToWaitingBitmask = 1u << 29;
        public const uint MetaBitmask = InvalidBitmask | WaitingBitmask | UnregisterToWaitingBitmask;
        public const uint MaxRenderers = ~(MetaBitmask);
        public const uint ValidBitmask = ~(MetaBitmask);

        static readonly UniversalProfilerMarker RegisterRendererMarker = new UniversalProfilerMarker("KandraRendererManager.RegisterRenderer");
        static readonly UniversalProfilerMarker UnregisterRendererMarker = new UniversalProfilerMarker("KandraRendererManager.UnregisterRenderer");
        static readonly UniversalProfilerMarker EarlyUpdateMarker = new UniversalProfilerMarker("KandraRendererManager.EarlyUpdate");
        static readonly UniversalProfilerMarker PreLateUpdateMarker = new UniversalProfilerMarker("KandraRendererManager.PreLateUpdate");
        static readonly UniversalProfilerMarker AfterAnimatorMarker = new UniversalProfilerMarker("KandraRendererManager.AfterAnimator");
        static readonly UniversalProfilerMarker TimeUpdateMarker = new UniversalProfilerMarker("KandraRendererManager.TimeUpdate");
        static readonly UniversalProfilerMarker BeginRenderingMarker = new UniversalProfilerMarker("KandraRendererManager.BeginRendering");

        public static KandraRendererManager Instance { get; private set; }

        public bool enabled = true;

        UnsafeBitmask _toRegister;
        UnsafeBitmask _toUnregister;
#if UNITY_EDITOR || AR_DEBUG
        UnsafeBitmask _toUnregisterAutomatic;
#endif
        // TODO: Make it temp for registration, with some method like FirstZeroFromOrBitsets
        UnsafeBitmask _submittedSlots;
        UnsafeBitmask _fullyRegisteredSlots;
        KandraRenderer[] _renderers;

        StructList<KandraRenderer> _trackedRenderersToDispose = new StructList<KandraRenderer>(16);
        StructList<KandraTrisCuller> _trackedCullersToDispose = new StructList<KandraTrisCuller>(16);
        StructList<KandraTrisCullee> _trackedCulleesToDispose = new StructList<KandraTrisCullee>(16);
        CommandBuffer _commandBuffer;

#if UNITY_EDITOR
        UnsafeArray<int> _editorRendererInstanceIds;
#endif
        
        UnsafeArray<UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>> _materialsIds;
        // TODO: Move to proper place
        UnsafeArray<float> _reciprocalUvDistributions;

        UnsafeHashSet<uint> _alreadyMessagedBrokenKandraIds;

        public RigManager RigManager { get; private set; }
        public MeshManager MeshManager { get; private set; }
        public AnimatorManager AnimatorManager { get; private set; }
        public BonesManager BonesManager { get; private set; }
        public SkinningManager SkinningManager { get; private set; }
        public BlendshapesManager BlendshapesManager { get; private set; }
        public VisibilityCullingManager VisibilityCullingManager { get; private set; }
        public SkinnedBatchRenderGroup SkinnedBatchRenderGroup { get; private set; }
        public MeshBroker MeshBroker { get; private set; }
        public MaterialBroker MaterialBroker { get; private set; }
        public StreamingManager StreamingManager { get; private set; }
        public KandraVfxHelper KandraVfxHelper { get; private set; }

        public uint RegisteredRenderers => _fullyRegisteredSlots.CountOnes();
        public int FullyRegisteredRenderersLength => _fullyRegisteredSlots.LastOne()+1;

        public KandraRenderer[] ActiveRenderers => _renderers;
        public UnsafeBitmask FullyRegisteredSlots => _fullyRegisteredSlots;
        public UnsafeBitmask ToUnregister => _toUnregister;

#if UNITY_EDITOR
        public UnsafeArray<int> EditorRendererInstanceIds => _editorRendererInstanceIds;
#endif

        // Buffer queries
        public static int FinalRenderersCapacity => BufferSize(Configuration.GetInt("kandra.renderers_capacity", RenderersCapacity));
        public static int FinalRigBonesCapacity => BufferSize(Configuration.GetInt("kandra.rig_bones_capacity", RigBonesCapacity));
        public static int FinalUniqueMeshesCapacity => BufferSize(Configuration.GetInt("kandra.unique_meshes_capacity", UniqueMeshesCapacity));
        public static int FinalUniqueBindposesCapacity => BufferSize(Configuration.GetInt("kandra.unique_bindposes_capacity", UniqueBindposesCapacity));
        public static int FinalUniqueVerticesCapacity => BufferSize(Configuration.GetInt("kandra.unique_vertices_capacity", UniqueVerticesCapacity));
        public static int FinalIndicesCapacity => BufferSize(Configuration.GetInt("kandra.indices_capacity", IndicesCapacity));
        public static int FinalSkinBonesCapacity => BufferSize(Configuration.GetInt("kandra.skin_bones_capacity", SkinBonesCapacity));
        public static int FinalBlendshapesCapacity => BufferSize(Configuration.GetInt("kandra.blendshapes_capacity", BlendshapesCapacity));
        public static int FinalBlendshapesDeltasCapacity => BufferSize(Configuration.GetInt("kandra.blendshapes_deltas_capacity", BlendshapesDeltasCapacity));
        public static int FinalSkinnedVerticesCapacity => BufferSize(Configuration.GetInt("kandra.skinned_vertices_capacity", SkinnedVerticesCapacity));

        public static void Init() {
            InitRuntimePart();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += static state => {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                    Instance.EDITOR_ExitPlaymode();
                    InitRuntimePart();
                    return;
                }
                
                if (!UnityEditor.EditorSettings.enterPlayModeOptionsEnabled || !UnityEditor.EditorSettings.enterPlayModeOptions.HasFlag(UnityEditor.EnterPlayModeOptions.DisableDomainReload)) {
                    if (state == UnityEditor.PlayModeStateChange.ExitingEditMode) {
                        Instance.Dispose();
                        Instance = null;
                    }
                } else if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode) {
                    InitRuntimePart();
                }
            };

            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += static () => {
                Instance?.Dispose();
                Instance = null;
            };
#endif
        }

        static void InitRuntimePart() {
            Instance?.Dispose();

            Instance = new KandraRendererManager();
            Instance.InitReferences();

            PlayerLoopUtils.RegisterToPlayerLoopBegin<KandraRendererManager, TimeUpdate>(Instance.OnTimeUpdateBegin);
            PlayerLoopUtils.RegisterToPlayerLoopBegin<VisibilityCullingManager, EarlyUpdate>(Instance.OnEarlyUpdateBegin);
            PlayerLoopUtils.RegisterToPlayerLoopBegin<VisibilityCullingManager, PreLateUpdate>(Instance.OnPreLateUpdateBegin);
            PlayerLoopUtils.RegisterToPlayerLoopAfter<KandraRendererManager, PreLateUpdate, PreLateUpdate.DirectorDeferredEvaluate>(Instance.OnPreLateUpdateEnd);
            RenderPipelineManager.beginContextRendering += Instance.OnBeginRendering;
        }

        KandraRendererManager() {
            IMainMemorySnapshotProvider.RegisterProvider(this);
        }

        void InitReferences() {
            var maxRenderers = (uint)FinalRenderersCapacity;
            Asserts.IsGreaterThan(MaxRenderers, maxRenderers, "KandraRendererManager: RenderersCapacity is too high");

            _toRegister = new UnsafeBitmask(maxRenderers, ARAlloc.Persistent);
            _toUnregister = new UnsafeBitmask(maxRenderers, ARAlloc.Persistent);
#if UNITY_EDITOR || AR_DEBUG
            _toUnregisterAutomatic = new UnsafeBitmask(maxRenderers, ARAlloc.Persistent);
#endif
            _submittedSlots = new UnsafeBitmask(maxRenderers, ARAlloc.Persistent);
            _fullyRegisteredSlots = new UnsafeBitmask(maxRenderers, ARAlloc.Persistent);
            _renderers = new KandraRenderer[maxRenderers];
#if UNITY_EDITOR
            _editorRendererInstanceIds = new UnsafeArray<int>(maxRenderers, ARAlloc.Persistent);
#endif
            _materialsIds = new UnsafeArray<UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>>(maxRenderers, ARAlloc.Persistent);
            _reciprocalUvDistributions = new UnsafeArray<float>(maxRenderers, ARAlloc.Persistent);

            var skinningShader = Resources.Load<ComputeShader>("Skinning");
            var prepareBonesShader = Resources.Load<ComputeShader>("PrepareBones");

            RigManager = new RigManager(prepareBonesShader);
            MeshManager = new MeshManager(skinningShader, prepareBonesShader);
            BonesManager = new BonesManager(skinningShader, prepareBonesShader);
            BlendshapesManager = new BlendshapesManager(skinningShader);
            SkinningManager = new SkinningManager(skinningShader);
            VisibilityCullingManager = new VisibilityCullingManager();
            SkinnedBatchRenderGroup = new SkinnedBatchRenderGroup(VisibilityCullingManager);
            AnimatorManager = new AnimatorManager(SkinnedBatchRenderGroup);
            MeshBroker = new MeshBroker();
            MaterialBroker = new MaterialBroker();
            StreamingManager = new StreamingManager();
            KandraVfxHelper = new KandraVfxHelper();

            MipmapsStreamingMasterMaterials.Instance.AddProvider(Instance);

            _commandBuffer = new CommandBuffer {
                name = "Kandra",
            };

            _alreadyMessagedBrokenKandraIds = new UnsafeHashSet<uint>(16, ARAlloc.Persistent);
        }

        void Dispose() {
            IMainMemorySnapshotProvider.UnregisterProvider(this);
            MipmapsStreamingMasterMaterials.Instance.RemoveProvider(Instance);

            PlayerLoopUtils.RemoveFromPlayerLoop<VisibilityCullingManager, EarlyUpdate>();
            PlayerLoopUtils.RemoveFromPlayerLoop<VisibilityCullingManager, PreLateUpdate>();
            PlayerLoopUtils.RemoveFromPlayerLoop<KandraRendererManager, PreLateUpdate>();
            PlayerLoopUtils.RemoveFromPlayerLoop<KandraRendererManager, TimeUpdate>();
            RenderPipelineManager.beginContextRendering -= OnBeginRendering;

            _commandBuffer.Dispose();

            RigManager.Dispose();
            MeshManager.Dispose();
            AnimatorManager.Dispose();
            BonesManager.Dispose();
            BlendshapesManager.Dispose();
            SkinningManager.Dispose();
            VisibilityCullingManager.Dispose();
            SkinnedBatchRenderGroup.Dispose();
            MeshBroker.Dispose();
            StreamingManager.Dispose();
            KandraVfxHelper.Dispose();

            _toRegister.Dispose();
            _toUnregister.Dispose();
#if UNITY_EDITOR || AR_DEBUG
            _toUnregisterAutomatic.Dispose();
#endif
            _submittedSlots.Dispose();
            _fullyRegisteredSlots.Dispose();
            _renderers = null;
#if UNITY_EDITOR
            _editorRendererInstanceIds.Dispose();
#endif
            for (var i = 0u; i < _materialsIds.Length; ++i) {
                if (_materialsIds[i].IsCreated) {
                    for (var j = 0u; j < _materialsIds[i].Length; ++j) {
                        MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(_materialsIds[i][j]);
                    }
                    _materialsIds[i].Dispose();
                }
            }
            _materialsIds.Dispose();
            _reciprocalUvDistributions.Dispose();

            _alreadyMessagedBrokenKandraIds.Dispose();
        }

        public void Register(KandraRenderer kandraRenderer) {
            var renderingId = kandraRenderer.RenderingId;
            RegisterRendererMarker.Begin();

            if (!IsInvalidId(renderingId)) {
                var oldSlot = USlot(renderingId);
                if (_toUnregister[oldSlot]) {
                    _toUnregister.Down(oldSlot);
#if UNITY_EDITOR || AR_DEBUG
                    _toUnregisterAutomatic.Down(oldSlot);
#endif
                    RegisterRendererMarker.End();
                    kandraRenderer.RenderingId = renderingId;
                    return;
                }
                Log.Important?.Error($"Trying to register with old handle {renderingId}, fallback to new registration", kandraRenderer);
            }

            var uSlot = (uint)_submittedSlots.FirstZero();
            _submittedSlots.Up(uSlot);
            _toRegister.Up(uSlot);
            _renderers[uSlot] = kandraRenderer;
#if UNITY_EDITOR
            _editorRendererInstanceIds[uSlot] = kandraRenderer.gameObject.GetHashCode();
#endif
            
            RegisterRendererMarker.End();

            kandraRenderer.RenderingId = uSlot | WaitingBitmask;
        }

        public void Unregister(KandraRenderer kandraRenderer) {
            var renderingId = kandraRenderer.RenderingId;
            var uSlot = USlot(renderingId);
            if (_toUnregister[uSlot]) {
#if UNITY_EDITOR || AR_DEBUG
                if (!_toUnregisterAutomatic[uSlot]) {
                    Log.Important?.Error("Trying to unregister a renderer which is scheduled to be unregistered");
                }
                _toUnregisterAutomatic.Down(uSlot);
#endif
                kandraRenderer.RenderingId &= ~UnregisterToWaitingBitmask;
                return;
            }

#if UNITY_EDITOR || AR_DEBUG
            _toUnregisterAutomatic.Down(uSlot);
#endif

            // If just added to registration list, just remove it from there
            if (_toRegister[uSlot]) {
                _submittedSlots.Down(uSlot);
                _toRegister.Down(uSlot);

                _renderers[uSlot] = default;
#if UNITY_EDITOR
                _editorRendererInstanceIds[uSlot] = 0;
#endif
                kandraRenderer.RenderingId = InvalidBitmask;
                return;
            }

            _toUnregister.Up(uSlot);
            kandraRenderer.RenderingId &= ~UnregisterToWaitingBitmask;
        }

        public void RigChanged(KandraRig kandraRig, List<KandraRenderer> renderers) {
            if (!RigManager.CanChange(kandraRig, out var newMemory)) {
                Log.Critical?.Error($"Cannot change rig {kandraRig.name} [{kandraRig.GetHashCode()}] because there is no space for it.", kandraRig);
                var renderersCount = renderers.Count;
                for (var i = 0; i < renderersCount; i++) {
                    var id = renderers[i].RenderingId;
                    if (IsWaitingId(id)) {
                        continue;
                    }
                    var uSlot = USlot(id);
                    if (_toUnregister[uSlot]) {
                        continue;
                    }
                    Unregister(renderers[i]);
#if UNITY_EDITOR || AR_DEBUG
                    _toUnregisterAutomatic.Up(uSlot);
#endif
                    renderers[i].RenderingId |= UnregisterToWaitingBitmask;
                }
            } else {
                // Update RigManager
                RigManager.RigChanged(kandraRig, newMemory);
                // Update BonesManager
                var renderersCount = renderers.Count;
                for (var i = 0; i < renderersCount; i++) {
                    var id = renderers[i].RenderingId;
                    if (IsWaitingId(id)) {
                        continue;
                    }
                    var uSlot = USlot(id);
                    if (_toRegister[uSlot]) {
                        continue;
                    }
                    ref var registerData = ref renderers[i].rendererData;
                    var meshRegion = MeshManager.GetMeshMemory(registerData.mesh);
                    BonesManager.RigChanged(uSlot, registerData.bones, newMemory, meshRegion.bindPosesMemory);
                }
            }
        }

        public void UpdateSubmeshIndices(uint renderingId, KandraRenderingMesh renderingMesh) {
            if (IsWaitingId(renderingId)) {
                return;
            }
            var uSlot = USlot(renderingId);
            if (_toRegister[uSlot]) {
                return;
            }

            SkinnedBatchRenderGroup.UpdateSubmeshIndices(uSlot, renderingMesh);
        }

        public void UpdateRenderingMaterials(uint renderingId, Material[] materials, KandraRenderingMesh renderingMesh) {
            if (IsWaitingId(renderingId)) {
                return;
            }
            var uSlot = USlot(renderingId);
            if (_toRegister[uSlot]) {
                return;
            }

            var renderer = _renderers[uSlot];
            var registerData = renderer.rendererData;
            var rendererFilterSettings = registerData.filteringSettings;
            var filterSettings = new SkinnedBatchRenderGroup.FilterSettings(rendererFilterSettings, renderer);
            SkinnedBatchRenderGroup.UpdateMaterials(uSlot, materials, renderingMesh, filterSettings);

            var materialsIds = _materialsIds[uSlot];
            if (materialsIds.Length == materials.Length) {
                for (var i = 0u; i < materialsIds.Length; i++) {
                    MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(materialsIds[i]);
                    materialsIds[i] = MipmapsStreamingMasterMaterials.Instance.AddMaterial(materials[i]);
                }
            } else {
                for (var i = 0u; i < materialsIds.Length; i++) {
                    MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(materialsIds[i]);
                }
                materialsIds.Dispose();
                materialsIds = new UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>((uint)materials.Length, ARAlloc.Persistent);
                _materialsIds[uSlot] = materialsIds;
                for (var i = 0u; i < materialsIds.Length; i++) {
                    materialsIds[i] = MipmapsStreamingMasterMaterials.Instance.AddMaterial(materials[i]);
                }
            }
        }

        public void UpdateFilterSettings(uint renderingId) {
            if (IsWaitingId(renderingId)) {
                return;
            }
            var uSlot = USlot(renderingId);
            if (_toRegister[uSlot]) {
                return;
            }

            var renderer = _renderers[uSlot];
            var registerData = renderer.rendererData;
            var rendererFilterSettings = registerData.filteringSettings;
            var filterSettings = new SkinnedBatchRenderGroup.FilterSettings(rendererFilterSettings, renderer);
            SkinnedBatchRenderGroup.UpdateFilterSettings(uSlot, filterSettings);
        }

        public void UpdateMipmapsStreaming(uint renderingId) {
            if (IsWaitingId(renderingId)) {
                return;
            }
            var uSlot = USlot(renderingId);
            if (_toRegister[uSlot]) {
                return;
            }

            var renderer = _renderers[uSlot];
            var materials = renderer.rendererData.RenderingMaterials;
            var materialsCount = (uint)materials.Length;

            var materialsIds = _materialsIds[uSlot];
            for (var i = 0u; i < materialsIds.Length; i++) {
                MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(materialsIds[i]);
            }

            if (materialsIds.Length != materialsCount) {
                materialsIds.Dispose();
                materialsIds = new UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>(materialsCount, ARAlloc.Persistent);
            }

            for (var i = 0u; i < materialsCount; i++) {
                var materialId = MipmapsStreamingMasterMaterials.Instance.AddMaterial(materials[i]);
                materialsIds[i] = materialId;
            }
            _materialsIds[uSlot] = materialsIds;
        }

        public void StartTracking(KandraRenderer kandraRenderer) {
            _trackedRenderersToDispose.Add(kandraRenderer);
        }

        public void StopTracking(KandraRenderer kandraRenderer) {
            _trackedRenderersToDispose.Remove(kandraRenderer);
        }

        public void StartTracking(KandraTrisCuller culler) {
            _trackedCullersToDispose.Add(culler);
        }

        public void StopTracking(KandraTrisCuller culler) {
            _trackedCullersToDispose.Remove(culler);
        }

        public void StartTracking(KandraTrisCullee cullee) {
            _trackedCulleesToDispose.Add(cullee);
        }

        public void StopTracking(KandraTrisCullee cullee) {
            _trackedCulleesToDispose.Remove(cullee);
        }

        // == Queries
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInvalidId(uint renderingId) {
            return (renderingId & InvalidBitmask) == InvalidBitmask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWaitingId(uint renderingId) {
            return (renderingId & WaitingBitmask) == WaitingBitmask;
        }

        public bool IsRegistered(uint renderingId) {
            if (IsInvalidId(renderingId)) {
                return false;
            }

            if (IsWaitingId(renderingId)) {
                return false;
            }

            var uSlot = USlot(renderingId);
            return _fullyRegisteredSlots[uSlot];
        }

        public void GetBoundsAndRootBone(uint renderingId, out float4 worldBoundingSphere, out float4x4 rootBoneMatrix) {
            var uSlot = USlot(renderingId);
            worldBoundingSphere.x = VisibilityCullingManager.xs[uSlot];
            worldBoundingSphere.y = VisibilityCullingManager.ys[uSlot];
            worldBoundingSphere.z = VisibilityCullingManager.zs[uSlot];
            worldBoundingSphere.w = VisibilityCullingManager.radii[uSlot];
            rootBoneMatrix = VisibilityCullingManager.rootBones[uSlot];
        }

        public bool IsCameraVisible(uint renderingId) {
            var uSlot = USlot(renderingId);
            return SkinnedBatchRenderGroup.cameraSplitMaskVisibility[uSlot] != 0;
        }

        public bool IsShadowVisible(uint renderingId) {
            var uSlot = USlot(renderingId);
            return SkinnedBatchRenderGroup.lightsAggregatedSplitMaskVisibility[uSlot] != 0;
        }

        public bool IsAnyVisible(uint renderingId) {
            var uSlot = USlot(renderingId);
            return SkinnedBatchRenderGroup.cameraSplitMaskVisibility[uSlot] + SkinnedBatchRenderGroup.lightsAggregatedSplitMaskVisibility[uSlot] != 0;
        }

        public bool TryGetInstanceData(KandraRenderer kandraRenderer, out SkinnedBatchRenderGroup.InstanceData instanceData) {
            instanceData = default;

            var renderingId = kandraRenderer.RenderingId;
            if (IsInvalidId(renderingId)) {
                return false;
            }

            var uSlot = USlot(kandraRenderer.RenderingId);
            if (_toRegister[uSlot]) {
                return false;
            }

            instanceData.instanceStartVertex = SkinningManager.GetVertexStart(uSlot);
            instanceData.sharedStartVertex = MeshManager.GetMeshMemory(kandraRenderer.rendererData.mesh).verticesMemory.start;
            return true;
        }

        void FinalizeRegistration() {
            _toRegister.ToIndicesOfOneArray(ARAlloc.Temp, out var toRegisterIndices);
            _toRegister.Zero();

            foreach (var toRegister in toRegisterIndices) {
                var renderer = _renderers[toRegister];
                ref readonly var registerData = ref renderer.rendererData;
                var rendererFilterSettings = registerData.filteringSettings;

                var filterSettings = new SkinnedBatchRenderGroup.FilterSettings(rendererFilterSettings, renderer);

                // -- Check if there is a space
                // ReSharper disable once ReplaceWithSingleAssignment.True
                var haveSpaceForRenderer = true;
                var errorMessage = _alreadyMessagedBrokenKandraIds.Contains(toRegister) ? null : string.Empty;

                if (!RigManager.CanRegister(registerData.rig, out var rigRegion, ref errorMessage)) {
                    haveSpaceForRenderer = false;
                }
                if (!MeshManager.CanRegister(registerData.mesh, out var meshRegion, ref errorMessage)) {
                    haveSpaceForRenderer = false;
                }
                if (!BonesManager.CanRegister(registerData.bones, out var bonesRegion, ref errorMessage)) {
                    haveSpaceForRenderer = false;
                }
                if (!BlendshapesManager.CanRegister(registerData.mesh, out var blendshapesRegions, ref errorMessage)) {
                    haveSpaceForRenderer = false;
                }
                if (!SkinningManager.CanRegister(meshRegion.verticesMemory, out var skinnedVerticesRegion, ref errorMessage)) {
                    haveSpaceForRenderer = false;
                }

                if (!haveSpaceForRenderer) {
                    blendshapesRegions.Dispose();
                    _toRegister.Up(toRegister);
                    if (_alreadyMessagedBrokenKandraIds.Add(toRegister)) {
                        BrokenKandraMessage.OutOfMemory(errorMessage, renderer);
                    }
                    continue;
                }

                renderer.EnsureMesh();

                // -- Final registration
                RigManager.RegisterRig(registerData.rig, rigRegion);
                MeshManager.RegisterMesh(registerData.mesh, meshRegion);
                BonesManager.Register(toRegister, registerData.bones, bonesRegion, rigRegion, meshRegion.bindPosesMemory);
                BlendshapesManager.Register(toRegister, registerData.mesh, registerData.blendshapeWeights, blendshapesRegions);
                blendshapesRegions.Dispose();
                SkinningManager.Register(toRegister, skinnedVerticesRegion, meshRegion.verticesMemory, bonesRegion.start);
                SkinnedBatchRenderGroup.Register(toRegister, registerData.RenderingMesh, registerData.RenderingMaterials, skinnedVerticesRegion.start, meshRegion.verticesMemory.start, filterSettings);
                VisibilityCullingManager.Register(toRegister, registerData, 1u << filterSettings.layer
#if UNITY_EDITOR
                    , filterSettings.sceneCullingMask
#endif
                    );
                AnimatorManager.RegisterAnimator(toRegister, registerData.rig.animator);

                var materials = registerData.RenderingMaterials;
                var materialsIds = new UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>((uint)materials.Length, ARAlloc.Persistent);
                for (var i = 0u; i < materials.Length; i++) {
                    var materialId = MipmapsStreamingMasterMaterials.Instance.AddMaterial(materials[i]);
                    materialsIds[i] = materialId;
                }
                _materialsIds[toRegister] = materialsIds;
                _reciprocalUvDistributions[toRegister] = registerData.mesh.reciprocalUvDistribution;
                _fullyRegisteredSlots.Up(toRegister);
                renderer.RenderingId = toRegister;
                // -- Final registration
            }

            toRegisterIndices.Dispose();
        }

        void FinalizeUnregistration() {
            _toUnregister.ToIndicesOfOneArray(ARAlloc.Temp, out var toUnregisterIndices);
            _toUnregister.Zero();

            foreach (var toUnregister in toUnregisterIndices) {
                UnregisterRendererMarker.Begin();
                var renderer = _renderers[toUnregister];

                var registerData = renderer.rendererData;
                _fullyRegisteredSlots.Down(toUnregister);
                _submittedSlots.Down(toUnregister);

                var materialsIds = _materialsIds[toUnregister];
                for (var i = 0u; i < materialsIds.Length; i++) {
                    MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(materialsIds[i]);
                }
                materialsIds.Dispose();
                _materialsIds[toUnregister] = default;

                RigManager.UnregisterRig(registerData.rig);
                MeshManager.UnregisterMesh(registerData.mesh);
                BonesManager.Unregister(toUnregister);
                BlendshapesManager.Unregister(toUnregister, registerData.mesh);
                SkinningManager.Unregister(toUnregister);
                SkinnedBatchRenderGroup.Unregister(toUnregister);
                VisibilityCullingManager.Unregister(toUnregister);
                AnimatorManager.UnregisterAnimator(toUnregister);

                // --- Dispose after unregistering
                if (!renderer.rendererData.culledMesh.IsValid) {
                    renderer.ReleaseOriginalMesh();
                }
                if (renderer.Destroyed) {
                    renderer.Dispose();
                }
                // --- Dispose after unregistering
                var oldId = renderer.RenderingId;

                renderer.RenderingId = toUnregister | InvalidBitmask;
                _renderers[toUnregister] = default;
#if UNITY_EDITOR
                _editorRendererInstanceIds[toUnregister] = 0;
#endif

                if ((oldId & UnregisterToWaitingBitmask) == UnregisterToWaitingBitmask) {
                    Register(renderer);
                } else {
                    _alreadyMessagedBrokenKandraIds.Remove(toUnregister);
                }

#if UNITY_EDITOR || AR_DEBUG
                _toUnregisterAutomatic.Down(toUnregister);
#endif

                UnregisterRendererMarker.End();
            }

            toUnregisterIndices.Dispose();
        }

        void OnTimeUpdateBegin() {
            TimeUpdateMarker.Begin();

            StreamingManager.OnFrameEnd();

            TimeUpdateMarker.End();
        }

        void OnEarlyUpdateBegin() {
            EarlyUpdateMarker.Begin();

            FinalizeUnregistration();
            FinalizeRegistration();

            EarlyUpdateMarker.End();
        }

        void OnPreLateUpdateBegin() {
            PreLateUpdateMarker.Begin();

            if (enabled) {
                AnimatorManager.UpdateAnimators(_fullyRegisteredSlots, _toUnregister);
            }

            PreLateUpdateMarker.End();
        }

        void OnPreLateUpdateEnd() {
            AfterAnimatorMarker.Begin();

            if (enabled) {
                MagicaManager.Cloth?.StartClothUpdate();
                AnimationPostProcessingService.BeginJob();
                RigManager.CollectBoneMatrices();
                VisibilityCullingManager.CollectCullingData(_fullyRegisteredSlots);
                JobHandle.ScheduleBatchedJobs();
            }

            AfterAnimatorMarker.End();
        }

        void OnBeginRendering(ScriptableRenderContext context, List<Camera> __) {
            BeginRenderingMarker.Begin();

            if (enabled) {
                _commandBuffer.Clear();
                _commandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

                RigManager.EnsureBuffers(_commandBuffer);
                MeshBroker.EnsureBuffers(_commandBuffer);
                BonesManager.EnsureBuffers(_commandBuffer);
                MeshManager.EnsureBuffers(_commandBuffer);
                SkinningManager.EnsureBuffers(_commandBuffer);
                BlendshapesManager.EnsureBuffers(_commandBuffer);

                SkinningManager.RunCopyPrevious(_commandBuffer);

                RigManager.UnlockBuffer(_commandBuffer);
                BonesManager.RunComputeShader(_commandBuffer);
                BlendshapesManager.UpdateBlendshapes(_fullyRegisteredSlots);
                SkinningManager.RunSkinning(_commandBuffer);
                VisibilityCullingManager.collectCullingDataJobHandle.Complete();

                context.ExecuteCommandBuffer(_commandBuffer);
            }

            for (int i = _trackedRenderersToDispose.Count - 1; i >= 0; i--) {
                var rendererToCheck = _trackedRenderersToDispose[i];
                if (rendererToCheck == null) {
                    _trackedRenderersToDispose.RemoveAt(i);
                    rendererToCheck.Dispose();
                }
            }

            for (int i = _trackedCullersToDispose.Count - 1; i >= 0; i--) {
                var cullerToChek = _trackedCullersToDispose[i];
                if (cullerToChek == null) {
                    _trackedCullersToDispose.RemoveAt(i);
                    cullerToChek.OnDestroy();
                }
            }

            for (int i = _trackedCulleesToDispose.Count - 1; i >= 0; i--) {
                var culleeToCheck = _trackedCulleesToDispose[i];
                if (culleeToCheck == null) {
                    _trackedCulleesToDispose.RemoveAt(i);
                    culleeToCheck.OnDestroy();
                }
            }

            BeginRenderingMarker.End();
        }

        public static uint USlot(uint slot) {
            CheckSlotId(slot);
            return (slot & ValidBitmask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Conditional("UNITY_EDITOR"), Conditional("AR_DEBUG")]
        static void CheckSlotId(uint slot) {
            if (IsInvalidId(slot)) {
                throw new ArgumentException($"Wanted to use invalid Kandra with id {slot}");
            }
        }

        static int BufferSize(int defaultValue) {
            var defaultMultiplier =
#if UNITY_EDITOR
                (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode ? PlayModeBufferSizeMultiplier : EditModeBufferSizeMultiplier);
#else
                RuntimeBufferSizeMultiplier;
#endif
            defaultMultiplier = Configuration.GetFloat("kandra.buffer_size_multiplier", defaultMultiplier);
            return (int)(defaultValue * defaultMultiplier);
        }

#if UNITY_EDITOR
        void EDITOR_ExitPlaymode() {
            for (int i = _trackedRenderersToDispose.Count - 1; i >= 0; i--) {
                _trackedRenderersToDispose[i].Dispose();
            }
            _trackedRenderersToDispose.Clear();

            for (int i = _trackedCullersToDispose.Count - 1; i >= 0; i--) {
                _trackedCullersToDispose[i].OnDestroy();
            }
            _trackedCullersToDispose.Clear();

            for (int i = _trackedCulleesToDispose.Count - 1; i >= 0; i--) {
                _trackedCulleesToDispose[i].OnDestroy();
            }
            _trackedCulleesToDispose.Clear();

            KandraEditorGuards.EDITOR_ExitPlaymodeCleanup();

            foreach (var slot in _fullyRegisteredSlots.EnumerateOnes()) {
                _toUnregister.Up(slot);
                _renderers[slot].EDITOR_Force_Uninitialized = true;
            }
            FinalizeUnregistration();
        }
#endif

        // IMipmapsFactorProvider
        void MipmapsStreamingMasterMaterials.IMipmapsFactorProvider.ProvideMipmapsFactors(in CameraData cameraData, in MipmapsStreamingMasterMaterials.ParallelWriter writer) {
            var jobHandle = new KandraMipmapsFactorJob {
                cameraData = cameraData,
                takenSlots = _fullyRegisteredSlots,
                toUnregister = _toUnregister,
                xs = VisibilityCullingManager.xs,
                ys = VisibilityCullingManager.ys,
                zs = VisibilityCullingManager.zs,
                radii = VisibilityCullingManager.radii,
                rootBoneMatrices = VisibilityCullingManager.rootBones,
                reciprocalUvDistributions = _reciprocalUvDistributions,
                materialIndices = _materialsIds,

                outMipmapsStreamingWriter = writer,
            }.Schedule(FullyRegisteredRenderersLength, default);
            writer.Dispose(jobHandle);
        }

        [BurstCompile]
        struct KandraMipmapsFactorJob : IJobFor {
            public CameraData cameraData;
            [ReadOnly] public UnsafeBitmask takenSlots;
            [ReadOnly] public UnsafeBitmask toUnregister;
            [ReadOnly] public UnsafeArray<float> xs;
            [ReadOnly] public UnsafeArray<float> ys;
            [ReadOnly] public UnsafeArray<float> zs;
            [ReadOnly] public UnsafeArray<float> radii;
            [ReadOnly] public UnsafeArray<float4x4> rootBoneMatrices;
            [ReadOnly] public UnsafeArray<float> reciprocalUvDistributions;
            [ReadOnly] public UnsafeArray<UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>> materialIndices;

            public MipmapsStreamingMasterMaterials.ParallelWriter outMipmapsStreamingWriter;

            public void Execute(int index) {
                var uIndex = (uint)index;
                if (!takenSlots[uIndex] || toUnregister[uIndex]) {
                    return;
                }

                var position = new float3(xs[uIndex], ys[uIndex], zs[uIndex]);
                var radius = radii[uIndex];
                var scale = math.square(math.cmax(rootBoneMatrices[uIndex].Scale()));

                var factorFactor = MipmapsStreamingUtils.CalculateMipmapFactorFactor(cameraData, position, radius, scale);
                var fullFactor = reciprocalUvDistributions[uIndex] * factorFactor;
                var subMaterialIndices = materialIndices[uIndex];

                for (uint j = 0; j < subMaterialIndices.Length; j++) {
                    outMipmapsStreamingWriter.UpdateMipFactor(subMaterialIndices[j], fullFactor);
                }
            }
        }

        // IMainMemorySnapshotProvider
        public int PreallocationSize => 10_000;

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 10;
            var taken = 0;

            _fullyRegisteredSlots.GetMemorySnapshot("Taken slots", memoryBuffer.Slice(0, 1));
            taken += RigManager.GetMemorySnapshot(memoryBuffer[(childrenCount+taken)..], memoryBuffer.Slice(1, 1));
            taken += MeshManager.GetMemorySnapshot(memoryBuffer[(childrenCount+taken)..], memoryBuffer.Slice(2, 1));
            taken += AnimatorManager.GetMemorySnapshot(memoryBuffer[(childrenCount+taken)..], memoryBuffer.Slice(3, 1));
            taken += BonesManager.GetMemorySnapshot(memoryBuffer[(childrenCount+taken)..], memoryBuffer.Slice(4, 1));
            taken += SkinningManager.GetMemorySnapshot(memoryBuffer[(childrenCount+taken)..], memoryBuffer.Slice(5, 1));
            taken += BlendshapesManager.GetMemorySnapshot(memoryBuffer[(childrenCount+taken)..], memoryBuffer.Slice(6, 1));
            taken += VisibilityCullingManager.GetMemorySnapshot(memoryBuffer[(childrenCount+taken)..], memoryBuffer.Slice(7, 1));
            taken += SkinnedBatchRenderGroup.GetMemorySnapshot(memoryBuffer[(childrenCount+taken)..], memoryBuffer.Slice(8, 1));
            taken += MeshBroker.GetMemorySnapshot(memoryBuffer[(childrenCount+taken)..], memoryBuffer.Slice(9, 1));

            ownPlace.Span[0] = new MemorySnapshot("Kandra:", 0, 0, memoryBuffer[..childrenCount], $"[{RegisteredRenderers}/{FullyRegisteredRenderersLength}/{_fullyRegisteredSlots.ElementsLength}]");

            return childrenCount;
        }
    }
}