using System;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    public class BonesManager : IMemorySnapshotProvider
    {
        public float FillPercentage;
        private MemoryBookkeeper _memoryRegions;

        public BonesManager(ComputeShader skinningShader, ComputeShader prepareBonesShader) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public bool CanRegister(ushort[] boneIndices, out MemoryBookkeeper.MemoryRegion memoryDestination, ref string errorMessage) {
            throw new NotImplementedException();
        }

        public void Register(uint slot, ushort[] boneIndices, in MemoryBookkeeper.MemoryRegion rendererBonesRegion, in MemoryBookkeeper.MemoryRegion rigMemory, in MemoryBookkeeper.MemoryRegion bindPosesMemory) {
            throw new NotImplementedException();
        }

        public void Unregister(uint slot) {
            throw new NotImplementedException();
        }

        public void RigChanged(uint slot, ushort[] bones, MemoryBookkeeper.MemoryRegion rigRegion, MemoryBookkeeper.MemoryRegion meshRegionBindPosesMemory) {
            throw new NotImplementedException();
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            throw new NotImplementedException();
        }

        public void RunComputeShader(CommandBuffer commandBuffer) {
            throw new NotImplementedException();
        }

        public bool TryGetBonesMemory(uint slot, out MemoryBookkeeper.MemoryRegion memory) {
            throw new NotImplementedException();
        }

        public ulong GetMemoryUsageFor(uint slot) {
            throw new NotImplementedException();
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }

        public readonly struct EditorAccess {
            readonly BonesManager _manager;

            public ref readonly MemoryBookkeeper SkinBonesMemory => ref _manager._memoryRegions;

            public EditorAccess(BonesManager manager) {
                throw new NotImplementedException();
            }

            public static EditorAccess Get() {
                throw new NotImplementedException();
            }
        }
    }
}