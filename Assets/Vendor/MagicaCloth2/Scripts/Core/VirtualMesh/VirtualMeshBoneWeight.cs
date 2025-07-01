// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// VirtualMeshで利用される頂点のボーンウエイト
    /// これはUnity.BoneWeight構造体を再マッピングしたもの
    /// </summary>
    public struct VirtualMeshBoneWeight
    {
        public float4 weights;
        public int4 boneIndices;

        public bool IsValid => weights[0] >= 1e-06f;


        public VirtualMeshBoneWeight(int4 boneIndices, float4 weights) : this()
        {
        }

        /// <summary>
        /// 有効なウエイト数
        /// </summary>
        public int Count
        {
            get
            {
                if (weights[3] > 0.0f)
                    return 4;
                if (weights[2] > 0.0f)
                    return 3;
                if (weights[1] > 0.0f)
                    return 2;
                if (weights[0] > 0.0f)
                    return 1;

                return 0;
            }
        }

        public void AddWeight(int boneIndex, float weight)
        {
        }

        public void AddWeight(in VirtualMeshBoneWeight bw)
        {
        }

        /// <summary>
        /// ウエイトを合計１に調整する
        /// </summary>
        public void AdjustWeight()
        {
        }

        public override string ToString()
        {
            return default;
        }
    }
}
