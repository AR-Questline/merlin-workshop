// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace MagicaCloth2
{
    public class TransformManager : IManager, IValid
    {
        // フラグ
        internal const byte Flag_Read = 0x01;
        internal const byte Flag_WorldRotWrite = 0x02; // ワールド回転のみ書き込み
        internal const byte Flag_LocalPosRotWrite = 0x04; // ローカル座標・回転書き込み
        internal const byte Flag_Restore = 0x08; // 復元する
        internal const byte Flag_Enable = 0x10; // 有効状態
        internal ExNativeArray<ExBitFlag8> flagArray;

        /// <summary>
        /// 初期localPosition
        /// </summary>
        internal ExNativeArray<float3> initLocalPositionArray;

        /// <summary>
        /// 初期localRotation
        /// </summary>
        internal ExNativeArray<quaternion> initLocalRotationArray;

        /// <summary>
        /// ワールド座標
        /// </summary>
        internal ExNativeArray<float3> positionArray;

        /// <summary>
        /// ワールド回転
        /// </summary>
        internal ExNativeArray<quaternion> rotationArray;

        /// <summary>
        /// ワールド逆回転
        /// </summary>
        //internal ExNativeArray<quaternion> inverseRotationArray;

        /// <summary>
        /// ワールドスケール
        /// Transform.lossyScaleと等価
        /// </summary>
        internal ExNativeArray<float3> scaleArray;

        /// <summary>
        /// ローカル座標
        /// </summary>
        internal ExNativeArray<float3> localPositionArray;

        /// <summary>
        /// ローカル回転
        /// </summary>
        internal ExNativeArray<quaternion> localRotationArray;

        /// <summary>
        /// ワールド変換マトリックス
        /// </summary>
        internal ExNativeArray<float4x4> localToWorldMatrixArray;

        /// <summary>
        /// 接続チームID(0=なし)
        /// </summary>
        internal ExNativeArray<short> teamIdArray;

        /// <summary>
        /// 読み込み用トランスフォームアクセス配列
        /// この配列は上記の配列グループとインデックが同期している
        /// </summary>
        internal TransformAccessArray transformAccessArray;


        internal int Count => flagArray?.Count ?? 0;

        //=========================================================================================
        /// <summary>
        /// 書き込み用トランスフォームのデータ参照インデックス
        /// つまり上記配列へのインデックス
        /// </summary>
        //internal ExNativeArray<short> writeIndexArray;

        /// <summary>
        /// 書き込み用トランスフォームアクセス配列
        /// </summary>
        //internal TransformAccessArray writeTransformAccessArray;

        bool isValid;

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
        /// VirtualMeshのTransformDataを追加する
        /// </summary>
        /// <param name="tdata"></param>
        /// <returns></returns>
        internal DataChunk AddTransform(VirtualMeshContainer cmesh, int teamId)
        {
            return default;
        }

        /// <summary>
        /// Transformの領域のみ追加する
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        internal DataChunk AddTransform(int count, int teamId)
        {
            return default;
        }

        /// <summary>
        /// Transform１つを追加する
        /// </summary>
        /// <param name="t"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        internal DataChunk AddTransform(Transform t, ExBitFlag8 flag, int teamId)
        {
            return default;
        }

        /// <summary>
        /// Transform情報を書き換える
        /// </summary>
        /// <param name="t"></param>
        /// <param name="flag"></param>
        /// <param name="index"></param>
        internal void SetTransform(Transform t, ExBitFlag8 flag, int index, int teamId)
        {
        }

        /// <summary>
        /// Transform情報をコピーする
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        internal void CopyTransform(int fromIndex, int toIndex)
        {
        }

        /// <summary>
        /// トランスフォームを削除する
        /// </summary>
        /// <param name="c"></param>
        internal void RemoveTransform(DataChunk c)
        {
        }

        /// <summary>
        /// トランスフォームの有効状態を切り替える
        /// </summary>
        /// <param name="c"></param>
        /// <param name="sw">true=有効, false=無効</param>
        internal void EnableTransform(DataChunk c, bool sw)
        {
        }

        [BurstCompile]
        struct EnableTransformJob : IJob
        {
            public DataChunk chunk;
            public bool sw;
            public NativeArray<ExBitFlag8> flagList;

            public void Execute()
            {
            }
        }

        /// <summary>
        /// トランスフォームの有効状態を切り替える
        /// </summary>
        /// <param name="index"></param>
        /// <param name="sw">true=有効, false=無効</param>
        internal void EnableTransform(int index, bool sw)
        {
        }

        internal DataChunk Expand(DataChunk c, int newLength)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// Transformを初期姿勢で復元させるジョブを発行する
        /// </summary>
        /// <param name="count"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle RestoreTransform(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct RestoreTransformJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> localRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;


            public void Execute(int index, TransformAccess transform)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// トランスフォームを読み込むジョブを発行する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle ReadTransform(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct ReadTransformJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> positionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotationArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> scaleList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localPositionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> localRotationArray;
            //[Unity.Collections.WriteOnly]
            //public NativeArray<quaternion> inverseRotationArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float4x4> localToWorldMatrixArray;

            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            public void Execute(int index, TransformAccess transform)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// Transformを書き込むジョブを発行する
        /// </summary>
        /// <param name="count"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle WriteTransform(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct WriteTransformJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> worldPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> worldRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> localRotations;

            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            public void Execute(int index, TransformAccess transform)
            {
            }
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
        }
    }
}
