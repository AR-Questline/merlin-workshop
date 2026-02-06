using System;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    public unsafe class RigManager : IMemorySnapshotProvider {
        private static readonly int InputBonesId;
        public float FillPercentage;
        private MemoryBookkeeper _memoryRegions;

        public RigManager(ComputeShader prepareBonesShader) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public bool CanRegister(KandraRig rig, out MemoryBookkeeper.MemoryRegion memoryDestination,
            ref string errorMessage) {
            throw new NotImplementedException();
        }

        public void RegisterRig(KandraRig rig, in MemoryBookkeeper.MemoryRegion memoryDestination) {
            throw new NotImplementedException();
        }

        public void UnregisterRig(KandraRig rig) {
            throw new NotImplementedException();
        }

        public bool CanChange(KandraRig rig, out MemoryBookkeeper.MemoryRegion memoryDestination) {
            throw new NotImplementedException();
        }

        public void RigChanged(KandraRig rig, in MemoryBookkeeper.MemoryRegion memoryDestination) {
            throw new NotImplementedException();
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            throw new NotImplementedException();
        }

        public void CollectBoneMatrices() {
            throw new NotImplementedException();
        }

        public void UnlockBuffer(CommandBuffer commandBuffer) {
            throw new NotImplementedException();
        }

        public void AddRigToTrack(KandraRig kandraRig) {
            throw new NotImplementedException();
        }

        public void StopRigTracking(KandraRig kandraRig) {
            throw new NotImplementedException();
        }

        public bool TryGetMemoryRegionFor(KandraRig rig, out MemoryBookkeeper.MemoryRegion region) {
            throw new NotImplementedException();
        }

        public ulong GetMemoryUsageFor(KandraRig rig) {
            throw new NotImplementedException();
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }

        public readonly struct EditorAccess {
            readonly RigManager _manager;

            public ref readonly MemoryBookkeeper BonesMemory => ref _manager._memoryRegions;

            public EditorAccess(RigManager manager) {
                _manager = manager;
            }

            public static EditorAccess Get() {
                return new EditorAccess(KandraRendererManager.Instance.RigManager);
            }
        }
    }
}