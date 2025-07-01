// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using System.Linq;
using Unity.Profiling;
using UnityEngine.PlayerLoop;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEditor.Compilation;
using UnityEditor;
#if UNITY_2023_1_OR_NEWER
using UnityEditor.Build;
#endif
#endif

// コードストリッピングを無効化する 
[assembly: AlwaysLinkAssembly]

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothマネージャ
    /// </summary>
    public static partial class MagicaManager
    {
        //=========================================================================================
        /// <summary>
        /// 登録マネージャリスト
        /// </summary>
        static List<IManager> managers = null;

        public static TimeManager Time => managers?[0] as TimeManager;
        public static TeamManager Team => managers?[1] as TeamManager;
        public static ClothManager Cloth => managers?[2] as ClothManager;
        public static RenderManager Render => managers?[3] as RenderManager;
        public static TransformManager Bone => managers?[4] as TransformManager;
        public static VirtualMeshManager VMesh => managers?[5] as VirtualMeshManager;
        public static SimulationManager Simulation => managers?[6] as SimulationManager;
        public static ColliderManager Collider => managers?[7] as ColliderManager;
        public static WindManager Wind => managers?[8] as WindManager;
        public static PreBuildManager PreBuild => managers?[9] as PreBuildManager;

        //=========================================================================================
        // player loop delegate
        public delegate void UpdateMethod();

        /// <summary>
        /// フレームの開始時、すべてのEarlyUpdateの後、FixedUpdate()の前
        /// </summary>
        public static UpdateMethod afterEarlyUpdateDelegate;

        /// <summary>
        /// FixedUpdate()の後
        /// </summary>
        public static UpdateMethod afterFixedUpdateDelegate;

        /// <summary>
        /// Update()の後
        /// </summary>
        public static UpdateMethod afterUpdateDelegate;

        /// <summary>
        /// LateUpdate()後の遅延処理後、yield nullの後
        /// </summary>
        public static UpdateMethod afterDelayedDelegate;

        /// <summary>
        /// レンダリング完了後
        /// </summary>
        public static UpdateMethod afterRenderingDelegate;

        /// <summary>
        /// 汎用的な定期更新
        /// ゲーム実行中はUpdate()後に呼び出さる。
        /// エディタではEditorApplication.updateデリゲートにより呼び出される。
        /// </summary>
        public static UpdateMethod defaultUpdateDelegate;


        //=========================================================================================
        static volatile bool isPlaying = false;

        //=========================================================================================
        /// <summary>
        /// Reload Domain 対策
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
        }

#if UNITY_EDITOR
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
        static void OnStarted(object obj)
        {
        }

        /// <summary>
        /// スクリプトコンパイル後
        /// </summary>
        //[DidReloadScripts(0)]
        //static void ReloadScripts()
        //{
        //    //Initialize();
        //}

        /// <summary>
        /// ゲームプレイの実行が停止したとき（エディタ環境のみ）
        /// </summary>
        static void EnterdEditMode()
        {
        }

        /// <summary>
        /// エディタでの定期更新
        /// </summary>
        static void EditoruUpdate()
        {
        }
#endif


        /// <summary>
        /// マネージャの破棄
        /// </summary>
        static void Dispose()
        {
        }

        public static bool IsPlaying()
        {
            return default;
        }

        //=========================================================================================
        /// <summary>
        /// カスタム更新ループ登録
        /// すでに登録されている場合は何もしない
        /// Custom update loop registration.
        /// Do nothing if already registered.
        /// </summary>
        public static void InitCustomGameLoop()
        {
        }

        static readonly ProfilerMarker EarlyUpdateMarker = new ProfilerMarker("MagicaPhysicsManager.EarlyUpdate");
        static readonly ProfilerMarker FixedUpdateMarker = new ProfilerMarker("MagicaPhysicsManager.FixedUpdate");
        static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("MagicaPhysicsManager.Update");
        static readonly ProfilerMarker PostDelayedUpdateMarker = new ProfilerMarker("MagicaPhysicsManager.PostDelayedLateUpdate");
        static readonly ProfilerMarker PostLateUpdateMarker = new ProfilerMarker("MagicaPhysicsManager.PostLateUpdate");

        static void SetCustomGameLoop(ref PlayerLoopSystem playerLoop)
        {
        }

        /// <summary>
        /// methodをPlayerLoopの(categoryName:systemName)の次に追加する
        /// </summary>
        /// <param name="method"></param>
        /// <param name="playerLoop"></param>
        /// <param name="categoryName"></param>
        /// <param name="systemName"></param>
        static void AddPlayerLoop(PlayerLoopSystem method, ref PlayerLoopSystem playerLoop, string categoryName, string systemName, bool last = false, bool before = false, bool first = false)
        {
        }

        /// <summary>
        /// MagicaClothのカスタムループが登録されているかチェックする
        /// </summary>
        /// <param name="playerLoop"></param>
        /// <returns></returns>
        static bool CheckRegist(ref PlayerLoopSystem playerLoop)
        {
            return default;
        }
    }
}
