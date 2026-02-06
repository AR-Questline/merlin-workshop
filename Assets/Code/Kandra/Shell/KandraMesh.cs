using System;
using Awaken.Kandra.Data;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Kandra {
    public class KandraMesh : ScriptableObject {
        public Bounds meshLocalBounds;
        public float4 localBoundingSphere;
        public SubmeshData[] submeshes = Array.Empty<SubmeshData>();
        public string[] blendshapesNames = Array.Empty<string>();
        public ushort vertexCount;
        public uint indicesCount;
        public ushort bindposesCount;
        public float reciprocalUvDistribution;
        public string modDirectory;
        public string Name => throw new NotImplementedException();

        public Data ReadSerializedData(UnsafeArray<byte>.Span serializedData) {
            throw new NotImplementedException();
        }

        public UnsafeArray<Blendshape> ReadBlendshapesData(UnsafeArray<byte>.Span serializedData, Allocator allocator) {
            throw new NotImplementedException();
        }

        public struct Data {
            public UnsafeArray<CompressedVertex>.Span vertices;
            public UnsafeArray<AdditionalVertexData>.Span additionalData;
            public UnsafeArray<PackedBonesWeights>.Span boneWeights;
            public UnsafeArray<float3x4>.Span bindposes;
        }
    }
}