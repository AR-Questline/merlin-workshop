// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// VirtualMeshで利用されるTransform情報
    /// スレッドからの利用を考えてデータをTransformから分離しておく
    /// </summary>
    public struct VirtualMeshTransform
    {
        /// <summary>
        /// 識別名（最大２９文字）
        /// </summary>
        public FixedString32Bytes name;

        public int index;
        //public float3 localPosition;
        //public quaternion localRotation;
        //public float3 localScale;
        public float4x4 localToWorldMatrix;
        public float4x4 worldToLocalMatrix;

        public int parentIndex;

        //=========================================================================================
        public VirtualMeshTransform(Transform t) : this()
        {
        }

        public VirtualMeshTransform(Transform t, int index) : this(t)
        {
        }

        public VirtualMeshTransform Clone()
        {
            return default;
        }

        /// <summary>
        /// ワールド座標原点
        /// </summary>
        public static VirtualMeshTransform Origin
        {
            get
            {
                var mt = new VirtualMeshTransform();
                mt.name = "VirtualMesh Origin";
                //mt.localScale = 1;
                mt.localToWorldMatrix = float4x4.identity;
                mt.worldToLocalMatrix = float4x4.identity;
                mt.parentIndex = -1;
                return mt;
            }
        }

        /// <summary>
        /// ハッシュは名前から生成する
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return default;
        }

        public void Update(Transform t)
        {
        }

        //=========================================================================================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 TransformPoint(float3 pos)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 TransformVector(float3 vec)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 TransformDirection(float3 dir)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 InverseTransformPoint(float3 pos)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 InverseTransformVector(float3 vec)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 InverseTransformDirection(float3 dir)
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion InverseTransformRotation(quaternion rot)
        {
            return default;
        }

        /// <summary>
        /// このTransformのローカル座標をtoのローカル座標に変換するTransformを返す
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        public VirtualMeshTransform Transform(in VirtualMeshTransform to)
        {
            return default;
        }
    }
}
