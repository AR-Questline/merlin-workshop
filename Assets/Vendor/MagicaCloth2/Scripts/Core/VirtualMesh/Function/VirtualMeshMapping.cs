// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class VirtualMesh
    {
        struct MappingWorkData
        {
            public float3 position;
            public int vertexIndex;
            public int proxyVertexIndex;
            public float proxyVertexDistance;
        }

        /// <summary>
        /// メッシュをプロキシメッシュにマッピングする（スレッド可）
        /// </summary>
        /// <param name="proxyMesh"></param>
        public void Mapping(VirtualMesh proxyMesh)
        {
        }

        [BurstCompile]
        struct Mapping_DirectConnectionVertexDataJob : IJob
        {
            // render meshの座標をproxyのローカル空間に変換するTransform
            public float4x4 toP;

            // render mesh
            public int vcnt;
            public DataChunk mergeChunk;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> attributes;

            // proxy mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> proxyAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyLocalPositions;

            // out
            [Unity.Collections.WriteOnly]
            public NativeArray<MappingWorkData> mappingWorkData;

            public void Execute()
            {
            }
        }

        struct Mapping_CalcDirectWeightJob : IJob
        {
            // data
            public int vcnt;
            public float weightLength;
            [Unity.Collections.ReadOnly]
            public NativeArray<MappingWorkData> mappingWorkData;

            // render mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            // proxy
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> proxyVertexToVertexIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> proxyVertexToVertexDataArray;

            public NativeParallelHashSet<ushort> useSet; // Unity2023.1.5対応

            public void Execute()
            {
            }
        }

        /// <summary>
        /// 頂点ごとにproxy頂点を検索しウエイト算出しboneWeightsに格納する
        /// </summary>
        [BurstCompile]
        struct Mapping_CalcConnectionVertexDataJob : IJob
        {
            public float gridSize;
            public float searchRadius;

            // vmeshの座標をproxyのローカル空間に変換するTransform
            public float4x4 toP;

            // vmesh
            public int vcnt;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> transformIds;
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> attributes;

            // proxy vmesh
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> proxyAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> proxyBoneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> proxyTransformIds;

            // out
            [Unity.Collections.WriteOnly]
            public NativeArray<MappingWorkData> mappingWorkData;

            public void Execute()
            {
            }
        }

        /// <summary>
        /// 近傍プロキシメッシュ頂点を基準に頂点ウエイトを算出する
        /// </summary>
        [BurstCompile]
        struct Mapping_CalcWeightJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<MappingWorkData> mappingWorkData;

            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> proxyAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyLocalNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> proxyVertexToVertexIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> proxyVertexToVertexDataArray;

            public void Execute(int vindex)
            {
            }
        }

        /// <summary>
        /// 距離リストからウエイト値を算出して返す
        /// </summary>
        /// <param name="distances"></param>
        /// <returns></returns>
        static float4 CalcVertexWeights(float4 distances)
        {
            return default;
        }
    }
}
