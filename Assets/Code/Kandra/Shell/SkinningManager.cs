using System;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    public class SkinningManager : IMemorySnapshotProvider {
        public float FillPercentage;
        public GraphicsBuffer OutputVerticesBuffer;
        private MemoryBookkeeper _memoryRegions;

        public SkinningManager(ComputeShader skinningShader) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public bool CanRegister(in MemoryBookkeeper.MemoryRegion meshMemory,
            out MemoryBookkeeper.MemoryRegion memoryDestination, ref string errorMessage) {
            throw new NotImplementedException();
        }

        public void Register(uint slot, in MemoryBookkeeper.MemoryRegion rendererRegion,
            in MemoryBookkeeper.MemoryRegion meshMemory, uint bonesOffset) {
            throw new NotImplementedException();
        }

        public void Unregister(uint slot) {
            throw new NotImplementedException();
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            throw new NotImplementedException();
        }

        public void RunCopyPrevious(CommandBuffer commandBuffer) {
            throw new NotImplementedException();
        }

        public void RunSkinning(CommandBuffer commandBuffer) {
            throw new NotImplementedException();
        }

        public uint GetVertexStart(uint slot) {
            throw new NotImplementedException();
        }

        public bool TryGetSkinnedVerticesMemory(uint slot, out MemoryBookkeeper.MemoryRegion memory) {
            throw new NotImplementedException();
        }

        public ulong GetMemoryUsageFor(uint slot) {
            throw new NotImplementedException();
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }

        public readonly struct EditorAccess {
            readonly SkinningManager _manager;

            public ref readonly MemoryBookkeeper SkinVertsMemory => ref _manager._memoryRegions;

            public EditorAccess(SkinningManager manager) {
                throw new NotImplementedException();
            }

            public static EditorAccess Get() {
                throw new NotImplementedException();
            }
        }
    }
}