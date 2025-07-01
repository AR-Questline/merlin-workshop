// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
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
        /// レンダー情報からインポートする（スレッド可）
        /// </summary>
        /// <param name="rsetup"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public void ImportFrom(RenderSetupData rsetup)
        {
        }

        /// <summary>
        /// Meshタイプのインポート
        /// </summary>
        /// <param name="rsetup"></param>
        /// <param name="transformIndices"></param>
        void ImportMeshType(RenderSetupData rsetup, int[] transformIndices)
        {
        }

        /// <summary>
        /// tangentを擬似生成する
        /// このtangentは描画用では無く姿勢制御用なのである意味適当でも大丈夫
        /// </summary>
        [BurstCompile]
        struct Import_GenerateTangentJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localTangents;

            public void Execute(int vindex)
            {
            }
        }


        /// <summary>
        /// スキニングメッシュの頂点をスキニングして元のローカル空間に変換する
        /// </summary>
        void ImportMeshSkinning()
        {
        }

        /// <summary>
        /// 頂点スキニングを行いワールド座標・法線・接線を求める
        /// </summary>
        [BurstCompile]
        struct Import_CalcSkinningJob : IJobParallelFor
        {
            //[Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            //[Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            //[Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> skinBoneTransformIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float4x4> bindPoses;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;

            public float4x4 toM;

            public void Execute(int vindex)
            {
            }
        }


        [BurstCompile]
        struct Import_BoneWeightJob1 : IJob
        {
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> bonesPerVertexArray;

            [Unity.Collections.WriteOnly]
            public NativeArray<int> startBoneWeightIndices;

            public void Execute()
            {
            }
        }

        [BurstCompile]
        struct Import_BoneWeightJob2 : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> startBoneWeightIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<BoneWeight1> boneWeightArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<byte> bonesPerVertexArray;

            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            public void Execute(int vindex)
            {
            }
        }

        /// <summary>
        /// Boneタイプのインポート
        /// </summary>
        /// <param name="rsetup"></param>
        /// <param name="transformIndices"></param>
        void ImportBoneType(RenderSetupData rsetup, int[] transformIndices)
        {
        }

        [BurstCompile]
        struct Import_BoneVertexJob : IJobParallelFor
        {
            public float4x4 WtoL;
            public float4x4 LtoW;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScales;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            [Unity.Collections.WriteOnly]
            public NativeArray<float4x4> skinBoneBindPoses;

            public void Execute(int vindex)
            {
            }
        }


        /// <summary>
        /// レンダーデータからインポートする
        /// </summary>
        /// <param name="renderData"></param>
        public void ImportFrom(RenderData renderData)
        {
        }

        //=========================================================================================
        /// <summary>
        /// セレクションデータをもとにメッシュを切り取る（スレッド可）
        /// セレクションデータの移動属性に影響するトライアングルのみを摘出する
        /// 結果的にメッシュの頂点／トライアングル数が０になる場合もあるので注意！
        /// </summary>
        /// <param name="selectionData"></param>
        /// <param name="selectionLocalToWorldMatrix">セレクションデータの基準姿勢</param>
        /// <param name="mergin">検索距離</param>
        public void SelectionMesh(
            SelectionData selectionData,
            float4x4 selectionLocalToWorldMatrix,
            float mergin
            )
        {
        }

        [BurstCompile]
        struct Select_PackVertexJob : IJob
        {
            public int vertexCount;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> newVertexRemapIndices;

            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<float2> uv;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            [Unity.Collections.WriteOnly]
            public NativeArray<int> newReferenceIndices;
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> newAttributes;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalNormals;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> newLocalTangents;
            [Unity.Collections.WriteOnly]
            public NativeArray<float2> newUv;
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> newBoneWeights;

            public void Execute()
            {
            }
        }

        [BurstCompile]
        struct Select_GridJob : IJob
        {
            public float gridSize;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;

            public int selectionCount;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> selectionPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> selectionAttributes;

            public int vertexCount;
            public int triangleCount;
            public float searchRadius;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> meshPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> meshTriangles;

            public NativeList<int3> newTriangles;
            public NativeArray<int> newVertexRemapIndices;
            [Unity.Collections.WriteOnly]
            public NativeReference<int> newVertexCount;

            public void Execute()
            {
            }
        }

        /// <summary>
        /// 現在のメッシュに対して最適なセレクションの余白距離を算出する
        /// </summary>
        /// <param name="useReduction"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public float CalcSelectionMergin(ReductionSettings settings)
        {
            return default;
        }


        //=========================================================================================
        /// <summary>
        /// メッシュを追加する（スレッド可）
        /// </summary>
        /// <param name="cmesh"></param>
        public void AddMesh(VirtualMesh cmesh)
        {
        }

        /// <summary>
        /// 空間が変更された場合はバインドポーズを再計算する
        /// </summary>
        [BurstCompile]
        struct Add_CalcBindPoseJob : IJobParallelFor
        {
            public int skinBoneOffset;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> srcSkinBoneTransformIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srcTransformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> srcTransformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srcTransformScaleArray;

            public float4x4 dstCenterLocalToWorldMatrix;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float4x4> dstSkinBoneBindPoses;

            public void Execute(int boneIndex)
            {
            }
        }

        /// <summary>
        /// 頂点データを新しい領域にコピーする
        /// </summary>
        [BurstCompile]
        struct Add_CopyVerticesJob : IJobParallelFor
        {
            public int vertexOffset;
            public int skinBoneOffset;

            // 座標空間変換
            //public int same; // 追加先と同じ空間なら(1)
            public float4x4 toM;

            // src
            //[Unity.Collections.ReadOnly]
            //public NativeArray<ExBitFlag8> srcFlags;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> srcAttributes;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> srcWorldPositions;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<quaternion> srcWorldRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srclocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srclocalNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srclocalTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<float2> srcUV;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> srcBoneWeights;

            // dst
            //[NativeDisableParallelForRestriction]
            //[Unity.Collections.WriteOnly]
            //public NativeArray<ExBitFlag8> dstFlags;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> dstAttributes;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> dstlocalPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> dstlocalNormals;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> dstlocalTangents;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float2> dstUV;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> dstBoneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> dstSkinBoneIndices;

            public void Execute(int vindex)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// メッシュの基準トランスフォームを設定する（メインスレッドのみ）
        /// </summary>
        /// <param name="center"></param>
        /// <param name="skinRoot"></param>
        public void SetTransform(Transform center, Transform skinRoot = null, int centerId = 0, int skinRootId = 0)
        {
        }

        /// <summary>
        /// レコード情報からメッシュの基準トランスフォームを設定する（スレッド可）
        /// </summary>
        /// <param name="record"></param>
        public void SetTransform(TransformRecord centerRecord, TransformRecord skinRootRecord = null)
        {
        }

        public void SetCenterTransform(Transform t, int tid = 0)
        {
        }

        public void SetSkinRoot(Transform t, int tid = 0)
        {
        }

        public Transform GetCenterTransform()
        {
            return default;
        }

        /// <summary>
        /// カスタムスキニング用ボーンを登録する
        /// </summary>
        /// <param name="bones"></param>
        public void SetCustomSkinningBones(TransformRecord clothTransformRecord, List<TransformRecord> bones)
        {
        }

        /// <summary>
        /// このメッシュと対象メッシュの座標空間が同じか判定する
        /// これはそれぞれ初期化時のマトリックスで比較される
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool CompareSpace(VirtualMesh target)
        {
            return default;
        }

        /// <summary>
        /// このメッシュの座標空間をtoメッシュの座標空間に変換するマトリックスを返す
        /// これはそれぞれ初期化時のマトリックスで計算される
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        public float4x4 CenterTransformTo(VirtualMesh to)
        {
            return default;
        }

        //=========================================================================================
#if false
        /// <summary>
        /// UnityMeshに出力する（メインスレッドのみ）
        /// ※ほぼデバッグ用
        /// </summary>
        /// <returns></returns>
        public Mesh ExportToMesh(bool buildSkinning = false, bool recalculationNormals = false, bool recalculationBounds = true)
        {
            Debug.Assert(IsSuccess);

            var mesh = new Mesh();
            mesh.MarkDynamic();

            // vertices
            var newVertices = new Vector3[VertexCount];
            var newNormals = new Vector3[VertexCount];
            localPositions.CopyTo(newVertices);
            localNormals.CopyTo(newNormals);

            // 接線はwを(-1)にして書き込む
            using var tangentArray = new NativeArray<Vector4>(VertexCount, Allocator.TempJob);
            JobUtility.FillRun(tangentArray, VertexCount, new Vector4(0, 0, 0, -1));
            var newTangents = tangentArray.ToArray();
            localTangents.CopyToWithTypeChangeStride(newTangents); // float3[] -> Vector4[]コピー

            mesh.vertices = newVertices;
            if (recalculationNormals == false)
                mesh.normals = newNormals;
            mesh.tangents = newTangents;

            // dymmy uv
            var newUvs = new Vector2[VertexCount];
            uv.CopyTo(newUvs); // 一応コピー（VirtualMeshのUVはTangent計算用、テクスチャマッピング用ではない）
            mesh.uv = newUvs;

            // triangle
            if (TriangleCount > 0)
            {
                var newTriangles = new int[TriangleCount * 3];
                triangles.CopyToWithTypeChange(newTriangles);
                mesh.triangles = newTriangles;
            }

            // skinning
            if (buildSkinning)
            {
                // bone weight
                var newBoneWeights = new BoneWeight[VertexCount];
                boneWeights.CopyTo(newBoneWeights);
                mesh.boneWeights = newBoneWeights;

                // bind poses
                var newBindPoses = new Matrix4x4[SkinBoneCount];
                skinBoneBindPoses.CopyTo(newBindPoses);
                mesh.bindposes = newBindPoses;
            }

            if (recalculationNormals)
                mesh.RecalculateNormals();
            //mesh.RecalculateTangents();
            if (recalculationBounds)
                mesh.RecalculateBounds();


            return mesh;
        }
#endif
    }
}
