// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    [System.Serializable]
    public class CurveSerializeData
    {
        /// <summary>
        /// Basic value.
        /// </summary>
        public float value;

        /// <summary>
        /// Use of curves.
        /// </summary>
        public bool useCurve;

        /// <summary>
        /// Animation curve.
        /// </summary>
        public AnimationCurve curve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

        public CurveSerializeData()
        {
        }

        public CurveSerializeData(float value)
        {
        }

        public CurveSerializeData(float value, float curveStart, float curveEnd, bool useCurve = true)
        {
        }

        public CurveSerializeData(float value, AnimationCurve curve)
        {
        }

        public void SetValue(float value)
        {
        }

        public void SetValue(float value, float curveStart, float curveEnd, bool useCurve = true)
        {
        }

        public void SetValue(float value, AnimationCurve curve)
        {
        }

        public void DataValidate(float min, float max)
        {
        }

        /// <summary>
        /// Get the current value of Time(0.0 ~ 1.0).
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public float Evaluate(float time)
        {
            return default;
        }

        /// <summary>
        /// カーブ情報をジョブで利用するための16個のfloat配列(float4x4)に変換して返す
        /// Convert the curve information into a 16 float array (float4x4) for use in the job and return it.
        /// </summary>
        /// <returns></returns>
        public float4x4 ConvertFloatArray()
        {
            return default;
        }

        public CurveSerializeData Clone()
        {
            return default;
        }
    }
}
