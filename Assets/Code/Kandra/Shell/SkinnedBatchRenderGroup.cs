using System;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    public unsafe class SkinnedBatchRenderGroup : IMemorySnapshotProvider {
        public UnsafeArray<ushort> cameraSplitMaskVisibility;
        public UnsafeArray<ushort> lightsSplitMaskVisibility;
        public UnsafeArray<ushort> lightsAggregatedSplitMaskVisibility;
        public bool enabled = true;

        public SkinnedBatchRenderGroup(VisibilityCullingManager visibilityCullingManager) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public void Register(uint slot, KandraRenderingMesh renderingMesh, Material[] materials,
            uint instanceStartVertex, uint sharedStartVertex, in FilterSettings filterSettings) {
            throw new NotImplementedException();
        }

        public void Unregister(uint slot) {
            throw new NotImplementedException();
        }

        public void UpdateSubmeshIndices(uint slot, KandraRenderingMesh renderingMesh) {
            throw new NotImplementedException();
        }

        public void UpdateMaterials(uint slot, Material[] materials, KandraRenderingMesh renderingMesh,
            in FilterSettings filterSettings) {
            throw new NotImplementedException();
        }

        public void UpdateFilterSettings(uint slot, in FilterSettings filterSettings) {
            throw new NotImplementedException();
        }

        public readonly struct FilterSettings : IEquatable<FilterSettings> {
            public readonly ulong sceneCullingMask;
            public readonly uint renderingLayerMask;
            public readonly ShadowCastingMode castShadows;
            public readonly byte layer;
            public readonly bool hasTransparency;

            public FilterSettings(uint renderingLayerMask, byte layer, ulong sceneCullingMask,
                ShadowCastingMode castShadows, bool hasTransparency) {
                throw new NotImplementedException();
            }

            public FilterSettings(KandraRenderer.RendererFilteringSettings rendererFilterSettings,
                KandraRenderer renderer) {
                throw new NotImplementedException();
            }

            public FilterSettings WithTransparency(bool hasTransparency) {
                throw new NotImplementedException();
            }

            public bool Equals(FilterSettings other) {
                throw new NotImplementedException();
            }

            public override int GetHashCode() {
                throw new NotImplementedException();
            }
        }

        public struct InstanceData {
            public uint instanceStartVertex;
            public uint sharedStartVertex;
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }
    }
}