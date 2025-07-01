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
    /// <summary>
    /// 最大距離制約
    /// 移動パーティクルが移動できる距離を自身のルートパーティクルとの距離から制限する
    /// </summary>
    public class TetherConstraint : IDisposable
    {
        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Maximum shrink limit (0.0 ~ 1.0).
            /// 0.0=do not shrink.
            /// 最大縮小限界(0.0 ~ 1.0)
            /// 0.0=縮小しない
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float distanceCompression;

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

        public struct TetherConstraintParams
        {
            /// <summary>
            /// 最大縮小割合(0.0 ~ 1.0)
            /// 0.0=縮小しない
            /// </summary>
            public float compressionLimit;

            /// <summary>
            /// 最大拡大割合(0.0 ~ 1.0)
            /// 0.0=拡大しない
            /// </summary>
            public float stretchLimit;

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
        /// <param name="clothBase"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe internal JobHandle SolverConstraint(JobHandle jobHandle)
        {
            return default;
        }

        [BurstCompile]
        struct TethreConstraintJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> stepParticleIndexArray;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRootIndices;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<short> teamIdArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // buffer
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> stepBasicPositionBuffer;

            // パーティクルごと
            public void Execute(int index)
            {
            }
        }
    }
}
