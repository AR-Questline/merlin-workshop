using System;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    public class BlendshapesManager : IMemorySnapshotProvider
    {
        public float FillPercentage;
        private MemoryBookkeeper _blendshapesMemory;
        private UnsafeArray<UnsafeArray<uint>> _indices;
        private UnsafeArray<UnsafeArray<float>.Span> _weights;
        private UnsafeHashMap<int, BlendshapesData> _blendshapes;

        public BlendshapesManager(ComputeShader skinningShader) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public bool CanRegister(KandraMesh mesh, out UnsafeArray<MemoryBookkeeper.MemoryRegion> memoryDestinations, ref string errorMessage) {
            throw new NotImplementedException();
        }

        public void Register(uint slot, KandraMesh mesh, UnsafeArray<float>.Span rendererWeights, UnsafeArray<MemoryBookkeeper.MemoryRegion> memoryDestinations) {
            throw new NotImplementedException();
        }

        public void Unregister(uint slot, KandraMesh mesh) {
            throw new NotImplementedException();
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            throw new NotImplementedException();
        }

        public void UpdateBlendshapes(UnsafeBitmask takenSlots) {
            throw new NotImplementedException();
        }

        public bool TryGetBlendshapesData(KandraMesh mesh, out UnsafeArray<MemoryBookkeeper.MemoryRegion> data) {
            throw new NotImplementedException();
        }

        public ulong GetMemoryUsageFor(KandraMesh mesh) {
            throw new NotImplementedException();
        }

        public struct BlendshapesData {
            public readonly UnsafeArray<MemoryBookkeeper.MemoryRegion> blendshapesMemory;
            public int refCount;

            public readonly uint Length;

            public BlendshapesData(UnsafeArray<MemoryBookkeeper.MemoryRegion> memory) {
                throw new NotImplementedException();
            }

            public void Dispose() {
                throw new NotImplementedException();
            }
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }

        public readonly struct EditorAccess {
            readonly BlendshapesManager _manager;
            public ref readonly MemoryBookkeeper BlendshapesMemory => ref _manager._blendshapesMemory;
            public ref readonly UnsafeArray<UnsafeArray<uint>> Indices => ref _manager._indices;
            public ref readonly UnsafeArray<UnsafeArray<float>.Span> Weights => ref _manager._weights;
            public ref readonly UnsafeHashMap<int, BlendshapesData> Blendshapes => ref _manager._blendshapes;

            public EditorAccess(BlendshapesManager manager) {
                throw new NotImplementedException();
            }

            public static EditorAccess Get() => throw new NotImplementedException();
        }
    }
}