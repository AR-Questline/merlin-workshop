// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicaCloth2
{
    [System.Serializable]
    public class CustomSkinningSettings : IValid, IDataValidate, ITransform
    {
        /// <summary>
        /// valid state.
        /// 有効状態
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public bool enable = false;

        /// <summary>
        /// bones for skinning.
        /// Calculated from the parent-child structure line of bones registered here.
        /// スキニング用ボーン
        /// ここに登録されたボーンの親子構造ラインから算出される
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public List<Transform> skinningBones = new List<Transform>();

        public void DataValidate()
        {
        }

        public bool IsValid()
        {
            return default;
        }

        public CustomSkinningSettings Clone()
        {
            return default;
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

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
        }
    }
}
