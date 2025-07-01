// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 頂点ペイントウインドウ
    /// </summary>
    [InitializeOnLoad]
    public class ClothPainter
    {
        public enum PaintMode
        {
            None,

            /// <summary>
            /// Move/Fixed/Ignore/Invalid
            /// </summary>
            Attribute,

            /// <summary>
            /// Max Distance/Backstop
            /// </summary>
            Motion,
        }

        static PaintMode paintMode = PaintMode.None;

        /// <summary>
        /// 編集対象のクロスコンポーネント
        /// </summary>
        static MagicaCloth cloth = null;

        /// <summary>
        /// 編集対象のクロスEditorクラス
        /// </summary>
        static MagicaClothEditor clothEditor = null;

        /// <summary>
        /// 編集対象のエディットメッシュ
        /// </summary>
        static VirtualMeshContainer editMeshContainer = null;

        /// <summary>
        /// 編集対象のセレクションデータ
        /// </summary>
        static SelectionData selectionData = null;

        /// <summary>
        /// 編集開始時のセレクションデータ（コピー）
        /// </summary>
        static SelectionData initSelectionData = null;

        internal const int WindowWidth = 200;
        internal const int WindowHeight = 200;

        //=========================================================================================
        const int PointFlag_Selecting = 1; // 選択中

        internal struct Point : IComparable<Point>
        {
            public int vindex;
            public float distance;
            public BitField32 flag;

            public int CompareTo(Point other)
            {
                return default;
            }
        }
        static NativeList<Point> dispPointList;
        static NativeArray<float3> pointWorldPositions;
        static VirtualMeshRaycastHit rayhit = default;
        static bool oldShowAll = false;
        static bool forceUpdate = false;

        //=========================================================================================
        static ClothPainter()
        {
        }

        /// <summary>
        /// ペイント開始
        /// </summary>
        /// <param name="clothComponent"></param>
        public static void EnterPaint(PaintMode mode, MagicaClothEditor editor, MagicaCloth clothComponent, VirtualMeshContainer cmesh, SelectionData sdata)
        {
        }

        /// <summary>
        /// ペイント終了
        /// </summary>
        public static void ExitPaint()
        {
        }

        /// <summary>
        /// Undo/Redo実行後のコールバック
        /// </summary>
        static void UndoRedoCallback()
        {
        }

        //=========================================================================================
        /// <summary>
        /// 指定クロスコンポーネントを編集中かどうか
        /// </summary>
        /// <param name="clothComponent"></param>
        /// <returns></returns>
        public static bool HasEditCloth(MagicaCloth clothComponent)
        {
            return default;
        }

        /// <summary>
        /// 編集中かどうか
        /// </summary>
        /// <returns></returns>
        public static bool IsPainting()
        {
            return default;
        }

        //=========================================================================================
        static void OnGUI(SceneView sceneView)
        {
        }

        /// <summary>
        /// ポイントデータを更新する
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="cam"></param>
        /// <param name="ray"></param>
        /// <param name="showAll"></param>
        /// <param name="brushSize"></param>
        static void UpdatePoint(bool through, Transform ct, Camera cam, Ray ray, bool showAll, float brushSize, float3 brushPos, float pointSize)
        {
        }

        static JobHandle CreateDispPointList(
            bool through, Transform ct, Camera cam,
            bool showAll, float brushSize, float3 brushPosition, JobHandle jobHandle = default
            )
        {
            return default;
        }

        [BurstCompile]
        struct CreateDispPointListJob : IJobParallelFor
        {
            public bool through;
            public float4x4 LtoW;
            public bool showAll;

            public float3 cameraPosition;
            public float3 cameraDirection;
            public float4x4 cameraProjectionMatrix;
            public float4x4 worldToCameraMatrix;
            public float cameraAspectRatio;

            public bool useBrush;
            public float3 brushPosition;
            public float brushSize;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> pointWorldPositions;
            [Unity.Collections.WriteOnly]
            public NativeList<Point>.ParallelWriter dispPointList;

            public void Execute(int vindex)
            {
            }
        }

        [BurstCompile]
        struct SortDispPointJob : IJob
        {
            public NativeList<Point> dispPointList;

            public void Execute()
            {
            }
        }

        /// <summary>
        /// 渡されたpointListをカメラからの距離の降順にソートして返す
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="positonOffset"></param>
        /// <param name="LtoW">positionをワールド座標変換するマトリックス</param>
        /// <param name="camPos">カメラワールド座標</param>
        /// <param name="pointList"></param>
        internal static void CalcPointCameraDistance(NativeArray<float3> positions, int positonOffset, float4x4 LtoW, float3 camPos, NativeList<Point> pointList)
        {
        }

        [BurstCompile]
        struct CalcCameraDistanceJob : IJobParallelFor
        {
            public float4x4 LtoW;

            public float3 cameraPosition;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;

            [NativeDisableParallelForRestriction]
            public NativeList<Point> pointList;
            public int positionOffset;

            public void Execute(int index)
            {
            }
        }

        /// <summary>
        /// 選択中ポイントに属性を付与する
        /// </summary>
        /// <param name="attribute"></param>
        static void ApplyPaint(ClothPainterWindowData windata, NativeList<Point> applyPointList)
        {
        }

        /// <summary>
        /// セレクションデータをシリアライズする
        /// </summary>
        static void ApplySelectionData()
        {
        }

        /// <summary>
        /// 塗りつぶし
        /// </summary>
        /// <param name="windata"></param>
        static void Fill(ClothPainterWindowData windata)
        {
        }

        static void DoWindow(int unusedWindowID)
        {
        }
    }

    //=============================================================================================
    /// <summary>
    /// ペイントウインドウの保持データ
    /// ScriptableSingletonを利用することによりエディタ終了時までデータを保持できる
    /// </summary>
    public class ClothPainterWindowData : ScriptableSingleton<ClothPainterWindowData>
    {
        /// <summary>
        /// ウインドウの位置とサイズ
        /// </summary>
        public Rect windowRect = new Rect(100, 100, ClothPainter.WindowWidth, ClothPainter.WindowHeight);

        /// <summary>
        /// 表示ポイントサイズ
        /// </summary>
        public float drawPointSize = 0.02f;

        /// <summary>
        /// ブラシサイズ
        /// </summary>
        public float brushSize = 0.05f;

        /// <summary>
        /// ブラシサイズ（透過モード時）
        /// シーンカメラ垂直サイズの(%)
        /// </summary>
        public float brushSizeThrough = 0.1f;

        /// <summary>
        /// 裏面カリング
        /// </summary>
        public bool backFaceCulling = true;

        /// <summary>
        /// 形状を表示
        /// </summary>
        public bool showShape = true;

        /// <summary>
        /// Zテスト
        /// </summary>
        public bool zTest = false;

        /// <summary>
        /// 透過モード
        /// </summary>
        public bool through = false;

        /// <summary>
        /// 現在アクティブなポイント属性(0=Move/1=Fixed/2=Ignore/3=Invalid)
        /// VertexAttributeとは異なるので注意！
        /// </summary>
        public int editAttribute = 0;

        /// <summary>
        /// 現在アクティブなモーション制約(0=Valid, 1=Invalid)
        /// </summary>
        public int editMotion = 0;
    }
}
