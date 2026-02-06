using System;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Awaken.Kandra.Managers {
    public class VisibilityCullingManager : IMemorySnapshotProvider {
        public UnsafeArray<float4x4> rootBones;
        public UnsafeArray<float> xs;
        public UnsafeArray<float> ys;
        public UnsafeArray<float> zs;
        public UnsafeArray<float> radii;
        public UnsafeArray<uint> layerMasks;
        public UnsafeArray<ulong> sceneCullingMasks;
        public JobHandle collectCullingDataJobHandle;
        public uint PossibleLayers;
        public ulong PossibleSceneCullingLayers;

        public VisibilityCullingManager() {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public void Register(uint slot, in KandraRenderer.RendererData rendererData, uint layerMask,
            ulong sceneCullingMask) {
            throw new NotImplementedException();
        }

        public void Unregister(uint slot) {
            throw new NotImplementedException();
        }

        public void CollectCullingData(UnsafeBitmask takenSlots) {
            throw new NotImplementedException();
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }
    }
}