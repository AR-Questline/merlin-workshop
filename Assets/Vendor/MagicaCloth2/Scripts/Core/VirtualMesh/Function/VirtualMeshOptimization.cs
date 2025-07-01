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
        /// <summary>
        /// メッシュを最適化する
        /// </summary>
        public void Optimization()
        {
        }

        /// <summary>
        /// 重複するトライアングルを除去する
        /// </summary>
        void RemoveDuplicateTriangles()
        {
        }

        [BurstCompile]
        struct Optimize_EdgeToTrianlgeJob : IJob
        {
            public int tcnt;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;

            public NativeParallelHashMap<int2, FixedList128Bytes<int>> edgeToTriangleList;
            [Unity.Collections.WriteOnly]
            public NativeList<int3> newTriangles;

            public NativeParallelHashSet<int4> useQuadSet; // Unity2023.1.5対応
            public NativeParallelHashSet<int3> removeTriangleSet; // Unity2023.1.5対応

            public void Execute()
            {
            }
        }

        /// <summary>
        /// 共通するエッジをもつ２つのトライアングルが開いているか判定する
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        bool CheckTwoTriangleOpen(in int3 tri1, in int3 tri2, in int2 edge, in float3 tri1n)
        {
            return default;
        }

        /// <summary>
        /// 共通するエッジをもつ２つのトライアングルのなす角を求める（デグリー角）
        /// </summary>
        /// <param name="tri1"></param>
        /// <param name="tri2"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        float CalcTwoTriangleAngle(in int3 tri1, in int3 tri2, in int2 edge)
        {
            return default;
        }
    }
}
