// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth2
{
    /// <summary>
    /// インポートアセットの判定
    /// </summary>
    public class ClothAssetPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// データベースにアセットが追加／削除／移動／更新された場合にその完了後に一度呼び出される
        /// </summary>
        /// <param name="importedAssets"></param>
        /// <param name="deletedAssets"></param>
        /// <param name="movedAssets"></param>
        /// <param name="movedFromAssetPaths"></param>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
        }
    }
}
