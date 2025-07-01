// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

using System;
using Unity.Jobs;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// スプリング制約
    /// 固定パーティクルにスプリングの機能を追加する
    /// 現在はBoneSpringのみで利用
    /// </summary>
    public class SpringConstraint : IDisposable
    {
        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Use of springs
            /// スプリングの利用
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useSpring;

            /// <summary>
            /// spring strength.(0.0 ~ 1.0)
            /// スプリングの強さ(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.001f, 0.2f)]
            public float springPower;

            /// <summary>
            /// Distance that can be moved from the origin.
            /// 原点から移動可能な距離
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 0.5f)]
            public float limitDistance;

            /// <summary>
            /// Movement restriction in normal direction.(0.0 ~ 1.0)
            /// 法線方向に対しての移動制限(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float normalLimitRatio;

            /// <summary>
            /// De-synchronize each spring.(0.0 ~ 1.0)
            /// 各スプリングの非同期化(0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float springNoise;

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

        public struct SpringConstraintParams
        {
            /// <summary>
            /// スプリングの強さ(0.0 ~ 1.0)
            /// スプリング未使用時は0.0
            /// </summary>
            public float springPower;

            /// <summary>
            /// 原点からの移動制限距離
            /// </summary>
            public float limitDistance;

            /// <summary>
            /// 法線方向に対する移動制限
            /// </summary>
            public float normalLimitRatio;

            /// <summary>
            /// 各スプリングの非同期率(0.0 ~ 1.0)
            /// </summary>
            public float springNoise;

            public void Convert(SerializeData sdata, ClothProcess.ClothType clothType)
            {
            }
        }

        public void Dispose()
        {
        }

        //=========================================================================================
        unsafe internal JobHandle SolverConstraint(JobHandle jobHandle)
        {
            return default;
        }
    }
}
