// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 角度復元/角度制限制約
    /// 内部処理がほぼ同じなため１つに統合
    /// </summary>
    public class AngleConstraint : IDisposable
    {
        /// <summary>
        /// angle restoration.
        /// 角度復元
        /// </summary>
        [System.Serializable]
        public class RestorationSerializeData : IDataValidate
        {
            /// <summary>
            /// Presence or absence of use.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useAngleRestoration;

            /// <summary>
            /// resilience.
            /// 復元力
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData stiffness;

            /// <summary>
            /// Velocity decay during restoration.
            /// 復元時の速度減衰
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float velocityAttenuation;

            /// <summary>
            /// Directional Attenuation of Gravity.
            /// Note that this attenuation occurs even if the gravity is 0!
            /// 復元の重力方向減衰
            /// この減衰は重力が０でも発生するので注意！
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float gravityFalloff;

            public RestorationSerializeData()
            {
            }

            public void DataValidate()
            {
            }

            public RestorationSerializeData Clone()
            {
                return default;
            }
        }

        /// <summary>
        /// angle limit.
        /// 角度制限
        /// </summary>
        [System.Serializable]
        public class LimitSerializeData : IDataValidate
        {
            /// <summary>
            /// Presence or absence of use.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useAngleLimit;

            /// <summary>
            /// Limit angle (deg).
            /// 制限角度(deg)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData limitAngle;

            /// <summary>
            /// Standard stiffness.
            /// 基準剛性
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float stiffness;

            public LimitSerializeData()
            {
            }

            public void DataValidate()
            {
            }

            public LimitSerializeData Clone()
            {
                return default;
            }
        }

        //=========================================================================================
        public struct AngleConstraintParams
        {
            public bool useAngleRestoration;

            /// <summary>
            /// 角度復元力
            /// </summary>
            public float4x4 restorationStiffness;

            /// <summary>
            /// 角度復元速度減衰
            /// </summary>
            public float restorationVelocityAttenuation;

            /// <summary>
            /// 角度復元の重力方向減衰
            /// </summary>
            public float restorationGravityFalloff;


            public bool useAngleLimit;

            /// <summary>
            /// 制限角度(deg)
            /// </summary>
            public float4x4 limitCurveData;

            /// <summary>
            /// 角度制限剛性
            /// </summary>
            public float limitstiffness;

            public void Convert(RestorationSerializeData restorationData, LimitSerializeData limitData)
            {
            }
        }

        //=========================================================================================
        NativeArray<float> lengthBuffer;
        NativeArray<float3> localPosBuffer;
        NativeArray<quaternion> localRotBuffer;
        NativeArray<quaternion> rotationBuffer;
        NativeArray<float3> restorationVectorBuffer;


        //=========================================================================================
        public AngleConstraint()
        {
        }

        public void Dispose()
        {
        }

        internal void WorkBufferUpdate()
        {
        }

        public override string ToString()
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// 制約の解決
        /// </summary>
        /// <param name="clothBase"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal unsafe JobHandle SolverConstraint(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct AngleConstraintJob : IJobParallelForDefer
        {
            public float4 simulationPower;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepBaseLineIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexParentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineStartDataIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineDataCounts;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineData;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // temp
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> stepBasicPositionBuffer;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> stepBasicRotationBuffer;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> lengthBufferArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> localPosBufferArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> localRotBufferArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<quaternion> rotationBufferArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> restorationVectorBufferArray;

            // ベースラインごと
            public void Execute(int index)
            {
            }
        }
    }
}
