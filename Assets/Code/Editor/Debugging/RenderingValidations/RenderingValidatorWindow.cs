using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.MedusaRenderer;
using Awaken.TG.Editor.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Entities;
using Unity.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.VFX;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    public class RenderingValidatorWindow : OdinEditorWindow {
        const string MainGroup = "MainGroup";
        const string StaticAnalysisTabName = "Static Analysis";
        const string DynamicAnalysisTabName = "Dynamic Analysis";
        const string MedusaTab = MainGroup + "/" + DynamicAnalysisTabName + "/Medusa";
        const string DrakeTab = MainGroup + "/" + DynamicAnalysisTabName + "/Medusa";
        
        // === Filters
        [TabGroup(MainGroup, StaticAnalysisTabName)]
        [ShowInInspector, OnValueChanged(nameof(ContextTypeFilterChanged))]
        ContextType _contextType;

        [TabGroup(MainGroup, StaticAnalysisTabName)]
        [ShowInInspector, OnValueChanged(nameof(LofTypeFilterChanged))]
        RenderingErrorLogType _minimumLogType = RenderingErrorLogType.Log;

        [TabGroup(MainGroup, StaticAnalysisTabName)]
        [ShowInInspector, OnValueChanged(nameof(MessageFilterChanged)), Delayed]
        string _messageFilter = string.Empty;

        [TabGroup(MainGroup, StaticAnalysisTabName)]
        [ShowInInspector, TableList(IsReadOnly = true, NumberOfItemsPerPage = 20, ShowPaging = true, AlwaysExpanded = true)]
        List<RenderingError> _errorsBuffer = new List<RenderingError>();

        [TabGroup(MainGroup, DynamicAnalysisTabName)]
        [Tooltip("How much time any medusa object with some material and mesh should be visible on screen to make sense to be a static medusa object which is never unloaded from memory")]
        [OnValueChanged(nameof(ProvideHintsFromRecordedData))]
        [ShowInInspector, Range(0f, 100f)]
        int _medusaMinimalVisibilityTimePercent = 50;
        
        [TabGroup(MainGroup, DynamicAnalysisTabName)]
        [ShowInInspector, ReadOnly] int _recordedFramesCount;
        
        [TabGroup(MedusaTab, "Medusa")]
        [LabelText("Medusa objects which probably need to be in Drake system")]
        [ListDrawerSettings(HideRemoveButton = true, DraggableItems = false, IsReadOnly = true)]
        [ShowInInspector, ShowIf(nameof(ShowRecordedData))]
        RendererWithVisibilityStats[] _medusaRendersMaterialsAndMeshes = Array.Empty<RendererWithVisibilityStats>();

        [TabGroup(DrakeTab, "Drake")]
        [LabelText("Drake objects which probably need to be in Medusa system")]
        [ListDrawerSettings(HideRemoveButton = true, DraggableItems = false, IsReadOnly = true)]
        [ShowInInspector, ShowIf(nameof(ShowRecordedData))]
        RendererWithVisibilityStats[] _entitiesRendersMaterialsAndMeshes = Array.Empty<RendererWithVisibilityStats>();

        string[] _messageFilterParts = Array.Empty<string>();
        List<Type> _contextTypesFilter = new();
        List<RenderingError> _allErrors = new List<RenderingError>();
        EntitiesGraphicsSystem _entitiesGraphicsSystem;
        MedusaBrgRenderer.EditorAccess _medusaBrgRenderer;
        RendererWithVisibilityStats[] _medusaRendersMaterialsAndMeshesFullList = Array.Empty<RendererWithVisibilityStats>();
        RendererWithVisibilityStats[] _entitiesRendersMaterialsAndMeshesFullList = Array.Empty<RendererWithVisibilityStats>();
        SerializedDictionary<MaterialMeshNameWithoutLOD, MaterialMeshVisibilityStats> _medusaMaterialMeshNameWithoutLODToVisibilityStatsMap = new();
        SerializedDictionary<MaterialMeshNameWithoutLOD, MaterialMeshVisibilityStats> _entitiesMaterialMeshNameWithoutLODToVisibilityStatsMap = new();
        SerializedDictionary<MaterialMeshNameWithoutLOD, LODLevelsVisibilityStats> _medusaMaterialMeshNameWithoutLODToLODVisibilityStats = new();
        SerializedDictionary<MaterialMeshNameWithoutLOD, LODLevelsVisibilityStats> _entitiesMaterialMeshNameWithoutLODToLODVisibilityStats = new();

        HashSet<MaterialMeshNameWithoutLOD> _processedInCurrentFrameLODMaterialMeshesCache = new();
        InitiallyLoadedScenesLoader _initiallyLoadedScenesLoader;
        Comparer<RendererWithVisibilityStats> _ascendingComparer;
        Comparer<RendererWithVisibilityStats> _descendingComparer;
        bool _isRecording;
        bool ShowRecordedData => !_isRecording && IsAnyRecordedData;
        bool IsAnyRecordedData => _recordedFramesCount > 0;
        bool CanClearRecordedData => IsAnyRecordedData && Application.isPlaying;
        bool CanStartRecording => !_isRecording && Application.isPlaying;
        bool CanStopRecording => _isRecording && Application.isPlaying;
        bool IsNotInPlaymode => Application.isPlaying == false;

        [MenuItem("TG/Debug/Rendering validator")]
        public static void ShowWindow() {
            var window = GetWindow<RenderingValidatorWindow>();
            window.Show();
        }

        protected override void OnEnable() {
            base.OnEnable();
            EditorApplication.playModeStateChanged += ApplicationStateChangedClearOnPlay;
        }

        [TabGroup(MainGroup, StaticAnalysisTabName)]
        [Button]
        void Refresh(bool forceCacheRefresh = true) {
            var contexts = RenderingContextsCollector.CollectContexts(!forceCacheRefresh);
            RenderingValidatorManager.Check(contexts, _allErrors);
            for (int i = 0; i < _allErrors.Count; i++) {
                _allErrors[i].Bake();
            }

            _allErrors.Sort(static (a, b) => b.Score.CompareTo(a.Score));
            FillFilteredErrors();
        }

        [TabGroup(MainGroup, DynamicAnalysisTabName)]
        [InfoBox("Enter playmode to start recording", visibleIfMemberName: nameof(IsNotInPlaymode), infoMessageType: InfoMessageType.Warning)]
        [Button, EnableIf(nameof(CanStartRecording)), HideIf(nameof(_isRecording))]
        void StartRecordingVisibleRenderers() {
            if (_isRecording) {
                return;
            }

            if (Application.isPlaying == false) {
                return;
            }
            ClearRecordedData();
            _isRecording = true;
            Log.Debug?.Info("Started recording Materials and Meshes Visibility");
            _entitiesGraphicsSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            bool isAnyBrg = false;
            if (_entitiesGraphicsSystem != null) {
                isAnyBrg = true;
                _entitiesGraphicsSystem.CollectBatchCullingOutputDebugData = true;
            }

            var medusaRendererManager = GameObject.FindAnyObjectByType<MedusaRendererManager>();
            if (medusaRendererManager != null) {
                var medusaRendererManagerEditorAccess = new MedusaRendererManager.EditorAccess(medusaRendererManager);
                _medusaBrgRenderer = medusaRendererManagerEditorAccess.BrgRenderer;
                // Initialize only medusa with zeros because for medusa it is important to know which objects were not rendered at all
                RenderingValidatorStaticAnalysis.InitializeMedusaRenderersWithZeroCount(medusaRendererManagerEditorAccess.Renderers, 
                    _medusaMaterialMeshNameWithoutLODToVisibilityStatsMap, _processedInCurrentFrameLODMaterialMeshesCache);
                if (_medusaBrgRenderer.IsNotNull) {
                    isAnyBrg = true;
                    _medusaBrgRenderer.CollectBatchCullingOutputDebugData = true;
                }
            }

            if (isAnyBrg) {
                PlayerLoopUtils.RegisterToPlayerLoopEnd<RenderingValidatorWindow, PostLateUpdate>(RecordBatchCullingOutputDebugData);
            }

            EditorApplication.playModeStateChanged += ApplicationStateChangedAutoStop;
        }

        [TabGroup(MainGroup, DynamicAnalysisTabName)]
        [Button, ShowIf(nameof(CanStopRecording))]
        void StopRecordingAndExitPlaymode() {
            if (!_isRecording) {
                return;
            }

            _isRecording = false;
            if (_entitiesGraphicsSystem != null) {
                _entitiesGraphicsSystem.CollectBatchCullingOutputDebugData = false;
            }

            if (_medusaBrgRenderer.IsNotNull) {
                _medusaBrgRenderer.CollectBatchCullingOutputDebugData = false;
            }

            PlayerLoopUtils.RemoveFromPlayerLoop<RenderingValidatorWindow, PostLateUpdate>();

            if (EditorApplication.isPlaying) {
                _initiallyLoadedScenesLoader = new InitiallyLoadedScenesLoader();
                _initiallyLoadedScenesLoader.SaveCurrentScenesAsInitiallyLoaded();
                EditorApplication.playModeStateChanged -= ApplicationStateChangedAutoStop;
                EditorApplication.playModeStateChanged += ApplicationStateChangedAutoStop;
                EditorApplication.isPlaying = false;
            }
        }

        [TabGroup(MainGroup, DynamicAnalysisTabName)]
        [Button, ShowIf(nameof(CanClearRecordedData))]
        void ClearRecordedData() {
            _recordedFramesCount = 0;
            _medusaRendersMaterialsAndMeshes = Array.Empty<RendererWithVisibilityStats>();
            _medusaRendersMaterialsAndMeshesFullList = Array.Empty<RendererWithVisibilityStats>();
            _entitiesRendersMaterialsAndMeshes = Array.Empty<RendererWithVisibilityStats>();
            _entitiesRendersMaterialsAndMeshesFullList = Array.Empty<RendererWithVisibilityStats>();
            _medusaMaterialMeshNameWithoutLODToVisibilityStatsMap.Clear();
            _entitiesMaterialMeshNameWithoutLODToVisibilityStatsMap.Clear();
            _medusaMaterialMeshNameWithoutLODToLODVisibilityStats.Clear();
            _entitiesMaterialMeshNameWithoutLODToLODVisibilityStats.Clear();
        }
        
        void ProvideHintsFromRecordedData() {
            if (_recordedFramesCount == 0) {
                Log.Debug?.Info("No recorded data");
                return;
            }

            if (_ascendingComparer == null) {
                _ascendingComparer = Comparer<RendererWithVisibilityStats>.Create((timePercentX, timePercentY) =>
                    timePercentX.visibilityPercentValue.CompareTo(timePercentY.visibilityPercentValue));
                RenderingValidatorStaticAnalysis.GetMedusaData(_medusaMaterialMeshNameWithoutLODToVisibilityStatsMap,
                    out var materialMeshNameWithoutLODToRenderersMap);
                _medusaRendersMaterialsAndMeshesFullList = RenderingValidatorStaticAnalysis.GetRenderersWithVisibilityStats(
                    materialMeshNameWithoutLODToRenderersMap, _medusaMaterialMeshNameWithoutLODToVisibilityStatsMap, 
                    _medusaMaterialMeshNameWithoutLODToLODVisibilityStats, _recordedFramesCount);
                Array.Sort(_medusaRendersMaterialsAndMeshesFullList, _ascendingComparer);
            }

            var medusaObjectMinimalVisibilityTimePercent01 = _medusaMinimalVisibilityTimePercent * 0.01f;
            var medusaRenderersCutIndex = _medusaRendersMaterialsAndMeshesFullList.GetIndexOfGreaterOrEqualElementInSortedAsc(
                new RendererWithVisibilityStats(default, default, medusaObjectMinimalVisibilityTimePercent01, false), _ascendingComparer);
            if (_medusaRendersMaterialsAndMeshesFullList.Length == 0) {
                _medusaRendersMaterialsAndMeshes = Array.Empty<RendererWithVisibilityStats>();
            } else if (medusaRenderersCutIndex == 0 && _medusaRendersMaterialsAndMeshesFullList[medusaRenderersCutIndex].visibilityPercentValue <= medusaObjectMinimalVisibilityTimePercent01) {
                _medusaRendersMaterialsAndMeshes = _medusaRendersMaterialsAndMeshesFullList.GetSubArray(0, _medusaRendersMaterialsAndMeshesFullList.Length);
            } else {
                _medusaRendersMaterialsAndMeshes = _medusaRendersMaterialsAndMeshesFullList.GetSubArray(0, medusaRenderersCutIndex);
            }

            if (_descendingComparer == null) {
                _descendingComparer = Comparer<RendererWithVisibilityStats>.Create((timePercentX, timePercentY) =>
                    timePercentY.visibilityPercentValue.CompareTo(timePercentX.visibilityPercentValue));
                RenderingValidatorStaticAnalysis.GetDrakeData(_entitiesMaterialMeshNameWithoutLODToVisibilityStatsMap,
                    out var materialMeshNameWithoutLODToRenderersMap);
                _entitiesRendersMaterialsAndMeshesFullList = RenderingValidatorStaticAnalysis.GetRenderersWithVisibilityStats(
                    materialMeshNameWithoutLODToRenderersMap, _entitiesMaterialMeshNameWithoutLODToVisibilityStatsMap, 
                    _entitiesMaterialMeshNameWithoutLODToLODVisibilityStats,  _recordedFramesCount);
                Array.Sort(_entitiesRendersMaterialsAndMeshesFullList, _descendingComparer);
            }

            var drakeRenderersCutIndex = _entitiesRendersMaterialsAndMeshesFullList.GetIndexOfSmallerOrEqualElementInSortedDesc(
                new RendererWithVisibilityStats(default, default, medusaObjectMinimalVisibilityTimePercent01, false), _descendingComparer);
            if (_entitiesRendersMaterialsAndMeshesFullList.Length == 0) {
                _entitiesRendersMaterialsAndMeshes = Array.Empty<RendererWithVisibilityStats>();
            } else if (drakeRenderersCutIndex == 0 && _entitiesRendersMaterialsAndMeshesFullList[drakeRenderersCutIndex].visibilityPercentValue >= medusaObjectMinimalVisibilityTimePercent01) {
                _entitiesRendersMaterialsAndMeshes = _entitiesRendersMaterialsAndMeshesFullList.GetSubArray(0, _entitiesRendersMaterialsAndMeshesFullList.Length);
            } else {
                _entitiesRendersMaterialsAndMeshes = _entitiesRendersMaterialsAndMeshesFullList.GetSubArray(0, drakeRenderersCutIndex);
            }

            _isRecording = false;
        }

        void ApplicationStateChangedAutoStop(PlayModeStateChange stateChange) {
            if (_recordedFramesCount == 0) {
                EditorApplication.playModeStateChanged -= ApplicationStateChangedAutoStop;
                return;
            }

            if (_isRecording && stateChange == PlayModeStateChange.ExitingPlayMode) {
                EditorApplication.playModeStateChanged -= ApplicationStateChangedAutoStop;
                StopRecordingAndExitPlaymode();
                return;
            }

            if (_isRecording == false && stateChange == PlayModeStateChange.EnteredEditMode) {
                EditorApplication.playModeStateChanged -= ApplicationStateChangedAutoStop;
                LoadCorrectScenesAndProvideHints();
            }
        }

        void ApplicationStateChangedClearOnPlay(PlayModeStateChange stateChange) {
            if (stateChange == PlayModeStateChange.EnteredPlayMode) {
                ClearRecordedData();
            }
        }

        void LoadCorrectScenesAndProvideHints() {
            _initiallyLoadedScenesLoader.RestoreInitiallyLoadedScenes("Assets/Scenes/ApplicationScene.unity");
            ProvideHintsFromRecordedData();
        }

        void RecordBatchCullingOutputDebugData() {
            _recordedFramesCount++;
            BatchCullingOutputDebugData medusaDebugData = _medusaBrgRenderer.IsNotNull ? _medusaBrgRenderer.BatchCullingOutputDebugData : default;
            RenderingValidatorStaticAnalysis.IncreaseVisibleInstancesCount(medusaDebugData.materialMeshRefToVisibleCountMap, _medusaMaterialMeshNameWithoutLODToVisibilityStatsMap,
                _medusaMaterialMeshNameWithoutLODToLODVisibilityStats, _processedInCurrentFrameLODMaterialMeshesCache);
            
            BatchCullingOutputDebugData entitiesDebugData = _entitiesGraphicsSystem?.BatchCullingOutputDebugData ?? default;
            RenderingValidatorStaticAnalysis.IncreaseVisibleInstancesCount(entitiesDebugData.materialMeshRefToVisibleCountMap, _entitiesMaterialMeshNameWithoutLODToVisibilityStatsMap,
                _entitiesMaterialMeshNameWithoutLODToLODVisibilityStats, _processedInCurrentFrameLODMaterialMeshesCache);
        }

        

        void FillFilteredErrors() {
            _errorsBuffer.Clear();
            for (int i = 0; i < _allErrors.Count; i++) {
                var error = _allErrors[i];
                if (FulfillsFilters(error)) {
                    _errorsBuffer.Add(error);
                }
            }
        }

        bool FulfillsFilters(in RenderingError error) {
            if (!FulfillsContextTypeFilters(error)) {
                return false;
            }

            if (!FulfillsLogTypeFilters(error)) {
                return false;
            }

            if (!(_messageFilterParts.IsNullOrEmpty() || FulfillsMessageFilters(error))) {
                return false;
            }

            return true;
        }

        bool FulfillsContextTypeFilters(in RenderingError error) {
            if (_contextTypesFilter.Count == 0) {
                return true;
            }

            return _contextTypesFilter.Contains(error.ContextObject.GetType());
        }

        bool FulfillsLogTypeFilters(in RenderingError error) {
            var minimumLogTypeStrength = _minimumLogType.Value();
            var highestLogTypeStrength = error.HighestLogType.Value();
            return highestLogTypeStrength >= minimumLogTypeStrength;
        }

        bool FulfillsMessageFilters(in RenderingError error) {
            return error.Messages.Any(m => m.Message.ContainsAny(_messageFilterParts));
        }

        void ContextTypeFilterChanged() {
            _contextTypesFilter.Clear();
            if (_contextType.HasFlag(ContextType.MeshRenderer)) {
                _contextTypesFilter.Add(typeof(MeshRenderer));
            }

            if (_contextType.HasFlag(ContextType.SkinnedMeshRenderer)) {
                _contextTypesFilter.Add(typeof(SkinnedMeshRenderer));
            }

            if (_contextType.HasFlag(ContextType.MeshCollider)) {
                _contextTypesFilter.Add(typeof(MeshCollider));
            }

            if (_contextType.HasFlag(ContextType.MeshFilter)) {
                _contextTypesFilter.Add(typeof(MeshFilter));
            }

            if (_contextType.HasFlag(ContextType.VisualEffect)) {
                _contextTypesFilter.Add(typeof(VisualEffect));
            }

            if (_contextType.HasFlag(ContextType.Mesh)) {
                _contextTypesFilter.Add(typeof(Mesh));
            }

            if (_contextType.HasFlag(ContextType.Material)) {
                _contextTypesFilter.Add(typeof(Material));
            }

            if (_contextType.HasFlag(ContextType.DrakeMeshRenderer)) {
                _contextTypesFilter.Add(typeof(DrakeMeshRendererHolder));
            }

            if (_contextType.HasFlag(ContextType.DrakeLodGroup)) {
                _contextTypesFilter.Add(typeof(DrakeLodGroup));
            }

            FillFilteredErrors();
        }

        void LofTypeFilterChanged() {
            FillFilteredErrors();
        }

        void MessageFilterChanged() {
            _messageFilterParts = !string.IsNullOrWhiteSpace(_messageFilter) ? 
                _messageFilter.Split(' ') : 
                Array.Empty<string>();
            FillFilteredErrors();
        }

        protected override void OnDisable() {
            base.OnDisable();
            EditorApplication.playModeStateChanged -= ApplicationStateChangedClearOnPlay;
        }

        protected override void OnDestroy() {
            base.OnDestroy();

            ClearRecordedData();
        }

        [Flags]
        enum ContextType {
            MeshRenderer = 1 << 0,
            SkinnedMeshRenderer = 1 << 1,
            MeshCollider = 1 << 2,
            MeshFilter = 1 << 3,
            VisualEffect = 1 << 4,
            Mesh = 1 << 5,
            Material = 1 << 6,
            DrakeMeshRenderer = 1 << 7,
            DrakeLodGroup = 1 << 8,
        }
        
    }
}