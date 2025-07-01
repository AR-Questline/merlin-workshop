// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// PreBuildDataの作成
    /// </summary>
    public static class PreBuildDataCreation
    {
        /// <summary>
        /// PreBuildDataを作成しアセットとして保存する.
        /// Create PreBuildData and save it as an asset.
        /// </summary>
        /// <param name="cloth"></param>
        /// <param name="useNewSaveDialog">Show save dialog if ScriptableObject does not exist.</param>
        /// <returns></returns>
        public static ResultCode CreatePreBuildData(MagicaCloth cloth, bool useNewSaveDialog = true)
        {
            return default;
        }

        static void MakePreBuildData(MagicaCloth cloth, string buildId, SharePreBuildData sharePreBuildData, UniquePreBuildData uniquePreBuildData)
        {
        }
    }
}
