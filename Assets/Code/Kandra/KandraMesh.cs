using System.Diagnostics;
using Awaken.Kandra.Data;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Kandra {
    public class KandraMesh : ScriptableObject {
        // -- Always data
        public Bounds meshLocalBounds;
        public float4 localBoundingSphere;
        public SubmeshData[] submeshes;
        public string[] blendshapesNames;

        public ushort vertexCount;
        public uint indicesCount;
        public ushort bindposesCount;
        public float reciprocalUvDistribution;

        public string archive;
        
        public unsafe Data ReadSerializedData(UnsafeArray<byte>.Span serializedData) {
            CheckSerializedDataLength(serializedData.Length);

            var data = new Data();
            var ptr = serializedData.Ptr;

            data.vertices = UnsafeArray<CompressedVertex>.FromExistingData((CompressedVertex*)ptr, vertexCount);
            ptr += vertexCount * sizeof(CompressedVertex);

            data.additionalData = UnsafeArray<AdditionalVertexData>.FromExistingData((AdditionalVertexData*)ptr, vertexCount);
            ptr += vertexCount * sizeof(AdditionalVertexData);

            data.boneWeights = UnsafeArray<PackedBonesWeights>.FromExistingData((PackedBonesWeights*)ptr, vertexCount);
            ptr += vertexCount * sizeof(PackedBonesWeights);

            data.bindposes = UnsafeArray<float3x4>.FromExistingData((float3x4*)ptr, bindposesCount);
            ptr += bindposesCount * sizeof(float3x4);

            return data;
        }

        public unsafe UnsafeArray<Blendshape> ReadBlendshapesData(UnsafeArray<byte>.Span serializedData, Allocator allocator) {
            CheckSerializedDataLength(serializedData.Length);

            var ptr = serializedData.Ptr;
            ptr += vertexCount * sizeof(CompressedVertex);
            ptr += vertexCount * sizeof(AdditionalVertexData);
            ptr += vertexCount * sizeof(PackedBonesWeights);
            ptr += bindposesCount * sizeof(float3x4);

            var blendshapesData = new UnsafeArray<Blendshape>((uint)blendshapesNames.Length, allocator);
            for (var i = 0u; i < blendshapesNames.Length; i++) {
                blendshapesData[i].data = UnsafeArray<PackedBlendshapeDatum>.FromExistingData((PackedBlendshapeDatum*)ptr, vertexCount);
                ptr += vertexCount * sizeof(PackedBlendshapeDatum);
            }

            return blendshapesData;
        }

        [Conditional("UNITY_EDITOR")]
        unsafe void CheckSerializedDataLength(uint serializedDataSize) {
            var verticesSize = vertexCount * sizeof(CompressedVertex);
            var additionalDataSize = vertexCount * sizeof(AdditionalVertexData);
            var boneWeightsSize = vertexCount * sizeof(PackedBonesWeights);
            var bindposesSize = bindposesCount * sizeof(float3x4);
            int blendshapesSize = 0;
            for (var i = 0; i < blendshapesNames.Length; i++) {
                blendshapesSize += vertexCount * sizeof(PackedBlendshapeDatum);
            }

            var expectedSize = (uint)(verticesSize + additionalDataSize + boneWeightsSize + bindposesSize + blendshapesSize);
            if (serializedDataSize != expectedSize) {
                BrokenKandraMessage.DataMismatch(this, expectedSize, serializedDataSize);
            }
        }

        public struct Data {
            public UnsafeArray<CompressedVertex>.Span vertices;
            public UnsafeArray<AdditionalVertexData>.Span additionalData;
            public UnsafeArray<PackedBonesWeights>.Span boneWeights;

            public UnsafeArray<float3x4>.Span bindposes;
        }
    }
}