// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;


namespace MagicaCloth2
{
    /// <summary>
    /// クロスコンポーネントのギズモ表示
    /// </summary>
    public static class ClothEditorUtility
    {
        public static readonly Color MovePointColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        //public static readonly Color MovePointColor = Color.white;
        public static readonly Color FixedPointColor = new Color(1.0f, 0.0f, 0.0f, 0.8f);
        public static readonly Color InvalidPointColor = new Color(0.0f, 0.0f, 0.0f, 0.8f);
        public static readonly Color AngleLimitConeColor = new Color(0.0f, 0.349f, 0.725f, 0.325f);
        public static readonly Color AngleLimitWireColor = new Color(0.0f, 0.843f, 1.0f, 0.313f);
        public static readonly Color BaseTriangleColor = new Color(204 / 255f, 153 / 255f, 255 / 255f);
        public static readonly Color BaseLineColor = new Color(153 / 255f, 204 / 255f, 255 / 255f);
        public static readonly Color TriangleColor = new Color(1.0f, 0.0f, 0.8f, 1f);
        public static readonly Color LineColor = Color.cyan;
        public static readonly Color SkininngLine = Color.yellow;

        /// <summary>
        /// クロスペイント時のギズモ表示設定
        /// </summary>
        internal static readonly ClothDebugSettings PaintSettings = new ClothDebugSettings()
        {
            enable = true,
            position = false,
            collider = false,
            //basicShape = true,
            animatedShape = true,
        };


        //=========================================================================================
        static List<Vector3> positionBuffer0 = new List<Vector3>(1024);
        static List<Vector3> positionBuffer1 = new List<Vector3>(1024);
        static List<Vector3> positionBuffer2 = new List<Vector3>(1024);
        static List<int> segmentBuffer0 = new List<int>(2048);
        static List<int> segmentBuffer1 = new List<int>(2048);
        static List<int> segmentBuffer2 = new List<int>(2048);

        //=========================================================================================
        /// <summary>
        /// 編集時のクロスデータの表示（すべてHandlesクラスで描画）
        /// </summary>
        /// <param name="editMesh"></param>
        /// <param name="drawSettings"></param>
        public static void DrawClothEditor(VirtualMeshContainer editMeshContainer, ClothDebugSettings drawSettings, ClothSerializeData serializeData, bool selected, bool direction, bool paint)
        {
        }

        /// <summary>
        /// 実行時のクロスデータの表示（すべてHandlesクラスで描画）
        /// </summary>
        /// <param name="cprocess"></param>
        /// <param name="drawSettings"></param>
        public static void DrawClothRuntime(ClothProcess cprocess, ClothDebugSettings drawSettings, bool selected)
        {
        }
    }
}
