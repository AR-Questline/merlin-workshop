// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// PreBuildの保存アセットデータ
    /// </summary>
    [CreateAssetMenu(fileName = "Data", menuName = "MagicaCloth2/PreBuildScriptableObject")]
    public class PreBuildScriptableObject : ScriptableObject
    {
        /// <summary>
        /// 複数のPreBuildデータを格納可能
        /// </summary>
        public List<SharePreBuildData> sharePreBuildDataList = new List<SharePreBuildData>();

        //=========================================================================================
        public bool HasPreBuildData(string buildId)
        {
            return default;
        }

        public SharePreBuildData GetPreBuildData(string buildId)
        {
            return default;
        }

        public void AddPreBuildData(SharePreBuildData sdata)
        {
        }

        //=========================================================================================
        /// <summary>
        /// すべてのPreBuildデータをデシリアライズしてマネージャに登録します
        /// この処理は負荷が高いため事前に実行しておくことでクロスデータ利用時の負荷を軽減できます
        /// Deserialize all PreBuild data and register it with the manager.
        /// This process requires a high load, so running it in advance can reduce the load when using cross data.
        /// </summary>
        public void Warmup()
        {
        }
    }
}
