// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public class MotionConstraint : IDisposable
    {
        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Whether or not to use maximum travel range
            /// 最大移動範囲
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useMaxDistance;

            /// <summary>
            /// Maximum travel range.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData maxDistance;

            /// <summary>
            /// Use of backstop.
            /// バックストップ使用の有無
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useBackstop;

            /// <summary>
            /// Backstop sphere radius.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.01f, 10.0f)]
            public float backstopRadius;

            /// <summary>
            /// Distance from vertex to backstop sphere.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData backstopDistance;

            /// <summary>
            /// repulsive force(0.0 ~ 1.0)
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

        public struct MotionConstraintParams
        {
            /// <summary>
            /// 最大移動範囲
            /// </summary>
            public bool useMaxDistance;
            public float4x4 maxDistanceCurveData;
            //public float maxDistanceOffset;

            /// <summary>
            /// バックストップ距離
            /// </summary>
            public bool useBackstop;
            public float backstopRadius;
            public float4x4 backstopDistanceCurveData;

            // stiffness
            public float stiffness;

            public void Convert(SerializeData sdata, ClothProcess.ClothType clothType)
            {
            }
        }

        public void Dispose()
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
        struct MotionConstraintJob : IJobParallelForDefer
        {
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
            public NativeArray<float> vertexDepths;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> collisionNormalArray;


            public void Execute(int index)
            {
            }
        }
    }
}
