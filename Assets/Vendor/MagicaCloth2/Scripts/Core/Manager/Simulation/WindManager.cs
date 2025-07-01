// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 風ゾーン管理マネージャ
    /// </summary>
    public class WindManager : IManager, IValid
    {
        public const int Flag_Valid = 0; // データの有効性
        public const int Flag_Enable = 1; // 動作状態
        public const int Flag_Addition = 2; // 加算風

        /// <summary>
        /// 風ゾーンの管理データ
        /// </summary>
        public struct WindData
        {
            public BitField32 flag;

            public MagicaWindZone.Mode mode;

            /// <summary>
            /// Global:(none)
            /// Box   :(x, y, z)
            /// Sphere:(radius, radius, radius)
            /// </summary>
            public float3 size;

            public float main;
            public float turbulence;
            public float zoneVolume;

            public float3 worldWindDirection;

            public float3 worldPositin;
            public quaternion worldRotation;
            public float3 worldScale;
            public float4x4 worldToLocalMatrix;
            public float4x4 attenuation;

            public bool IsValid() => flag.IsSet(Flag_Valid);
            public bool IsEnable() => flag.IsSet(Flag_Enable);
            public bool IsAddition() => flag.IsSet(Flag_Addition);
        }
        public ExNativeArray<WindData> windDataArray;

        public int WindCount => windDataArray?.Count ?? 0;

        bool isValid;

        /// <summary>
        /// WindIDとゾーンコンポーネントの関連辞書
        /// </summary>
        Dictionary<int, MagicaWindZone> windZoneDict = new Dictionary<int, MagicaWindZone>();

        //=========================================================================================
        public void Dispose()
        {
        }

        public void EnterdEditMode()
        {
        }

        public void Initialize()
        {
        }

        public bool IsValid()
        {
            return default;
        }

        //=========================================================================================
        public int AddWind(MagicaWindZone windZone)
        {
            return default;
        }

        public void RemoveWind(int windId)
        {
        }

        public void SetEnable(int windId, bool sw)
        {
        }

        //=========================================================================================
        /// <summary>
        /// 毎フレーム常に実行する更新
        /// </summary>
        internal void AlwaysWindUpdate()
        {
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
        }
    }
}
