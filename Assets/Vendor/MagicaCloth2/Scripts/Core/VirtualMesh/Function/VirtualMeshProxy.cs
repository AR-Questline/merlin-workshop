// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class VirtualMesh
    {
        /// <summary>
        /// メッシュをプロキシメッシュに変換する（スレッド可）
        /// プロキシメッシュは頂点法線接線を接続するトライアングルから求めるようになる
        /// またマッピング用の頂点ごとのバインドポーズも保持する
        /// </summary>
        public void ConvertProxyMesh(
            ClothSerializeData sdata,
            TransformRecord clothTransformRecord,
            List<TransformRecord> customSkinningBoneRecords,
            TransformRecord normalAdjustmentTransformRecord
            )
        {
        }

#if false
        [BurstCompile]
        struct Proxy_ConvertInvalidToFixedJob : IJobParallelFor
        {
            // proxy mesh
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> vertexToVertexIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> vertexToVertexDataArray;

            public void Execute(int vindex)
            {
                var attr = attributes[vindex];

                // 無効判定
                if (attr.IsInvalid())
                {
                    // 移動属性に接続する場合のみ固定に変更する
                    var pack = vertexToVertexIndexArray[vindex];
                    DataUtility.Unpack10_22(pack, out var dcnt, out var dstart);
                    for (int i = 0; i < dcnt; i++)
                    {
                        int tvindex = vertexToVertexDataArray[dstart + i];
                        var tattr = attributes[tvindex];
                        if (tattr.IsMove())
                        {
                            // 固定に変更
                            attr.SetFlag(VertexAttribute.Flag_Ignore, true);
                            attr.SetFlag(VertexAttribute.Flag_Fixed, true);
                            attributes[vindex] = attr;
                            return;
                        }
                    }
                }
            }
        }
#endif

        /// <summary>
        /// 法線方向の調整
        /// </summary>
        void ProxyNormalAdjustment(ClothSerializeData sdata, TransformRecord normalAdjustmentTransformRecord)
        {
        }

        [BurstCompile]
        struct ProxyNormalRadiationAdjustmentJob : IJobParallelFor
        {
            public float3 center;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexParentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> vertexChildIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> vertexChildDataArray;

            // out
            public NativeArray<float3> localNormals;
            public NativeArray<float3> localTangents;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> normalAdjustmentRotations;

            public void Execute(int vindex)
            {
            }
        }

        /// <summary>
        /// シミュレーションに関係する頂点からAABBと固定頂点のリストを作成する
        /// </summary>
        void ProxyCreateFixedListAndAABB()
        {
        }

        [BurstCompile]
        struct ProxyCreateFixedListAndAABBJob : IJob
        {
            public int vcnt;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> vertexToVertexIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> vertexToVertexDataArray;

            // out
            [Unity.Collections.WriteOnly]
            public NativeReference<AABB> outAABB;
            [Unity.Collections.WriteOnly]
            public NativeList<ushort> fixedList;
            [Unity.Collections.WriteOnly]
            public NativeReference<float3> localCenterPosition;

            public void Execute()
            {
            }
        }


        /// <summary>
        /// トライアングルの方向を頂点法線に沿うようにできるだけ合わせる
        /// ※この処理はとても重要。この合わせにより法線の計算精度が上がる。
        /// </summary>
        void OptimizeTriangleDirection(NativeArray<float3> triangleNormals, float sameSurfaceAngle)
        {
        }


        /// <summary>
        /// トライアングル法線を求める
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcTriangleNormalJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositins;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> triangleNormals;

            public void Execute(int tindex)
            {
            }
        }

        /// <summary>
        /// トライアングル接線を求める
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcTriangleTangentJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositins;
            [Unity.Collections.ReadOnly]
            public NativeArray<float2> uv;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> triangleTangents;

            public void Execute(int tindex)
            {
            }
        }

        /// <summary>
        /// 頂点ごとに接続するトライアングルを求める（最大７つ）
        /// </summary>
        [BurstCompile]
        unsafe struct Proxy_CreateVertexToTrianglesJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;

            public void Execute()
            {
            }
        }

        /// <summary>
        /// 接続トライアングルから頂点法線接線を計算するために最適なトライアングル方向を計算して格納する
        /// またトライアングルに属する頂点にはフラグを立てる
        /// </summary>
        [BurstCompile]
        struct Proxy_OrganizeVertexToTrianglsJob : IJobParallelFor
        {
            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleTangents;

            public NativeArray<VertexAttribute> attributes;

            public void Execute(int vindex)
            {
            }
        }

        /// <summary>
        /// 現在メッシュの頂点法線接線を接続トライアングル情報から更新する
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcVertexNormalTangentFromTriangleJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleTangents;

            [Unity.Collections.ReadOnly]
            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;
            public NativeArray<float3> localNormals;
            public NativeArray<float3> localTangents;

            public void Execute(int vindex)
            {
            }
        }

        [BurstCompile]
        struct Proxy_CalcVertexToTransformJob : IJobParallelFor
        {
            public quaternion invRot;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> vertexToTransformRotations;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotations;

            public void Execute(int vindex)
            {
            }
        }

        /// <summary>
        /// エッジごとの接続トライアングルマップを作成する
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcEdgeToTriangleJob : IJob
        {
            public int tcnt;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            public NativeParallelMultiHashMap<int2, ushort> edgeToTriangles;

            public void Execute()
            {
            }
        }

        /// <summary>
        /// 頂点のバインドポーズを求める
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcVertexBindPoseJob2 : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> vertexBindPosePositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> vertexBindPoseRotations;

            public void Execute(int vindex)
            {
            }
        }

        /// <summary>
        /// トライアングルに接続する頂点セットを求める
        /// およびエッジセットを作成する
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcVertexToVertexFromTriangleJob : IJob
        {
            public int triangleCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            public NativeParallelMultiHashMap<int, ushort> vertexToVertexMap;
            public NativeParallelHashSet<int2> edgeSet;

            public void Execute()
            {
            }
        }

        /// <summary>
        /// ラインに接続する頂点セットを求める
        /// およびエッジセットを作成する
        /// </summary>
        [BurstCompile]
        struct Proxy_CalcVertexToVertexFromLineJob : IJob
        {
            public int lineCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<int2> lines;
            public NativeParallelMultiHashMap<int, ushort> vertexToVertexMap;
            public NativeParallelHashSet<int2> edgeSet;

            public void Execute()
            {
            }
        }

        [BurstCompile]
        struct Proxy_CreateEdgeFlagJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> edges;
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int2, ushort> edgeToTriangles;

            [Unity.Collections.WriteOnly]
            public NativeArray<ExBitFlag8> edgeFlags;

            public void Execute(int eindex)
            {
            }
        }

        //=========================================================================================
        struct SkinningBoneInfo
        {
            //public int transformIndex;
            public int startTransformIndex;
            public float3 startPos;
            public int endTransformIndex;
            public float3 endPos;
        }

        /// <summary>
        /// カスタムスキニング情報の作成
        /// </summary>
        void CreateCustomSkinning(CustomSkinningSettings setting, List<TransformRecord> bones)
        {
        }

#if true
        [BurstCompile]
        struct Proxy_CalcCustomSkinningWeightsJob : IJobParallelFor
        {
            public bool isBoneCloth;
            public float angularAttenuation;
            public float distanceReduction;
            public float distancePow;

            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeList<SkinningBoneInfo> boneInfoList;
            [Unity.Collections.WriteOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;


            public void Execute(int vindex)
            {
            }
        }
#endif

        //=========================================================================================
        /// <summary>
        /// セレクションデータ属性をプロキシメッシュに反映させる（スレッド可）
        /// </summary>
        /// <param name="selectionData"></param>
        public void ApplySelectionAttribute(SelectionData selectionData)
        {
        }

        [BurstCompile]
        struct Proxy_ApplySelectionJob : IJobParallelFor
        {
            public float gridSize;
            public float radius;

            // proxy mesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            public NativeArray<VertexAttribute> attributes;

            // selection
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> selectionPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> selectionAttributes;

            public void Execute(int vindex)
            {
            }
        }

        [BurstCompile]
        struct Proxy_BoneClothApplayTransformFlagJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;

            public NativeArray<ExBitFlag8> transformFlags;

            public void Execute(int vindex)
            {
            }
        }

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// [MeshCloth]ベースラインの作成
        /// 頂点接続情報から親情報を作成する
        /// </summary>
        void CreateMeshBaseLine()
        {
        }

        struct BaseLineWork : IComparable<BaseLineWork>
        {
            public int vindex;
            public float dist;

            public int CompareTo(BaseLineWork other)
            {
                return default;
            }
        }

        [BurstCompile]
        struct BaseLine_Mesh_CreateParentJob2 : IJob
        {
            public int vcnt;
            public float avgDist;

            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attribues;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> vertexToVertexIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> vertexToVertexDataArray;

            public NativeArray<int> vertexParentIndices;
            public NativeParallelMultiHashMap<int, ushort> vertexChildMap;

            [Unity.Collections.ReadOnly]
            public NativeList<int> fixedList;
            public NativeList<BaseLineWork> nextList;

            public NativeArray<byte> markBuff; // Unity2023.1.5対応
            public NativeParallelHashMap<int, BaseLineWork> vertexMap; // Unity2023.1.5対応

            public void Execute()
            {
            }
        }

        /// <summary>
        /// (Mesh)固定ポイントをリストにする
        /// </summary>
        [BurstCompile]
        struct BaseLine_Mesh_CareteFixedListJob : IJob
        {
            public int vcnt;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attribues;

            public NativeList<int> fixedList;

            public void Execute()
            {
            }
        }

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// [BoneCloth]ベースライン情報の作成
        /// BoneClothでは単純にTransformの親子構造がそのままベースラインとなる
        /// </summary>
        void CreateTransformBaseLine()
        {
        }

        /// <summary>
        /// (Bone)ベースライン上の頂点ごとの子頂点リストを求める
        /// </summary>
        [BurstCompile]
        struct BaseLine_Bone_CreateBoneChildInfoJob : IJob
        {
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> parentIndices;
            //[Unity.Collections.WriteOnly]
            public NativeParallelMultiHashMap<int, ushort> childMap;

            public void Execute()
            {
            }
        }

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// (Mesh/Bone)ベースラインの基準姿勢を求める
        /// </summary>
        void CreateBaseLinePose()
        {
        }

        /// <summary>
        /// ベースラインの基準姿勢を求める
        /// </summary>
        [BurstCompile]
        struct BaseLine_CalcLocalPositionRotationJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> parentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;


            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineIndices;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> vertexLocalPositions;
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> vertexLocalRotations;

            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// 頂点ごとのルートインデックスと深さを求める
        /// </summary>
        void CreateVertexRootAndDepth()
        {
        }

        [BurstCompile]
        struct BaseLine_CalcMaxBaseLineLengthJob : IJob
        {
            public int vcnt;

            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attribues;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexParentIndices;

            [Unity.Collections.WriteOnly]
            public NativeArray<float> vertexDepths;
            [Unity.Collections.WriteOnly]
            public NativeArray<int> vertexRootIndices;

            public NativeArray<float> rootLengthArray;


            public void Execute()
            {
            }
        }

        //=========================================================================================
#if false // pitch/yaw個別制限はv1.0では実装しないので一旦ん停止
        /// <summary>
        /// 角度制限計算用ローカル回転の算出
        /// </summary>
        public void CreateAngleCalcLocalRotation(NormalCalcMode normalCalcMode, float3 normalCalcCenter)
        {
            if (VertexCount == 0)
                return;

            // 配列初期化
            vertexAngleCalcLocalRotations = new NativeArray<quaternion>(VertexCount, Allocator.Persistent);
            JobUtility.FillRun(vertexAngleCalcLocalRotations, VertexCount, quaternion.identity);

            // 頂点ごとに算出する
            var job = new AngleCalcLocalRotationJob()
            {
                calcMode = normalCalcMode,
                calcPoint = normalCalcCenter,
                //calcPoint = GetCenterTransform().TransformPoint(normalCalcCenter),
                //calcPoint = math.transform(initLocalToWorld, normalCalcCenter),

                attribues = attributes.GetNativeArray(),
                localPositions = localPositions.GetNativeArray(),
                localNormals = localNormals.GetNativeArray(),
                localTangents = localTangents.GetNativeArray(),
                vertexParentIndices = vertexParentIndices,
                vertexChildIndices = vertexChildIndices,
                vertexAngleCalcLocalRotations = vertexAngleCalcLocalRotations,
            };
            job.Run(VertexCount);
        }

        [BurstCompile]
        struct AngleCalcLocalRotationJob : IJobParallelFor
        {
            public NormalCalcMode calcMode;
            public float3 calcPoint;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attribues;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexParentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ExFixedSet32Bytes<ushort>> vertexChildIndices;

            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> vertexAngleCalcLocalRotations;

            public void Execute(int vindex)
            {
                // 子への方向、子がいない場合は親からの方向をfowardベクトルとする
                float3 z = 0;
                var clist = vertexChildIndices[vindex];
                int pindex = vertexParentIndices[vindex];
                var pos = localPositions[vindex];
                var nor = localNormals[vindex]; // 実はforward
                var tan = localTangents[vindex]; // 実はup
                var bin = MathUtility.Binormal(nor, tan);
                if (clist.Count > 0)
                {
                    // 子への方向の平均ベクトル
                    for (int i = 0; i < clist.Count; i++)
                    {
                        int cindex = clist.Get(i);
                        var cpos = localPositions[cindex];
                        z += (cpos - pos);
                    }
                    z = math.normalize(z);
                }
                else if (pindex >= 0)
                {
                    // 親からのベクトル
                    var ppos = localPositions[pindex];
                    z = math.normalize(pos - ppos);
                }
                else
                    return;

                // upベクトル
                float3 y = 0;
                if (calcMode == NormalCalcMode.Auto)
                {
                    // z方向と内積がもっとも0に近いものを見つける（つまり直角）
                    // その軸をupベクトルとする
                    float norDot = math.abs(math.dot(z, nor));
                    float tanDot = math.abs(math.dot(z, tan));
                    y = norDot < tanDot ? nor : tan;
                }
                else if (calcMode == NormalCalcMode.X_Axis)
                {
                    // 元のX軸をupベクトルとする
                    y = bin;
                }
                else if (calcMode == NormalCalcMode.Y_Axis)
                {
                    // 元のY軸をupベクトルとする
                    y = tan;
                }
                else if (calcMode == NormalCalcMode.Z_Axis)
                {
                    // 元のZ軸をupベクトルとする
                    y = nor;
                }
                else if (calcMode == NormalCalcMode.Point_Outside)
                {
                    // 指定された中心点から外側へ
                    y = math.normalize(pos - calcPoint);
                }
                else if (calcMode == NormalCalcMode.Point_Inside)
                {
                    // 指定された中心点から内側へ
                    y = math.normalize(calcPoint - pos);
                }
                else
                {
                    Debug.LogError("まだ未実装！");
                    return;
                }

                // rightベクトルを求める
                float3 x = math.cross(z, y);

                // もう一度upベクトルを求める
                y = math.cross(x, z);

                // このz/y軸で回転を作成
                var angleRot = quaternion.LookRotation(z, y);

                // 元の頂点回転姿勢
                var rot = MathUtility.ToRotation(nor, tan);

                // ローカル回転に変換
                angleRot = math.mul(math.inverse(rot), angleRot);


                vertexAngleCalcLocalRotations[vindex] = angleRot;
            }
        }
#endif
    }
}
