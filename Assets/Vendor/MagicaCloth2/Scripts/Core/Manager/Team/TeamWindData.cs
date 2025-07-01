// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 風ゾーン１つの情報
    /// </summary>
    public struct TeamWindInfo : IValid
    {
        public int windId;
        public float time;
        public float main;
        public float3 direction;

        public bool IsValid()
        {
            return default;
        }

        public override string ToString()
        {
            return default;
        }

        public void DebugLog()
        {
        }
    }

    /// <summary>
    /// チームに影響する風ゾーンと移動風の情報
    /// </summary>
    public struct TeamWindData
    {
        public FixedList128Bytes<TeamWindInfo> windZoneList;
        public TeamWindInfo movingWind;


        public int ZoneCount => windZoneList.Length;

        public int IndexOf(int windId)
        {
            return default;
        }

        public void ClearZoneList()
        {
        }

        public void AddOrReplaceWindZone(TeamWindInfo windInfo, in TeamWindData oldWindData)
        {
        }

        public void RemoveWindZone(int windId)
        {
        }

        //public void DebugLog(int teamId)
        //{
        //    Debug.Log($"TeamWindData:{teamId}, zoneCnt:{ZoneCount}");
        //    for (int i = 0; i < ZoneCount; i++)
        //    {
        //        windZoneList[i].DebugLog();
        //    }

        //    if (movingWind.IsValid())
        //    {
        //        Debug.Log("Moving");
        //        movingWind.DebugLog();
        //    }
        //}
    }
}
