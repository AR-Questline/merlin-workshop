// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class VirtualMesh
    {
        //static readonly ProfilerMarker reductionProfiler = new ProfilerMarker("Reduction");

        //=========================================================================================
        /// <summary>
        /// リダクションを実行する（スレッド可）
        /// 処理時間が長いためCancellationTokenを受け入れる
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="ct"></param>
        public void Reduction(ReductionSettings settings, CancellationToken ct)
        {
        }

        //=========================================================================================
        /// <summary>
        /// リダクション用作業データの初期化
        /// </summary>
        /// <param name="workData"></param>
        void InitReductionWorkData(ReductionWorkData workData)
        {
        }

        [BurstCompile]
        unsafe struct Reduction_InitVertexToVertexJob2 : IJob
        {
            public int triangleCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;

            public void Execute()
            {
            }
        }

#if false
        [BurstCompile]
        unsafe struct Reduction_InitVertexToVertexJob : IJob
        {
            public int triangleCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            public NativeArray<FixedList128Bytes<ushort>> vertexToVertexArray;

            public void Execute()
            {
                var arrayPtr = (FixedList128Bytes<ushort>*)vertexToVertexArray.GetUnsafePtr();

                for (int i = 0; i < triangleCount; i++)
                {
                    int3 tri = triangles[i];

                    var ptrx = (arrayPtr + tri.x);
                    var ptry = (arrayPtr + tri.y);
                    var ptrz = (arrayPtr + tri.z);

                    ushort x = (ushort)tri.x;
                    ushort y = (ushort)tri.y;
                    ushort z = (ushort)tri.z;
                    ptrx->SetLimit(y);
                    ptrx->SetLimit(z);
                    ptry->SetLimit(x);
                    ptry->SetLimit(z);
                    ptrz->SetLimit(x);
                    ptrz->SetLimit(y);
                }
            }
        }
#endif

        //=========================================================================================
        /// <summary>
        /// リダクション結果からデータを再編成する
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="workData"></param>
        void Organization(ReductionSettings setting, ReductionWorkData workData)
        {
        }

        //=========================================================================================
        /// <summary>
        /// 再編成に必要なすべての準備を整える
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="workData"></param>
        void OrganizationInit(ReductionSettings setting, ReductionWorkData workData)
        {
        }

        //=========================================================================================
        /// <summary>
        /// リマップデータの作成
        /// 削除された頂点やボーンを生存するデータへ接続する
        /// </summary>
        /// <param name="workData"></param>
        void OrganizationCreateRemapData(ReductionWorkData workData)
        {
        }

        /// <summary>
        /// 生存頂点にインデックスを割り振る
        /// </summary>
        [BurstCompile]
        struct Organize_RemapVertexJob : IJob
        {
            public int oldVertexCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            public NativeArray<int> vertexRemapIndices;

            public void Execute()
            {
            }
        }

        /// <summary>
        /// 使用しているスキニングボーンを収集する
        /// </summary>
        [BurstCompile]
        struct Organize_CollectUseSkinBoneJob : IJob
        {
            public int oldVertexCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> oldBoneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<float4x4> oldBindPoses;

            public NativeParallelHashMap<int, int> useSkinBoneMap;

            public NativeList<int> newSkinBoneTransformIndices;
            public NativeList<float4x4> newSkinBoneBindPoses;
            public NativeReference<int> newSkinBoneCount;

            public NativeList<int> useSkinBoneMapKeyList; // Unity2023.1.5対応

            public void Execute()
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// 基本データ作成
        /// Line/Triangleを再編成するための基本的なデータを作成する
        /// </summary>
        /// <param name="workData"></param>
        void OrganizationCreateBasicData(ReductionWorkData workData)
        {
        }

        /// <summary>
        /// 新しい頂点にリダクション後のPositin/Normal/Tangent/Attributeをコピーする
        /// </summary>
        [BurstCompile]
        struct Organize_CopyVertexJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRemapIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> oldAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldLocalNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldLocalTangents;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> newAttributes;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalNormals;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalTangents;

            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// 新しいボーンウエイトリストを作成する
        /// </summary>
        [BurstCompile]
        struct Organize_RemapBoneWeightJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRemapIndices;
            [Unity.Collections.ReadOnly]
            public NativeParallelHashMap<int, int> useSkinBoneMap;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> oldSkinBoneIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> oldBoneWeights;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> newBoneWeights;

            public void Execute(int vindex)
            {
            }
        }

        /// <summary>
        /// 新しい頂点の接続頂点リストを作成する
        /// </summary>
        [BurstCompile]
        struct Organize_RemapLinkPointArrayJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRemapIndices;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> oldVertexToVertexMap;
            [NativeDisableParallelForRestriction]
            public NativeParallelMultiHashMap<ushort, ushort> newVertexToVertexMap;

            public void Execute(int vindex)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// 新しいラインとトライアングルを生成する
        /// </summary>
        /// <param name="workData"></param>
        void OrganizationCreateLineTriangle(ReductionWorkData workData)
        {
        }

        /// <summary>
        /// エッジセットを作成する
        /// </summary>
        [BurstCompile]
        struct Organize_CreateLineTriangleJob : IJob
        {
            public int newVertexCount;

            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> newVertexToVertexMap;

            [Unity.Collections.WriteOnly]
            public NativeParallelHashSet<int2> edgeSet;

            public void Execute()
            {
            }
        }

        /// <summary>
        /// エッジセットからラインとトライアングルセットを作成する
        /// </summary>
        [BurstCompile]
        struct Organize_CreateLineTriangleJob2 : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<ushort, ushort> newVertexToVertexMap;

            [Unity.Collections.WriteOnly]
            public NativeList<int2> newLineList;

            [Unity.Collections.ReadOnly]
            public NativeParallelHashSet<int2> edgeSet;
            [Unity.Collections.WriteOnly]
            public NativeParallelHashSet<int3> triangleSet;

            public void Execute()
            {
            }
        }

        /// <summary>
        /// トライアングルセットからトライアングルリストを作成する
        /// </summary>
        [BurstCompile]
        struct Organize_CreateNewTriangleJob3 : IJob
        {
            [Unity.Collections.WriteOnly]
            public NativeList<int3> newTriangleList;

            [Unity.Collections.ReadOnly]
            public NativeParallelHashSet<int3> triangleSet;

            public void Execute()
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// リダクション結果をvmeshに反映させる
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="workData"></param>
        void OrganizeStoreVirtualMesh(ReductionWorkData workData)
        {
        }
    }
}
