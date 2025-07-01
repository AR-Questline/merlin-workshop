// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// プリセットに関するユーティリティ
    /// </summary>
    public static class ClothPresetUtility
    {
        const string prefix = "MC2_Preset";
        const string configName = "MC2 preset folder";

        public static void DrawPresetButton(MagicaCloth cloth, ClothSerializeData sdata)
        {
        }

        static string GetComponentTypeName(ClothSerializeData sdata)
        {
            return default;
        }

        class PresetInfo
        {
            public string presetPath;
            public string presetName;
            public TextAsset text;
        }

        private static void CreatePresetPopupMenu(MagicaCloth cloth, ClothSerializeData sdata)
        {
        }

        /// <summary>
        /// プリセットファイル保存
        /// </summary>
        /// <param name="clothParam"></param>
        private static void SavePreset(ClothSerializeData sdata)
        {
        }

        /// <summary>
        /// プリセットファイル読み込み
        /// </summary>
        /// <param name="clothParam"></param>
        private static void LoadPreset(MagicaCloth cloth, ClothSerializeData sdata)
        {
        }

        /// <summary>
        /// プリセットファイル読み込み後処理
        /// </summary>
        /// <param name="cloth"></param>
        private static void LoadPresetFinish(MagicaCloth cloth)
        {
        }
    }
}
