// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class TransformData
    {
        /// <summary>
        /// PreBuildの共有部分保存データ
        /// </summary>
        [System.Serializable]
        public class ShareSerializationData
        {
            public ExSimpleNativeArray<ExBitFlag8>.SerializationData flagArray;
            public ExSimpleNativeArray<float3>.SerializationData initLocalPositionArray;
            public ExSimpleNativeArray<quaternion>.SerializationData initLocalRotationArray;
        }

        public ShareSerializationData ShareSerialize()
        {
            return default;
        }

        public static TransformData ShareDeserialize(ShareSerializationData sdata)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// PreBuild固有部分の保存データ
        /// </summary>
        [System.Serializable]
        public class UniqueSerializationData : ITransform
        {
            public Transform[] transformArray;

            public void GetUsedTransform(HashSet<Transform> transformSet)
            {
            }

            public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
            {
            }
        }

        public UniqueSerializationData UniqueSerialize()
        {
            return default;
        }
    }
}
