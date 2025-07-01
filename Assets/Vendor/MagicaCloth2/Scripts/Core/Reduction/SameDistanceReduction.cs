// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
#if MAGICACLOTH2_REDUCTION_DEBUG
using UnityEngine;
#endif

namespace MagicaCloth2
{
    /// <summary>
    /// 距離内のすべての頂点を一度に結合させる
    /// </summary>
    public class SameDistanceReduction : IDisposable
    {
        string name = string.Empty;
        VirtualMesh vmesh;
        ReductionWorkData workData;
        ResultCode result;
        float mergeLength;

        //=========================================================================================
        GridMap<int> gridMap;
        //NativeParallelHashSet<int2> joinPairSet;
        NativeParallelMultiHashMap<ushort, ushort> joinPairMap;
        NativeReference<int> resultRef;

        //=========================================================================================
        public SameDistanceReduction() {
        }

        public SameDistanceReduction(
            string name,
            VirtualMesh mesh,
            ReductionWorkData workingData,
            float mergeLength
            )
        {
        }

        public virtual void Dispose()
        {
        }

        public ResultCode Result => result;

        //=========================================================================================
        /// <summary>
        /// リダクション実行（スレッド可）
        /// </summary>
        /// <returns></returns>
        public ResultCode Reduction()
        {
            return default;
        }

        //=========================================================================================
        [BurstCompile]
        struct InitGridJob : IJob
        {
            public int vcnt;
            public float gridSize;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            public NativeParallelMultiHashMap<int3, int> gridMap;

            // 頂点ごと
            public void Execute()
            {
            }
        }

        [BurstCompile]
        struct SearchJoinJob : IJob
        {
            public int vcnt;
            public float gridSize;
            public float radius;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;

            //public NativeParallelHashSet<int2> joinPairSet;
            public NativeParallelMultiHashMap<ushort, ushort> joinPairMap;

            public void Execute()
            {
            }
        }

        [BurstCompile]
        struct JoinJob2 : IJob
        {
            public int vertexCount;

            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> joinPairMap;

            public NativeArray<int> joinIndices;
            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            public NativeArray<VertexAttribute> attributes;
            public NativeReference<int> result;

            public NativeList<ushort> tempList;

            public void Execute()
            {
            }
        }

#if false // old
        [BurstCompile]
        struct JoinJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeParallelHashSet<int2> joinPairSet;

            public NativeArray<int> joinIndices;
            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            public NativeArray<VertexAttribute> attributes;
            public NativeReference<int> result;

            public void Execute()
            {
                var workSet = new FixedList512Bytes<ushort>();
                int cnt = 0;

                foreach (var ehash in joinPairSet)
                {
                    int vindexLive = ehash[0]; // 生存側
                    int vindexDead = ehash[1]; // 削除側

                    while (joinIndices[vindexDead] >= 0)
                    {
                        vindexDead = joinIndices[vindexDead];
                    }
                    while (joinIndices[vindexLive] >= 0)
                    {
                        vindexLive = joinIndices[vindexLive];
                    }
                    if (vindexDead == vindexLive)
                        continue;

                    // 結合(vertex1 -> vertex2)
                    joinIndices[vindexDead] = vindexLive;
                    cnt++;

                    // 接続数を結合する（重複は弾かれる）
                    workSet.Clear();
                    foreach (ushort i in vertexToVertexMap.GetValuesForKey((ushort)vindexDead))
                    {
                        int index = i;
                        // 生存インデックス
                        while (joinIndices[index] >= 0)
                        {
                            index = joinIndices[index];
                        }
                        if (index != vindexDead && index != vindexLive)
                            workSet.MC2Set((ushort)index);
                    }
                    foreach (ushort i in vertexToVertexMap.GetValuesForKey((ushort)vindexLive))
                    {
                        int index = i;
                        // 生存インデックス
                        while (joinIndices[index] >= 0)
                        {
                            index = joinIndices[index];
                        }
                        if (index != vindexDead && index != vindexLive)
                            workSet.MC2Set((ushort)index);
                    }
                    vertexToVertexMap.Remove((ushort)vindexLive);
                    for (int i = 0; i < workSet.Length; i++)
                    {
                        vertexToVertexMap.Add((ushort)vindexLive, workSet[i]);
                    }
                    //Debug.Assert(workSet.Length > 0);

                    // p2にBoneWeightを結合
                    var bw = boneWeights[vindexLive];
                    bw.AddWeight(boneWeights[vindexDead]);
                    boneWeights[vindexLive] = bw;

                    // 属性
                    var attr1 = attributes[vindexDead];
                    var attr2 = attributes[vindexLive];
                    attributes[vindexLive] = VertexAttribute.JoinAttribute(attr1, attr2);
                    attributes[vindexDead] = VertexAttribute.Invalid; // 削除頂点は無効にする
                }

                // 削除頂点数記録
                result.Value = cnt;
            }
        }
#endif

        //=========================================================================================
        /// <summary>
        /// 接続状態を最新に更新するジョブを発行する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        void UpdateJoinAndLink()
        {
        }

        [BurstCompile]
        struct UpdateJoinIndexJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> joinIndices;

            public void Execute(int vindex)
            {
            }
        }

        [BurstCompile]
        struct UpdateLinkIndexJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> joinIndices;
            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;

            public void Execute(int vindex)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// リダクション後のデータを整える
        /// </summary>
        void UpdateReductionResultJob()
        {
        }

        [BurstCompile]
        struct FinalMergeVertexJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;

            public NativeArray<float3> localNormals;
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            // 頂点ごと
            public void Execute(int vindex)
            {
            }
        }
    }
}
