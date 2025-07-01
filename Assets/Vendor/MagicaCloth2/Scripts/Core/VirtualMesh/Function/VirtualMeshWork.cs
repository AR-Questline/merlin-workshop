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
        //=========================================================================================
        /// <summary>
        /// 頂点間の平均/最大距離を調べる（スレッド可）
        /// 結果はaverageVertexDistance/maxVertexDistanceに格納される
        /// </summary>
        internal void CalcAverageAndMaxVertexDistanceRun()
        {
        }

        [BurstCompile]
        struct Work_AverageTriangleDistanceJob : IJob
        {
            public int vcnt;
            public int tcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            public NativeReference<float> averageVertexDistance;
            public NativeReference<int> averageCount;
            public NativeReference<float> maxVertexDistance;

            public void Execute()
            {
            }
        }

        [BurstCompile]
        struct Work_AverageLineDistanceJob : IJob
        {
            public int vcnt;
            public int lcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> lines;

            public NativeReference<float> averageVertexDistance;
            public NativeReference<int> averageCount;
            public NativeReference<float> maxVertexDistance;

            public void Execute()
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// 頂点インデックスを格納したグリッドマップを作成して返す
        /// </summary>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        internal GridMap<int> CreateVertexIndexGridMapRun(float gridSize)
        {
            return default;
        }

        [BurstCompile]
        struct Work_AddVertexIndexGirdMapJob : IJob
        {
            public float gridSize;
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positins;
            [Unity.Collections.WriteOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;

            public void Execute()
            {
            }
        }

        //=========================================================================================
        public VirtualMeshRaycastHit IntersectRayMesh(float3 rayPos, float3 rayDir, bool doubleSide, float pointRadius)
        {
            return default;
        }

        [BurstCompile]
        struct Work_IntersectTriangleJob : IJobParallelFor
        {
            public float3 localRayPos;
            public float3 localRayDir;
            public float3 localRayEndPos;
            public bool doubleSide;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            // output
            [Unity.Collections.WriteOnly]
            public NativeList<VirtualMeshRaycastHit>.ParallelWriter hitList;


            public void Execute(int tindex)
            {
            }
        }

        [BurstCompile]
        struct Work_IntersectEdgeJob : IJobParallelFor
        {
            public float3 localRayPos;
            public float3 localRayDir;
            public float3 localRayEndPos;
            public float3 rayDir;

            public float localEdgeRadius;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> edges;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int2, ushort> edgeToTriangles;

            // output
            [Unity.Collections.WriteOnly]
            public NativeList<VirtualMeshRaycastHit>.ParallelWriter hitList;

            public void Execute(int eindex)
            {
            }
        }

        [BurstCompile]
        struct Work_IntersectPointJob : IJobParallelFor
        {
            public float3 localRayPos;
            public float3 localRayDir;
            public float3 rayDir;

            public float localPointRadius;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<FixedList32Bytes<int>> vertexToTriangles;

            // output
            [Unity.Collections.WriteOnly]
            public NativeList<VirtualMeshRaycastHit>.ParallelWriter hitList;

            public void Execute(int vindex)
            {
            }
        }

        [BurstCompile]
        struct Work_IntersetcSortJob : IJob
        {
            public NativeList<VirtualMeshRaycastHit> hitList;

            public void Execute()
            {
            }
        }
    }
}
