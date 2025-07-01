// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Configuration data for reduction.
    /// リダクション用の設定データ
    /// </summary>
    [System.Serializable]
    public class ReductionSettings : IDataValidate
    {
        /// <summary>
        /// Simple distance reduction (% of AABB maximum distance) (0.0 ~ 1.0).
        /// 単純な距離による削減(AABB最大距離の%)(0.0 ~ 1.0)
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 0.2f)]
        public float simpleDistance = 0.0f;

        /// <summary>
        /// Reduction by distance considering geometry (% of AABB maximum distance) (0.0 ~ 1.0).
        /// 形状を考慮した距離による削減(AABB最大距離の%)(0.0 ~ 1.0)
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 0.2f)]
        public float shapeDistance = 0.0f;

        //=========================================================================================
        public bool IsEnabled => Define.System.ReductionEnable;

        public float GetMaxConnectionDistance()
        {
            return default;
        }

        public ReductionSettings Clone()
        {
            return default;
        }

        public void DataValidate()
        {
        }

        /// <summary>
        /// エディタメッシュの更新を判定するためのハッシュコード
        /// （このハッシュは実行時には利用されない編集用のもの）
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return default;
        }

        public override string ToString()
        {
            return default;
        }
    }
}
