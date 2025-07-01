// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 各クロスコンポーネントの更新処理
    /// </summary>
    public class ClothManager : IManager, IValid
    {
        // すべて
        internal HashSet<ClothProcess> clothSet = new HashSet<ClothProcess>();

        // BoneCloth,BoneSpring
        internal HashSet<ClothProcess> boneClothSet = new HashSet<ClothProcess>();

        // MeshCloth
        internal HashSet<ClothProcess> meshClothSet = new HashSet<ClothProcess>();

        //=========================================================================================
        Dictionary<int, bool> animatorVisibleDict = new Dictionary<int, bool>(30);
        Dictionary<int, bool> rendererVisibleDict = new Dictionary<int, bool>(100);
        
        static readonly ProfilerMarker OnPreSimulationMarker = new ProfilerMarker("ClothManager.OnPreSimulation");
        static readonly ProfilerMarker OnAlwaysTeamUpdateMarker = new ProfilerMarker("ClothManager.OnAlwaysTeamUpdate");
        static readonly ProfilerMarker OnSimulationStepMarker = new ProfilerMarker("ClothManager.OnSimulationStep");
        static readonly ProfilerMarker OnMeshWorkMarker = new ProfilerMarker("ClothManager.OnMeshWork");
        static readonly ProfilerMarker OnWindUpdateMarker = new ProfilerMarker("ClothManager.OnWindUpdate");
        static readonly ProfilerMarker OnPrepareDataMarker = new ProfilerMarker("ClothManager.OnPrepareData");
        static readonly ProfilerMarker OnApplySimulationMarker = new ProfilerMarker("ClothManager.OnApplySimulation");
        //=========================================================================================
        /// <summary>
        /// マスタージョブハンドル
        /// </summary>
        JobHandle masterJob = default;
        public JobHandle BoneJobHandle { get; private set; }
        public static bool MasterJobExecuting { get; private set; }

        bool isValid = false;

        //=========================================================================================
        public void Dispose()
        {
        }

        public void EnterdEditMode()
        {
        }

        public void Initialize()
        {
        }

        public bool IsValid()
        {
            return default;
        }

        //=========================================================================================
        void ClearMasterJob()
        {
        }

        void CompleteMasterJob()
        {
        }

        //=========================================================================================
        internal int AddCloth(ClothProcess cprocess, in ClothParameters clothParams)
        {
            return default;
        }

        internal void RemoveCloth(ClothProcess cprocess)
        {
        }

        //=========================================================================================
        /// <summary>
        /// フレーム開始時に実行される更新処理
        /// </summary>
        void OnEarlyClothUpdate()
        {
        }

        //=========================================================================================
        static readonly ProfilerMarker startClothUpdateMainProfiler = new ProfilerMarker("StartClothUpdate.Main");
        static readonly ProfilerMarker startClothUpdateScheduleProfiler = new ProfilerMarker("StartClothUpdate.Schedule");

        /// <summary>
        /// クロスコンポーネントの更新
        /// </summary>
        public void StartClothUpdate()
        {
        }

        void CompleteClothUpdate()
        {
        }

        //=========================================================================================
        internal void ClearVisibleDict()
        {
        }

        internal bool CheckVisible(Animator ani, List<Renderer> renderers)
        {
            return default;
        }

        bool CheckRendererVisible(List<Renderer> renderers)
        {
            return default;
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
        }
    }
}
