// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// 頂点の属性情報(移動/固定/無効)データ
    /// このデータはシリアライズされる
    /// 座標はクロスコンポーネントのローカル空間で格納される
    /// Vertex attribute information (move/fix/disable) data.
    /// This data is serialized.
    /// Coordinates are stored in the cloth component's local space.
    /// </summary>
    [System.Serializable]
    public class SelectionData : IValid
    {
        /// <summary>
        /// 属性のローカル座標
        /// これはクロスコンポーネント空間
        /// Attribute local coordinates.
        /// This is the cloth component space.
        /// </summary>
        public float3[] positions;

        /// <summary>
        /// 上記の属性値
        /// サイズはpositionsと同じでなくてはならない
        /// Attribute value above.
        /// size must be the same as positions.
        /// </summary>
        public VertexAttribute[] attributes;

        /// <summary>
        /// セレクションデータ構築時のVirtualMeshの最大頂点接続距離
        /// Maximum vertex connection distance of VirtualMesh when constructing selection data.
        /// </summary>
        public float maxConnectionDistance;

        /// <summary>
        /// ユーザーが編集したデータかどうか
        /// Is the data edited by the user?
        /// </summary>
        public bool userEdit = false;

        //=========================================================================================
        public SelectionData() {
        }

        public SelectionData(int cnt)
        {
        }

        public SelectionData(VirtualMesh vmesh, float4x4 transformMatrix)
        {
        }

        [BurstCompile]
        struct TransformPositionJob : IJobParallelFor
        {
            public float4x4 transformMatrix;
            public NativeArray<float3> localPositions;

            public void Execute(int index)
            {
            }
        }

        public int Count
        {
            get
            {
                return positions?.Length ?? 0;
            }
        }

        public bool IsValid()
        {
            return default;
        }

        public bool IsUserEdit() => userEdit;

        public SelectionData Clone()
        {
            return default;
        }

        /// <summary>
        /// ハッシュ（このセレクションデータの識別に利用される）
        /// </summary>
        /// <returns></returns>
        //public override int GetHashCode()
        //{
        //    if (IsValid() == false)
        //        return 0;

        //    // 頂点座標のハッシュをいくつかサンプリングする
        //    uint hash = 0;
        //    int len = positions.Length;
        //    int step = math.max(len / 4, 1);
        //    for (int i = 0; i < len; i += step)
        //    {
        //        hash += math.hash(positions[i]);
        //        hash += (uint)attributes[i].Value;
        //    }
        //    hash += math.hash(positions[len - 1]);
        //    hash += (uint)attributes[len - 1].Value;

        //    return (int)hash;
        //}

        /// <summary>
        /// ２つのセレクションデータが等しいか判定する
        /// （座標の詳細は見ない。属性の詳細は見る）
        /// </summary>
        /// <param name="sdata"></param>
        /// <returns></returns>
        public bool Compare(SelectionData sdata)
        {
            return default;
        }

        public void AddRange(float3[] addPositions, VertexAttribute[] addAttributes = null)
        {
        }

        public void Fill(VertexAttribute attr)
        {
        }

        //=========================================================================================
        public NativeArray<float3> GetPositionNativeArray()
        {
            return default;
        }

        public NativeArray<float3> GetPositionNativeArray(float4x4 transformMatrix)
        {
            return default;
        }

        public NativeArray<VertexAttribute> GetAttributeNativeArray()
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 属性座標をグリッドマップに登録して返す
        /// </summary>
        /// <param name="gridSize"></param>
        /// <param name="positions"></param>
        /// <param name="attributes"></param>
        /// <param name="move">移動属性を含めるかどうか</param>
        /// <param name="fix">固定属性を含めるかどうか</param>
        /// <param name="invalid">無効属性を含めるかどうか</param>
        /// <returns></returns>
        public static GridMap<int> CreateGridMapRun(
            float gridSize,
            in NativeArray<float3> positions,
            in NativeArray<VertexAttribute> attributes,
            bool move = true, bool fix = true, bool ignore = true, bool invalid = true
            )
        {
            return default;
        }

        [BurstCompile]
        struct CreateGridMapJob : IJob
        {
            public bool move;
            public bool fix;
            public bool ignore;
            public bool invalid;

            public NativeParallelMultiHashMap<int3, int> gridMap;
            public float gridSize;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attribute;

            public void Execute()
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// セレクションデータを結合する
        /// </summary>
        /// <param name="from"></param>
        public void Merge(SelectionData from)
        {
        }

        /// <summary>
        /// 頂点数の異なるセレクションデータを移植する
        /// </summary>
        /// <param name="from">移動元セレクションデータ</param>
        public void ConvertFrom(SelectionData from)
        {
        }

        [BurstCompile]
        struct ConvertSelectionJob : IJobParallelFor
        {
            public float gridSize;
            public float radius;

            // to
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> toPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<VertexAttribute> toAttributes;

            // from
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<int3, int> gridMap;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> fromPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> fromAttributes;

            public void Execute(int vindex)
            {
            }
        }
    }
}
