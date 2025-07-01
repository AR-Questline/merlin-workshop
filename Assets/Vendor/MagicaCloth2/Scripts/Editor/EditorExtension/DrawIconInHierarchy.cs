// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// ヒエラルキーへアイコンの表示
    /// </summary>
    [InitializeOnLoad]
    public class DrawIconInHierarchy
    {
        const int iconSize = 16;

        static DrawIconInHierarchy()
        {
        }

        static void DrawIcon(int instanceId, Rect rect)
        {
        }
    }

    /// <summary>
    /// テキストのサイズを取得
    /// </summary>
    public static class GUIStyleExtensions
    {
        public static Vector2 CalcSize(this GUIStyle self, string text)
        {
            return default;
        }
    }
}
