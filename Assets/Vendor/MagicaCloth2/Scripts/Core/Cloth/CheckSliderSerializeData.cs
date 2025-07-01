// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    [System.Serializable]
    public class CheckSliderSerializeData
    {
        /// <summary>
        /// slider value.
        /// </summary>
        public float value;

        /// <summary>
        /// Use
        /// </summary>
        public bool use;

        public CheckSliderSerializeData()
        {
        }

        public CheckSliderSerializeData(bool use, float value)
        {
        }

        public float GetValue(float unusedValue)
        {
            return default;
        }

        public void SetValue(bool use, float value)
        {
        }

        public void DataValidate(float min, float max)
        {
        }

        public CheckSliderSerializeData Clone()
        {
            return default;
        }
    }
}
