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
    /// トライアングル曲げ制約
    /// </summary>
    public class TriangleBendingConstraint : IDisposable
    {
        /// <summary>
        /// ボリュームとして処理する判定フラグ
        /// </summary>
        const sbyte VOLUME_SIGN = 100;

        public enum Method
        {
            None = 0,

            /// <summary>
            /// ２面角による曲げ制御
            /// ２面は初期の角度を保つように移動する。ただし角度のみなので±の近い方に曲る。
            /// </summary>
            DihedralAngle = 1,

            /// <summary>
            /// 方向性ありの２面角曲げ制約
            /// 初期姿勢を保つように復元する
            /// </summary>
            DirectionDihedralAngle = 2,
        }

        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Restoring force (0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float stiffness;

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

        public struct TriangleBendingConstraintParams
        {
            public Method method;
            public float stiffness;

            public void Convert(SerializeData sdata)
            {
            }
        }

        //=========================================================================================
        [System.Serializable]
        public class ConstraintData : IValid
        {
            public ResultCode result;
            public ulong[] trianglePairArray;
            public float[] restAngleOrVolumeArray;
            public sbyte[] signOrVolumeArray;

            public int writeBufferCount;
            public uint[] writeDataArray;
            public uint[] writeIndexArray;

            public bool IsValid()
            {
                return default;
            }
        }

        /// <summary>
        /// トライアングルペアの４頂点をushortとしてulongにパッキングしたもの
        ///   v2 +
        ///     /|\
        /// v0 + | + v1
        ///     \|/
        ///   v3 +
        /// 上位ビットからv0-v1-v2-v3の順で並んでいる
        /// </summary>
        public ExNativeArray<ulong> trianglePairArray;

        /// <summary>
        /// トライアングルペアごとの復元角度もしくはボリューム値
        /// </summary>
        public ExNativeArray<float> restAngleOrVolumeArray;

        /// <summary>
        /// トライアングルペアごとの復元方向もしくはボリューム判定（VOLUME_SIGN(100)=このペアはボリュームである）
        /// </summary>
        public ExNativeArray<sbyte> signOrVolumeArray;

        /// <summary>
        /// トライアングルペアごとの結果書き込みローカルインデックス
        /// ４つのbyteを１つのuintに結合したもの
        /// </summary>
        public ExNativeArray<uint> writeDataArray;

        /// <summary>
        /// 頂点ごとの書き込みバッファの数と開始インデックス
        /// (上位10bit = カウンタ, 下位22bit = 開始インデックス）
        /// </summary>
        public ExNativeArray<uint> writeIndexArray;

        /// <summary>
        /// 頂点ごとの書き込みバッファ（集計用）
        /// writeIndexArrayに従う
        /// </summary>
        public ExNativeArray<float3> writeBuffer;

        public int DataCount => trianglePairArray?.Count ?? 0;

        /// <summary>
        /// ボリューム計算の浮動小数点誤差を回避するための倍数
        /// </summary>
        const float VolumeScale = 1000.0f;

        //=========================================================================================
        public TriangleBendingConstraint()
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
        /// <param name="proxyMesh"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static ConstraintData CreateData(VirtualMesh proxyMesh, in ClothParameters parameters)
        {
            return default;
        }

        static void InitVolume(VirtualMesh proxyMesh, int v0, int v1, int v2, int v3, out float volumeRest, out sbyte signFlag)
        {
            volumeRest = default(float);
            signFlag = default(sbyte);
        }

        static void InitDihedralAngle(VirtualMesh proxyMesh, int v0, int v1, int v2, int v3, out float restAngle, out sbyte signFlag)
        {
            restAngle = default(float);
            signFlag = default(sbyte);
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
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe internal JobHandle SolverConstraint(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct TriangleBendingJob : IJobParallelForDefer
        {
            public float4 simulationPower;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepTriangleBendIndexArray;

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
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // constraints
            [Unity.Collections.ReadOnly]
            public NativeArray<ulong> trianglePairArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> restAngleOrVolumeArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<sbyte> signOrVolumeArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> writeDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> writeIndexArray;

            // output
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> writeBuffer;

            // ベンドトライアングルペアごと
            public void Execute(int index)
            {
            }

            bool Volume(in float3x4 nextPosBuffer, in float4 invMassBuffer, float volumeRest, float stiffness, ref float3x4 addPosBuffer)
            {
                return default;
            }

            /// <summary>
            /// トライアングルベンド計算
            /// </summary>
            /// <param name="sign">方向性ありなら0以外</param>
            /// <param name="nextPosBuffer"></param>
            /// <param name="invMassBuffer"></param>
            /// <param name="restAngle"></param>
            /// <param name="stiffness"></param>
            /// <param name="addPosBuffer"></param>
            /// <returns></returns>
            bool DihedralAngle(float sign, in float3x4 nextPosBuffer, in float4 invMassBuffer, float restAngle, float stiffness, ref float3x4 addPosBuffer)
            {
                return default;
            }
        }

        [BurstCompile]
        struct SolveAggregateBufferJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepParticleIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            // constraint
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> writeIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> writeBuffer;

            // ステップパーティクルごと
            public void Execute(int index)
            {
            }
        }
    }
}
