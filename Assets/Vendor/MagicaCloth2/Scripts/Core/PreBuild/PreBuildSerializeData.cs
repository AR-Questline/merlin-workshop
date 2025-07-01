// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// PreBuildの保存データ
    /// </summary>
    [System.Serializable]
    public class PreBuildSerializeData : ITransform
    {
        /// <summary>
        /// 有効状態
        /// Valid state.
        /// </summary>
        public bool enabled = false;

        /// <summary>
        /// ビルド識別ID
        /// </summary>
        public string buildId;

        /// <summary>
        /// ビルドデータの共有部分
        /// このデータは複数のインスタンスで共有されるためScriptableObjectとして外部アセットとして保存される
        /// </summary>
        public PreBuildScriptableObject preBuildScriptableObject = null;

        /// <summary>
        /// ビルドデータの固有部分
        /// このデータはインスタンスごとに固有となる
        /// </summary>
        public UniquePreBuildData uniquePreBuildData;

        //=========================================================================================
        /// <summary>
        /// PreBuild利用の有無
        /// </summary>
        /// <returns></returns>
        public bool UsePreBuild() => enabled;

        /// <summary>
        /// PreBuildデータの検証
        /// </summary>
        /// <returns></returns>
        public ResultCode DataValidate()
        {
            return default;
        }

        public SharePreBuildData GetSharePreBuildData()
        {
            return default;
        }

        /// <summary>
        /// ビルドIDを生成する(英数字８文字)
        /// </summary>
        /// <returns></returns>
        public static string GenerateBuildID()
        {
            return default;
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
        }
    }
}
