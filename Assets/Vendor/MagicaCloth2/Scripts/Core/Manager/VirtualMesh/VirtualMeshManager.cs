// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// VirtualMeshの管理マネージャ
    /// </summary>
    public class VirtualMeshManager : IManager, IValid
    {
        //=========================================================================================
        // ■ProxyMesh
        //=========================================================================================
        // ■共通
        /// <summary>
        /// 対応するチームID
        /// </summary>
        public ExNativeArray<short> teamIds;

        /// <summary>
        /// 頂点属性
        /// </summary>
        public ExNativeArray<VertexAttribute> attributes;

        /// <summary>
        /// 頂点ごとの接続トライアングルインデックスと法線接線フリップフラグ（最大７つ）
        /// これは法線を再計算するために用いられるもので７つあれば十分であると判断したもの。
        /// そのため正確なトライアングル接続を表していない。
        /// データは12-20bitのuintでパックされている
        /// 12(hi) = 法線接線のフリップフラグ(法線:0x1,接線:0x2)。ONの場合フリップ。
        /// 20(low) = トライアングルインデックス。
        /// </summary>
        public ExNativeArray<FixedList32Bytes<uint>> vertexToTriangles;

        /// <summary>
        /// 頂点ごとの接続頂点インデックス
        /// ※現在未使用
        /// </summary>
        //public ExNativeArray<uint> vertexToVertexIndexArray;
        //public ExNativeArray<ushort> vertexToVertexDataArray;

        /// <summary>
        /// 頂点ごとのバインドポーズ
        /// 頂点バインドにはスケール値は不要
        /// </summary>
        public ExNativeArray<float3> vertexBindPosePositions;
        public ExNativeArray<quaternion> vertexBindPoseRotations;

        /// <summary>
        /// 各頂点の深さ(0.0-1.0)
        /// </summary>
        public ExNativeArray<float> vertexDepths;

        /// <summary>
        /// 各頂点のルートインデックス(-1=なし)
        /// </summary>
        public ExNativeArray<int> vertexRootIndices;

        /// <summary>
        /// 各頂点の親からの基準ローカル座標
        /// </summary>
        public ExNativeArray<float3> vertexLocalPositions;

        /// <summary>
        /// 各頂点の親からの基準ローカル回転
        /// </summary>
        public ExNativeArray<quaternion> vertexLocalRotations;

        /// <summary>
        /// 各頂点の親頂点インデックス(-1=なし)
        /// </summary>
        public ExNativeArray<int> vertexParentIndices;

        /// <summary>
        /// 各頂点の子頂点インデックスリスト
        /// </summary>
        public ExNativeArray<uint> vertexChildIndexArray;
        public ExNativeArray<ushort> vertexChildDataArray;

        /// <summary>
        /// 法線調整用回転
        /// </summary>
        public ExNativeArray<quaternion> normalAdjustmentRotations;

        /// <summary>
        /// 各頂点の角度計算用のローカル回転
        /// pitch/yaw個別制限はv1.0では実装しないので一旦停止させる
        /// </summary>
        //public ExNativeArray<quaternion> vertexAngleCalcLocalRotations;

        /// <summary>
        /// UV
        /// VirtualMeshのUVはTangent計算用でありテクスチャマッピング用ではないので注意！
        /// </summary>
        public ExNativeArray<float2> uv;


        public int VertexCount => teamIds?.Count ?? 0;

        // ■トライアングル -----------------------------------------------------
        public ExNativeArray<short> triangleTeamIdArray;

        /// <summary>
        /// トライアングル頂点インデックス
        /// </summary>
        public ExNativeArray<int3> triangles;

        /// <summary>
        /// トライアングル法線
        /// </summary>
        public ExNativeArray<float3> triangleNormals;

        /// <summary>
        /// トライアングル接線
        /// </summary>
        public ExNativeArray<float3> triangleTangents;

        public int TriangleCount => triangles?.Count ?? 0;

        // ■エッジ -------------------------------------------------------------
        public ExNativeArray<short> edgeTeamIdArray;

        /// <summary>
        /// エッジ頂点インデックス
        /// </summary>
        public ExNativeArray<int2> edges;

        /// <summary>
        /// エッジ固有フラグ(VirtualMesh.EdgeFlag_~)
        /// </summary>
        public ExNativeArray<ExBitFlag8> edgeFlags;

        public int EdgeCount => edges?.Count ?? 0;

        // ■ベースライン -------------------------------------------------------
        /// <summary>
        /// ベースラインごとのフラグ
        /// </summary>
        public ExNativeArray<ExBitFlag8> baseLineFlags;

        /// <summary>
        /// ベースラインごとのチームID
        /// </summary>
        public ExNativeArray<short> baseLineTeamIds;

        /// <summary>
        /// ベースラインごとのデータ開始インデックス
        /// </summary>
        public ExNativeArray<ushort> baseLineStartDataIndices;

        /// <summary>
        /// ベースラインごとのデータ数
        /// </summary>
        public ExNativeArray<ushort> baseLineDataCounts;

        /// <summary>
        /// ベースラインデータ（頂点インデックス）
        /// </summary>
        public ExNativeArray<ushort> baseLineData;

        public int BaseLineCount => baseLineFlags?.Count ?? 0;

        // ■メッシュ基本(共通) -------------------------------------------------
        public ExNativeArray<float3> localPositions;
        public ExNativeArray<float3> localNormals;
        public ExNativeArray<float3> localTangents;
        public ExNativeArray<VirtualMeshBoneWeight> boneWeights;
        public ExNativeArray<int> skinBoneTransformIndices;
        public ExNativeArray<float4x4> skinBoneBindPoses;

        // ■MeshClothのみ -----------------------------------------------------
        public int MeshClothVertexCount => localPositions?.Count ?? 0;

        // ■BoneClothのみ -----------------------------------------------------
        public ExNativeArray<quaternion> vertexToTransformRotations;

        // ■最終頂点姿勢
        public ExNativeArray<float3> positions;
        public ExNativeArray<quaternion> rotations;

        //=========================================================================================
        // ■MappingMesh
        //=========================================================================================
        public ExNativeArray<short> mappingIdArray; // (+1)されているので注意！
        public ExNativeArray<int> mappingReferenceIndices;
        public ExNativeArray<VertexAttribute> mappingAttributes;
        public ExNativeArray<float3> mappingLocalPositins;
        public ExNativeArray<float3> mappingLocalNormals;
        //public ExNativeArray<float3> mappingLocalTangents;
        public ExNativeArray<VirtualMeshBoneWeight> mappingBoneWeights;
        public ExNativeArray<float3> mappingPositions;
        public ExNativeArray<float3> mappingNormals;


        public int MappingVertexCount => mappingIdArray?.Count ?? 0;


        //=========================================================================================
        bool isValid = false;

        //=========================================================================================
        public void Dispose()
        {
        }

        public void EnterdEditMode()
        {
        }

        public void Initialize()
        {
        }

        public bool IsValid()
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// プロキシメッシュをマネージャに登録する
        /// </summary>
        public void RegisterProxyMesh(int teamId, VirtualMeshContainer proxyMeshContainer)
        {
        }

        /// <summary>
        /// プロキシメッシュをマネージャから解除する
        /// </summary>
        public void ExitProxyMesh(int teamId)
        {
        }

        //=========================================================================================
        /// <summary>
        /// マッピングメッシュをマネージャに登録する（チームにも登録される）
        /// </summary>
        /// <param name="cbase"></param>
        /// <param name="mappingMesh"></param>
        /// <returns></returns>
        public DataChunk RegisterMappingMesh(int teamId, VirtualMeshContainer mappingMeshContainer)
        {
            return default;
        }

        public void ExitMappingMesh(int teamId, int mappingIndex)
        {
        }

        //=========================================================================================
        /// <summary>
        /// ProxyMeshの現在の姿勢を計算する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe internal JobHandle PreProxyMeshUpdate(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct ClearProxyMeshUpdateBufferJob : IJob
        {
            public NativeReference<int> processingCounter0;
            public NativeReference<int> processingCounter1;
            public NativeReference<int> processingCounter2;
            public NativeReference<int> processingCounter3;

            public void Execute()
            {
            }
        }

        [BurstCompile]
        struct CreateProxyMeshUpdateVertexList : IJobParallelFor
        {
            public NativeArray<TeamManager.TeamData> teamDataArray;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingCounter1;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> processingList1;

            public void Execute(int teamId)
            {
            }
        }

        /// <summary>
        /// プロキシメッシュの頂点スキニングを行い座標・法線・接線を求める
        /// [BoneCloth][MeshCloth]兼用
        /// 姿勢はワールド座標で格納される
        /// </summary>
        [BurstCompile]
        struct CalcProxyMeshSkinningJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobVertexIndexList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIds;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> skinBoneTransformIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float4x4> skinBoneBindPoses;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> positions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotations;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float4x4> transformLocalToWorldMatrixArray;

            public void Execute(int index)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// クロスシミュレーションの結果をProxyMeshへ反映させる
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal unsafe JobHandle PostProxyMeshUpdate(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct CreatePostProxyMeshUpdateListJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // triangle vertex update
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingCounter0;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> processingList0;

            // transform vertex update
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingCounter1;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> processingList1;

            // base line
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingCounter2;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> processingList2;

            // triangle
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeReference<int> processingCounter3;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> processingList3;

            public void Execute(int teamId)
            {
            }
        }

        /// <summary>
        /// ラインのワールド法線接線を求める
        /// </summary>
        [BurstCompile]
        struct CalcLineNormalTangentJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobBaseLineList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> vertexLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexLocalRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> parentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> childIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> childDataArray;

            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> baseLineFlags;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> baseLineTeamIds;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineStartIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineDataCounts;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineData;

            // ベースラインごと
            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// トライアングルの法線と接線を求める
        /// 座標系の変換は行わない
        /// </summary>
        [BurstCompile]
        struct CalcTriangleNormalTangentJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobTriangleList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // triangle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> triangleTeamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> outTriangleNormals;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> outTriangleTangents;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float2> uv;


            // トライアングルごと
            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// 接続するトライアングルの法線接線を平均化して頂点法線接線を求める
        /// </summary>
        [BurstCompile]
        struct CalcVertexNormalTangentFromTriangleJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobVertexIndexList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIds;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> normalAdjustmentRotations;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> outRotations;

            // 頂点ごと
            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// パーティクルの姿勢をTransformDataに書き込む
        /// </summary>
        [BurstCompile]
        struct WriteTransformDataJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobVertexIndexList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // transform
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> transformPositionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> transformRotationArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIds;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexToTransformRotations;

            // トランスフォームパーティクルごと
            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// TransformパーティクルのTransformについて親からのローカル姿勢を計算する
        /// </summary>
        [BurstCompile]
        struct WriteTransformLocalDataJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> jobVertexIndexList;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmeah
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIds;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> vertexParentIndices;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> transformLocalPositionArray;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> transformLocalRotationArray;

            // Transformパーティクルごと
            public void Execute(int index)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// マッピングメッシュの頂点姿勢を連動するプロキシメッシュから頂点スキニングして求める
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle PostMappingMeshUpdate(JobHandle jobHandle)
        {
            return default;
        }

        /// <summary>
        /// // マッピングメッシュとプロキシメッシュの座標変換マトリックスを求める
        /// </summary>
        [BurstCompile]
        struct CalcMeshConvertMatrixJob : IJobParallelFor
        {
            public NativeArray<TeamManager.MappingData> mappingDataArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<quaternion> transformInverseRotationArray;

            // マッピングメッシュごと
            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// プロキシメッシュからマッピングメッシュの頂点の座標・法線・接線をスキニングして計算する
        /// </summary>
        [BurstCompile]
        struct CalcProxySkinningJob : IJobParallelFor
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // mapping data
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.MappingData> mappingDataArray;

            // mapping mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<short> mappingIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> mappingAttributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> mappingLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> mappingLocalNormals;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> mappingLocalTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> mappingBoneWeights;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> mappingPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> mappingNormals;

            // proxy mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> proxyRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> proxyVertexBindPosePositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> proxyVertexBindPoseRotations;

            // マッピングメッシュ頂点ごと
            public void Execute(int mvindex)
            {
            }
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
        }
    }
}
