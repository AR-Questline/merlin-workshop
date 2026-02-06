using System;
using System.Collections.Generic;
using Awaken.Kandra.Managers;
using Awaken.Kandra.VFXs;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Graphics.Mipmaps;
using Awaken.Utility.LowLevel.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Kandra {
    public class KandraRendererManager : MipmapsStreamingMasterMaterials.IMipmapsFactorProvider,
        IMainMemorySnapshotProvider {
        public const int RenderersCapacity = 1_000;
        public const int RigBonesCapacity = 6_400;
        public const int UniqueMeshesCapacity = 50;
        public const int UniqueBindposesCapacity = 2_500;
        public const int UniqueVerticesCapacity = 750_000;
        public const int IndicesCapacity = 3_850_000;
        public const int SkinBonesCapacity = 13_000;
        public const int BlendshapesCapacity = 2_500;
        public const int BlendshapesDeltasCapacity = 2_750_000;
        public const int SkinnedVerticesCapacity = 2_750_000;
        public const uint InvalidBitmask = 1u << 31;
        public const uint WaitingBitmask = 1u << 30;
        public const uint UnregisterToWaitingBitmask = 1u << 29;
        public const uint MetaBitmask = InvalidBitmask | WaitingBitmask | UnregisterToWaitingBitmask;
        public const uint MaxRenderers = ~(MetaBitmask);
        public const uint ValidBitmask = ~(MetaBitmask);

        public static KandraRendererManager Instance { get; private set; }

        public bool enabled;

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

        public uint RegisteredRenderers;
        public int FullyRegisteredRenderersLength;

        public KandraRenderer[] ActiveRenderers;
        KandraRenderer[] _renderers;
        public UnsafeBitmask FullyRegisteredSlots;
        public UnsafeBitmask ToUnregister;

        public UnsafeArray<int> EditorRendererInstanceIds;
        UnsafeBitmask _toRegister;
        public static int FinalRenderersCapacity;
        public static int FinalRigBonesCapacity;
        public static int FinalUniqueMeshesCapacity;
        public static int FinalUniqueBindposesCapacity;
        public static int FinalUniqueVerticesCapacity;
        public static int FinalIndicesCapacity;
        public static int FinalSkinBonesCapacity;
        public static int FinalBlendshapesCapacity;
        public static int FinalBlendshapesDeltasCapacity;
        public static int FinalSkinnedVerticesCapacity;

        public static void Init() {
            throw new NotImplementedException();
        }

        KandraRendererManager() {
            throw new NotImplementedException();
        }

        public void Register(KandraRenderer kandraRenderer) {
            throw new NotImplementedException();
        }

        public void Unregister(KandraRenderer kandraRenderer) {
            throw new NotImplementedException();
        }

        public void RigChanged(KandraRig kandraRig, List<KandraRenderer> renderers) {
            throw new NotImplementedException();
        }

        public void UpdateSubmeshIndices(uint renderingId, KandraRenderingMesh renderingMesh) {
            throw new NotImplementedException();
        }

        public void UpdateRenderingMaterials(uint renderingId, Material[] materials,
            KandraRenderingMesh renderingMesh) {
            throw new NotImplementedException();
        }

        public void UpdateFilterSettings(uint renderingId) {
            throw new NotImplementedException();
        }

        public void UpdateMipmapsStreaming(uint renderingId) {
            throw new NotImplementedException();
        }

        public void StartTracking(KandraRenderer kandraRenderer) {
            throw new NotImplementedException();
        }

        public void StopTracking(KandraRenderer kandraRenderer) {
            throw new NotImplementedException();
        }

        public void StartTracking(KandraTrisCuller culler) {
            throw new NotImplementedException();
        }

        public void StopTracking(KandraTrisCuller culler) {
            throw new NotImplementedException();
        }

        public void StartTracking(KandraTrisCullee cullee) {
            throw new NotImplementedException();
        }

        public void StopTracking(KandraTrisCullee cullee) {
            throw new NotImplementedException();
        }

        public static bool IsInvalidId(uint renderingId) {
            throw new NotImplementedException();
        }

        public static bool IsWaitingId(uint renderingId) {
            throw new NotImplementedException();
        }

        public bool IsRegistered(uint renderingId) {
            throw new NotImplementedException();
        }

        public void GetBoundsAndRootBone(uint renderingId, out float4 worldBoundingSphere,
            out float4x4 rootBoneMatrix) {
            throw new NotImplementedException();
        }

        public bool IsCameraVisible(uint renderingId) {
            throw new NotImplementedException();
        }

        public bool IsShadowVisible(uint renderingId) {
            throw new NotImplementedException();
        }

        public bool IsAnyVisible(uint renderingId) {
            throw new NotImplementedException();
        }

        public bool TryGetInstanceData(KandraRenderer kandraRenderer,
            out SkinnedBatchRenderGroup.InstanceData instanceData) {
            throw new NotImplementedException();
        }

        public static uint USlot(uint slot) {
            throw new NotImplementedException();
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }

        public void ProvideMipmapsFactors(in CameraData cameraData,
            in MipmapsStreamingMasterMaterials.ParallelWriter writer) {
            throw new NotImplementedException();
        }

        public int PreallocationSize => throw new NotImplementedException();
    }
}