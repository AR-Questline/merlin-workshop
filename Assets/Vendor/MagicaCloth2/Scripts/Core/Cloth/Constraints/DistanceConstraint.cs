// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 距離制約
    /// </summary>
    public class DistanceConstraint : IDisposable
    {
        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Overall connection stiffness (0.0 ~ 1.0).
            /// 全体的な接続の剛性(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData stiffness;

            public SerializeData()
            {
            }

            public void DataValidate()
            {
            }

            public SerializeData Clone()
            {
                return default;
            }
        }

        public struct DistanceConstraintParams
        {
            /// <summary>
            /// 剛性
            /// </summary>
            public float4x4 restorationStiffness;

            /// <summary>
            /// 距離制約の速度減衰率(0.0 ~ 1.0)
            /// </summary>
            public float velocityAttenuation;

            public void Convert(SerializeData sdata, ClothProcess.ClothType clothType)
            {
            }
        }

        //=========================================================================================
        /// <summary>
        /// 接続タイプ数
        /// </summary>
        public const int TypeCount = 2;

        /// <summary>
        /// 制約データ
        /// </summary>
        [System.Serializable]
        public class ConstraintData : IValid
        {
            public ResultCode result;

            public uint[] indexArray;
            public ushort[] dataArray;
            public float[] distanceArray;

            public bool IsValid()
            {
                return default;
            }
        }

        /// <summary>
        /// パーティクルごとのデータ開始インデックスとデータ数を１つのuint(10-22)にパックしたもの
        /// </summary>
        public ExNativeArray<uint> indexArray;

        /// <summary>
        /// 接続パーティクルリスト
        /// </summary>
        public ExNativeArray<ushort> dataArray;

        /// <summary>
        /// 対象への基準距離リスト
        /// ただし符号によりタイプを示す(+:Vertical, -:Horizontal)
        /// </summary>
        public ExNativeArray<float> distanceArray;

        /// <summary>
        /// 登録データ数を返す
        /// </summary>
        public int DataCount => indexArray?.Count ?? 0;

        //=========================================================================================
        public DistanceConstraint()
        {
        }

        public void Dispose()
        {
        }

        public override string ToString()
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 制約データの作成
        /// </summary>
        /// <param name="cbase"></param>
        public static ConstraintData CreateData(VirtualMesh proxyMesh, in ClothParameters parameters)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 制約データを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Register(ClothProcess cprocess)
        {
        }

        /// <summary>
        /// 制約データを解除する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Exit(ClothProcess cprocess)
        {
        }

        //=========================================================================================
        /// <summary>
        /// 制約の解決
        /// </summary>
        /// <param name="clothBase"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe internal JobHandle SolverConstraint(JobHandle jobHandle)
        {
            return default;
        }

        /// <summary>
        /// 距離制約の解決
        /// </summary>
        [BurstCompile]
        struct DistanceConstraintJob : IJobParallelForDefer
        {
            public float4 simulationPower;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepParticleIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // buffer
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> stepBasicPositionBuffer;

            // constrants
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> indexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> dataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> distanceArray;

            // ステップ有効パーティクルごと
            public void Execute(int index)
            {
            }
        }
    }
}
