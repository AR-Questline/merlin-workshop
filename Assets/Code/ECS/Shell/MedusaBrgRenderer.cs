using System;
using System.Collections.Generic;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Graphics.Mipmaps;
using Awaken.Utility.LowLevel.Collections;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.ECS.MedusaRenderer {
    public class MedusaBrgRenderer : MipmapsStreamingMasterMaterials.IMipmapsFactorProvider, IMemorySnapshotProvider {
        public MedusaBrgRenderer(int count, string sceneName) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public void SetTransforms(int count) {
            throw new NotImplementedException();
        }

        public void SetRenderers(Renderer[] renderers, uint flatTransformsCount,
            uint flatReciprocalUvDistributionsCount) {
            throw new NotImplementedException();
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }

        public readonly struct EditorAccess {
            readonly MedusaBrgRenderer _renderer;
            public bool IsNull => throw new NotImplementedException();
            public bool IsNotNull => throw new NotImplementedException();
            public bool IsValid => throw new NotImplementedException();
            public UnsafeArray<float>.Span Xs => throw new NotImplementedException();
            public UnsafeArray<float>.Span Ys => throw new NotImplementedException();
            public UnsafeArray<float>.Span Zs => throw new NotImplementedException();
            public UnsafeArray<float>.Span Radii => throw new NotImplementedException();
            public UnsafeArray<byte>.Span LastLodMasks => throw new NotImplementedException();
            public UnsafeArray<byte> LodVisibility => throw new NotImplementedException();
            public UnsafeArray<ushort> SplitVisibilityMask => throw new NotImplementedException();
            public string BasePath => throw new NotImplementedException();
#if UNITY_EDITOR
            public BatchCullingOutputDebugData BatchCullingOutputDebugData => throw new NotImplementedException();
#endif

            public bool CollectBatchCullingOutputDebugData {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            public EditorAccess(MedusaBrgRenderer renderer) {
                throw new NotImplementedException();
            }

            public float LodDistanceSq(uint transform, byte lod) {
                throw new NotImplementedException();
            }

            public UnsafeArray<uint>.Span TransformIndices(uint renderer) {
                throw new NotImplementedException();
            }
        }

        public void ProvideMipmapsFactors(in CameraData cameraData,
            in MipmapsStreamingMasterMaterials.ParallelWriter writer) {
            throw new NotImplementedException();
        }
    }

    public struct Renderer : IEquatable<Renderer> {
        public List<RenderDatum> renderData;
        public byte lodMask;
        public uint instancesCount;

        public bool Equals(Renderer other) {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() {
            throw new NotImplementedException();
        }

        public static bool operator ==(Renderer left, Renderer right) {
            throw new NotImplementedException();
        }

        public static bool operator !=(Renderer left, Renderer right) {
            throw new NotImplementedException();
        }
    }

    public struct RenderDatum : IEquatable<RenderDatum> {
        public Mesh mesh;
        public Material material;
        public ushort subMeshIndex;

        public bool Equals(RenderDatum other) {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() {
            throw new NotImplementedException();
        }

        public static bool operator ==(RenderDatum left, RenderDatum right) {
            throw new NotImplementedException();
        }

        public static bool operator !=(RenderDatum left, RenderDatum right) {
            throw new NotImplementedException();
        }
    }
}