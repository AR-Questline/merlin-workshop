// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 風調整用シリアライズデータ
    /// Serialized data for wind adjustment.
    /// </summary>
    [System.Serializable]
    public class WindSettings : IValid, IDataValidate
    {
        /// <summary>
        /// 全体の影響率(1.0=100%)
        /// Overall impact rate (1.0=100%).
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 2.0f)]
        public float influence = 1.0f;

        /// <summary>
        /// 揺れの周期（値を大きくすると周期が速くなる）
        /// Period of shaking (the higher the value, the faster the period).
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 2.0f)]
        public float frequency = 1.0f;

        /// <summary>
        /// 乱流率
        /// turbulence rate.
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 2.0f)]
        public float turbulence = 1.0f;

        /// <summary>
        /// Sin波とNoise波のブレンド率(0.0:sin ~ 1.0:noise)
        /// Blend ratio of sine wave and noise wave (0.0:sin ~ 1.0:noise).
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float blend = 0.7f;

        /// <summary>
        /// ベースラインごとの同期率
        /// Synchronization rate by baseline.
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float synchronization = 0.7f;

        /// <summary>
        /// 深さ影響率
        /// Depth influence factor.
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float depthWeight = 0.0f;

        /// <summary>
        /// 移動時の風速
        /// Wind speed when moving.
        /// [OK] Runtime changes.
        /// [OK] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 10.0f)]
        public float movingWind = 0.0f;

        //=========================================================================================
        public bool IsValid()
        {
            return default;
        }

        public void DataValidate()
        {
        }

        public WindSettings Clone()
        {
            return default;
        }
    }
}
