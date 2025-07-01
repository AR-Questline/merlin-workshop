// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class VirtualMesh
    {
        /// <summary>
        /// PreBuildの共有部分データ
        /// </summary>
        [System.Serializable]
        public class ShareSerializationData
        {
            public string name;
            public MeshType meshType;
            public bool isBoneCloth;

            // 基本
            public ExSimpleNativeArray<int>.SerializationData referenceIndices;
            public ExSimpleNativeArray<VertexAttribute>.SerializationData attributes;
            public ExSimpleNativeArray<float3>.SerializationData localPositions;
            public ExSimpleNativeArray<float3>.SerializationData localNormals;
            public ExSimpleNativeArray<float3>.SerializationData localTangents;
            public ExSimpleNativeArray<float2>.SerializationData uv;
            public ExSimpleNativeArray<VirtualMeshBoneWeight>.SerializationData boneWeights;
            public ExSimpleNativeArray<int3>.SerializationData triangles;
            public ExSimpleNativeArray<int2>.SerializationData lines;
            public int centerTransformIndex;
            public float4x4 initLocalToWorld;
            public float4x4 initWorldToLocal;
            public quaternion initRotation;
            public quaternion initInverseRotation;
            public float3 initScale;
            public int skinRootIndex;
            public ExSimpleNativeArray<int>.SerializationData skinBoneTransformIndices;
            public ExSimpleNativeArray<float4x4>.SerializationData skinBoneBindPoses;
            public TransformData.ShareSerializationData transformData;
            public AABB boundingBox;
            public float averageVertexDistance;
            public float maxVertexDistance;

            // プロキシメッシュ
            public byte[] vertexToTriangles;
            public byte[] vertexToVertexIndexArray;
            public byte[] vertexToVertexDataArray;
            public byte[] edges;
            public byte[] edgeFlags;
            public int2[] edgeToTrianglesKeys;
            public ushort[] edgeToTrianglesValues;
            public byte[] vertexBindPosePositions;
            public byte[] vertexBindPoseRotations;
            public byte[] vertexToTransformRotations;
            public byte[] vertexDepths;
            public byte[] vertexRootIndices;
            public byte[] vertexParentIndices;
            public byte[] vertexChildIndexArray;
            public byte[] vertexChildDataArray;
            public byte[] vertexLocalPositions;
            public byte[] vertexLocalRotations;
            public byte[] normalAdjustmentRotations;
            public byte[] baseLineFlags;
            public byte[] baseLineStartDataIndices;
            public byte[] baseLineDataCounts;
            public byte[] baseLineData;
            public int[] customSkinningBoneIndices;
            public ushort[] centerFixedList;
            public float3 localCenterPosition;

            // マッピングメッシュ
            public float3 centerWorldPosition;
            public quaternion centerWorldRotation;
            public float3 centerWorldScale;
            public float4x4 toProxyMatrix;
            public quaternion toProxyRotation;

            public override string ToString()
            {
                return default;
            }
        }

        public ShareSerializationData ShareSerialize()
        {
            return default;
        }

        public static VirtualMesh ShareDeserialize(ShareSerializationData sdata)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// PreBuildの固有部分データ
        /// </summary>
        [System.Serializable]
        public class UniqueSerializationData : ITransform
        {
            public TransformData.UniqueSerializationData transformData;

            public void GetUsedTransform(HashSet<Transform> transformSet)
            {
            }

            public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
            {
            }
        }

        public UniqueSerializationData UniqueSerialize()
        {
            return default;
        }
    }
}
