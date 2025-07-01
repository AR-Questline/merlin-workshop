// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
#if UNITY_2020
using UnityEditor.Experimental.SceneManagement;
#else
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// エディタ編集時のコンポーネント管理
    /// </summary>
    [InitializeOnLoad]
    public class ClothEditorManager
    {
        /// <summary>
        /// コンポーネント情報
        /// </summary>
        class ClothInfo
        {
            public ResultCode result = ResultCode.Empty;
            public bool building;
            public GizmoType gizmoType;
            public ClothBehaviour component;
            public int componentHash;
            public VirtualMeshContainer editMeshContainer;
            public int nextBuildHash;
            public int importCount;
        }

        static Dictionary<int, ClothInfo> editClothDict = new Dictionary<int, ClothInfo>();

        static List<int> destroyList = new List<int>();
        static List<ClothInfo> drawList = new List<ClothInfo>();
        static CancellationTokenSource cancelToken = new CancellationTokenSource();

        static bool isValid = false;

        static internal Action OnEditMeshBuildComplete;

        //=========================================================================================
        static ClothEditorManager()
        {
        }

        /// <summary>
        /// エディタの実行状態が変更された場合に呼び出される
        /// </summary>
        [InitializeOnLoadMethod]
        static void PlayModeStateChange()
        {
        }

        /// <summary>
        /// スクリプトコンパイル開始
        /// </summary>
        /// <param name="obj"></param>
        static void OnStartCompile(object obj)
        {
        }

        /// <summary>
        /// ビルド完了時
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pathToBuiltProject"></param>
        [PostProcessBuildAttribute(1)]
        static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
        }

        /// <summary>
        /// プレハブステージが閉じる時
        /// </summary>
        /// <param name="obj"></param>
        static void OnPrefabStageClosing(PrefabStage pstage)
        {
        }

        /// <summary>
        /// Undo/Redo実行時
        /// </summary>
        static void OnUndoRedoPerformed()
        {
        }

        /// <summary>
        /// MagidaClothコンポーネントの登録および編集メッシュの作成/更新
        /// </summary>
        /// <param name="component"></param>
        public static void RegisterComponent(ClothBehaviour component, GizmoType gizmoType, bool forceUpdate = false)
        {
        }

        public static VirtualMeshContainer GetEditMeshContainer(ClothBehaviour comp)
        {
            return default;
        }

        /// <summary>
        /// 現在のコンポーネント状態を返す
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static ResultCode GetResultCode(ClothBehaviour comp)
        {
            return default;
        }

        static void Dispose()
        {
        }

        /// <summary>
        /// コンポーネントの削除チェック
        /// </summary>
        static void DestroyCheck()
        {
        }

        /// <summary>
        /// アセット更新にともなう編集用メッシュの更新
        /// </summary>
        /// <param name="importedAssets"></param>
        public static void UpdateFromAssetImport(string[] importedAssets)
        {
        }

        /// <summary>
        /// 強制的にすべてのコンポーネントの更新フラグを立てる
        /// </summary>
        static void ForceUpdateAllComponents()
        {
        }

        //=========================================================================================
        /// <summary>
        /// 編集用メッシュの作成/更新
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cloth"></param>
        /// <param name="createSelectionData"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        static async Task CreateOrUpdateEditMesh(int id, MagicaCloth cloth, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// セレクションデータをシリアライズ化する
        /// </summary>
        /// <param name="cloth"></param>
        /// <param name="selectionData"></param>
        public static void ApplySelectionData(MagicaCloth cloth, SelectionData selectionData)
        {
        }


        /// <summary>
        /// 編集メッシュから自動でセレクションデータを生成する（メインスレッドのみ）
        /// </summary>
        /// <param name="sdata"></param>
        /// <param name="emesh"></param>
        /// <param name="setupList"></param>
        /// <returns></returns>
        public static SelectionData CreateAutoSelectionData(MagicaCloth cloth, ClothSerializeData sdata, VirtualMesh emesh)
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// シーンビューへのギズモ描画
        /// </summary>
        /// <param name="sceneView"></param>
        static void OnSceneGUI(SceneView sceneView)
        {
        }
    }
}
