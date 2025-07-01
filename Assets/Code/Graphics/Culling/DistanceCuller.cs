using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.TG.Graphics.DayNightSystem;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Awaken.Utility.Profiling;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.IL2CPP.CompilerServices;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Graphics.Culling {
    [DefaultExecutionOrder(-100), Il2CppEagerStaticClassConstruction]
    public partial class DistanceCuller : MonoBehaviour {
        const float BiasRemapMin = 5;
        const float BiasRemapMax = 1;
        
        static readonly List<Renderer> Renderers = new(64);
        static readonly (float maxVolume, float distance)[] Ranges = {
            (1.2f, 20f), // Of course flowers are between 0.3 - 1.1x :)
            (5, 50f),
            (20, 90f),
            (50, 150f),
            (200, 200f),
            (600, 300f),
            (2000, 800f),
            (5000, 1200f),
            (float.MaxValue, 5001f), // Far plane + 1
        };

        // === Setup/SerializeField
        [SerializeField]
        ToRegisterRenderer[] sceneStaticMeshes = Array.Empty<ToRegisterRenderer>();
        [SerializeField] int maxAdditionalSizeSize = 1_000;
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)] DistanceCullerGroup[] renderersGroups = Array.Empty<DistanceCullerGroup>();

        // === Internal state
        // -- Data
        NativeArray<float> _cullDistancesSq;
        // -- Renderers
        DistanceCullerRendererData[] _renderers = Array.Empty<DistanceCullerRendererData>();
        DistanceCullerRenderer[] _distanceCullerRenderers = Array.Empty<DistanceCullerRenderer>();
        DistanceCullerImpl _renderersCuller;
        // -- Groups
        DistanceCullerImpl _renderersGroupsCuller;
        // -- Buffers
        readonly List<DistanceCullerRenderer> _toRemoveRenderers = new(128);
        // We need this only to have easier lookup of elements to skip in 'apply changes'
        readonly List<int> _toRemoveIndices = new(128);
        // -- Others
        JobHandle _handle;

        // -- Profiling/Debug
        ProfilerMarker _updateAllMarker;
        ProfilerMarker<int> _updateRenderersMarker;
        ProfilerMarker<int> _updateGroupsMarker;
        ProfilerMarker<int> _updateRemovedMarker;

        bool _showGUI;
        bool _initialized;
        ISubscene _subscene;
        ISubscenesOwner _subscenesOwner;
        int _framesLeftToRecalculateBounds = 2;
        static float Bias => (World.Any<DistanceCullingSetting>()?.Value ?? 1).Remap01(BiasRemapMin, BiasRemapMax, true);

        public bool ShowGUI {
            get => _showGUI;
            set {
                _showGUI = value;
                UpdateRecorders();
            }
        }

        // === Lifetime
        void Awake() {
            var scene = gameObject.scene;
            _subscene = GameObjects.FindComponentByTypeInScene<ISubscene>(scene, false);
            _subscenesOwner = GameObjects.FindComponentByTypeInScene<ISubscenesOwner>(scene, true);
        }
        
        void Start() {
            // Subscene owner will initialize it itself at right time.
            // Subscenes will be initialized by subscenes owner
            if (_subscenesOwner != null || _subscene != null) {
                return;
            }
            Initialize();
        }

        public void Initialize() {
            if (_initialized) {
                return;
            }
            _initialized = true;
            var myScene = gameObject.scene;
            
            if (_subscene != null && _subscene.OwnerScene != myScene) {
                var ownerScene = _subscene.OwnerScene;
                
                var distanceCuller = GameObjects.FindComponentByTypeInScene<DistanceCuller>(ownerScene, false);
                if (distanceCuller == null) {
                    Log.Important?.Error($"Subscene distance culler without parent {ownerScene.name}", gameObject);
                    Destroy(this);
                    return;
                }
                distanceCuller.UnionWith(this);
                World.Services.Get<DistanceCullersService>().Register(distanceCuller, myScene);

                Destroy(this);
                return;
            }
            
            var sceneName = myScene.name;
            
            _updateAllMarker = new(ProfilerCategory.Scripts, $"DistanceCuller.Update - {sceneName}");
            _updateRenderersMarker = new(ProfilerCategory.Scripts, $"DistanceCuller.UpdateRenderers - {sceneName}", "Count");
            _updateGroupsMarker = new(ProfilerCategory.Scripts, $"DistanceCuller.UpdateGroups - {sceneName}", "Count");
            _updateRemovedMarker = new(ProfilerCategory.Scripts, $"DistanceCuller.DeleteRemoved - {sceneName}", "Count");

            var maxRenderers = sceneStaticMeshes.Length + maxAdditionalSizeSize;
            _renderersCuller.Create(maxRenderers);
            _renderers = new DistanceCullerRendererData[maxRenderers];
            _distanceCullerRenderers = new DistanceCullerRenderer[maxRenderers];

            _renderersGroupsCuller.Create(renderersGroups.Length);

            _cullDistancesSq = new(Ranges.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < Ranges.Length; i++) {
                _cullDistancesSq[i] = Ranges[i].distance * Ranges[i].distance / Bias;
            }

            for (int i = 0; i < sceneStaticMeshes.Length; i++) {
                var toRegisterRenderer = sceneStaticMeshes[i];
                var index = RegisterRenderer(toRegisterRenderer);
                toRegisterRenderer.indexInRuntimeArrays = index;
                sceneStaticMeshes[i] = toRegisterRenderer;
            }

            for (int i = 0; i < renderersGroups.Length; i++) {
                RegisterGroup(renderersGroups[i]);
            }
            World.Services.Get<DistanceCullersService>().Register(this, myScene);
        }

        public void RecalculateBoundsIfZeroSize() {
            int count = sceneStaticMeshes.Length;
            for (int i = 0; i < count; i++) {
                var toRegisterRenderer = sceneStaticMeshes[i];
                if (math.all(toRegisterRenderer.extents == float3.zero)) {
                    UpdateRendererBoundsData(toRegisterRenderer.renderer, toRegisterRenderer.indexInRuntimeArrays);
                }
            }
        }

        void OnEnable() {
            var states = _renderersCuller.States;
            for (var i = 0; i < _renderersCuller.NextElementIndex; i++) {
                _renderers[i].SetEnabled(states[i].IsVisible());
            }
            states = _renderersGroupsCuller.States;
            for (var i = 0; i < _renderersGroupsCuller.NextElementIndex; i++) {
                renderersGroups[i].SetEnabled(states[i].IsVisible(), true);
            }
        }

        void OnDisable() {
            _handle.Complete();

            // Can be called at scene unload so it could be in a bit strange state
            // So better to add some null checks than cause exception
            for (var i = 0; i < _renderersCuller.NextElementIndex; i++) {
                if (_renderers[i].Renderer) {
                    _renderers[i].SetEnabled(true);
                }
            }

            for (var i = 0; i < _renderersGroupsCuller.NextElementIndex; i++) {
                if (renderersGroups[i]) {
                    renderersGroups[i].SetEnabled(true, true, true);
                }
            }
        }

        void OnDestroy() {
            if (_subscene == null || _subscene.OwnerScene == gameObject.scene) {
                World.Services.Get<DistanceCullersService>().Unregister(this);
            }
            _handle.Complete();
            
            _cullDistancesSq.Dispose();
            _renderersCuller.Dispose();
            _renderersCuller = default;

            _renderersGroupsCuller.Dispose();
            _renderersGroupsCuller = default;

            _renderers = Array.Empty<DistanceCullerRendererData>();
            _distanceCullerRenderers = Array.Empty<DistanceCullerRenderer>();
            renderersGroups = Array.Empty<DistanceCullerGroup>();
        }

        public void UnionWith(DistanceCuller additional) {
            if (additional.sceneStaticMeshes.Length > 0) {
                Array.Resize(ref sceneStaticMeshes, sceneStaticMeshes.Length + additional.sceneStaticMeshes.Length);
                Array.Copy(additional.sceneStaticMeshes, 0, sceneStaticMeshes, sceneStaticMeshes.Length - additional.sceneStaticMeshes.Length, additional.sceneStaticMeshes.Length);
                Array.Clear(additional.sceneStaticMeshes, 0, additional.sceneStaticMeshes.Length);
                additional.sceneStaticMeshes = Array.Empty<ToRegisterRenderer>();
            }
            if (additional.renderersGroups.Length > 0) {
                Array.Resize(ref renderersGroups, renderersGroups.Length + additional.renderersGroups.Length);
                Array.Copy(additional.renderersGroups, 0, renderersGroups, renderersGroups.Length - additional.renderersGroups.Length, additional.renderersGroups.Length);
                Array.Clear(additional.renderersGroups, 0, additional.renderersGroups.Length);
                additional.renderersGroups = Array.Empty<DistanceCullerGroup>();
            }
        }

        // === Update
        void LateUpdate() {
            if (!_initialized) {
                return;
            }
            _updateAllMarker.Begin();
            UpdateState();
            RunJobs();
            _updateAllMarker.End();
        }

        void UpdateState() {
            _handle.Complete();
            _framesLeftToRecalculateBounds--;
            if (_framesLeftToRecalculateBounds == 0) {
                RecalculateBoundsIfZeroSize();
            }
            UpdateRenderers();
            UpdateGroups();
            DeleteRemoved();
        }

        void UpdateRenderers() {
            var count = _renderersCuller.ChangedCount;
            _updateRenderersMarker.Begin(count);
            var changedIndices = _renderersCuller.ChangedIndices;
            var states = _renderersCuller.States;
            for (var i = 0; i < count; i++) {
                var index = changedIndices[i];
                if (_toRemoveIndices.Contains(index)) {
                    continue;
                }
                _renderers[index].SetEnabled(states[index].IsVisible());
#if DEBUG
                _distanceCullerRenderers[index].DebugChangePerformed(states[index].IsVisible());
#endif
            }
            _updateRenderersMarker.End();
        }

        void UpdateGroups() {
            var count = _renderersGroupsCuller.ChangedCount;
            _updateGroupsMarker.Begin(count);
            var changedIndices = _renderersGroupsCuller.ChangedIndices;
            var states = _renderersGroupsCuller.States;
            for (var i = 0; i < count; i++) {
                var index = changedIndices[i];
                renderersGroups[index].SetEnabled(states[index].IsVisible(), false);
            }
            _updateGroupsMarker.End();
        }

        void DeleteRemoved() {
            if (_toRemoveRenderers.Count <= 0) {
                return;
            }
            _updateRemovedMarker.Begin(_toRemoveRenderers.Count);
            // Here _toRemoveIndices becomes invalid and only _toRemoveRenderers have up-to-date data
            for (var i = 0; i < _toRemoveRenderers.Count; i++) {
                var lastElement = _renderersCuller.NextElementIndex - 1;
                var toRemove = _toRemoveRenderers[i];
                var toRemoveIndex = toRemove.id;
                if (lastElement == toRemoveIndex) {
                    _distanceCullerRenderers[toRemoveIndex] = null;
                    _renderers[toRemoveIndex] = default;
                    _renderersCuller.RemoveLast();
                    continue;
                }

                RemoveSwapBack(_renderers, toRemoveIndex, lastElement);
                RemoveSwapBack(_distanceCullerRenderers, toRemoveIndex, lastElement);
                // Here _distanceCullerRenderers[toRemoveIndex] is element which was _distanceCullerRenderers[lastElement]
                _distanceCullerRenderers[toRemoveIndex].id = toRemoveIndex;
                _renderersCuller.RemoveSwapBack(toRemoveIndex);
            }
            _toRemoveRenderers.Clear();
            _toRemoveIndices.Clear();
            _updateRemovedMarker.End();
        }

        void RunJobs() {
            var mainCamera = World.Any<GameCamera>()?.MainCamera;
            if (!mainCamera) {
                return;
            }

            var cameraTransform = mainCamera.transform;
            var cameraPosition = cameraTransform.position;
            var cameraForward = cameraTransform.forward;

            var renderersHandle = _renderersCuller.Execute(cameraPosition, cameraForward, _cullDistancesSq);
            var groupsHandle = _renderersGroupsCuller.Execute(cameraPosition, cameraForward, _cullDistancesSq);
            _handle = JobHandle.CombineDependencies(renderersHandle, groupsHandle);
        }

        // === (Un)Register
        public void RegisterLocationPrefab(GameObject visualGameObject) {
            Renderers.Clear();
            visualGameObject.GetComponentsInChildren(Renderers);
            if (Renderers.IsNullOrEmpty()) {
                return;
            }
            foreach (var locationRenderer in Renderers) {
                if (locationRenderer is MeshRenderer && locationRenderer.GetComponent<MeshFilter>().sharedMesh == null) {
                    continue;
                }
                var parentGroup = locationRenderer.GetComponentInParent<DistanceCullerGroup>();
                RegisterRenderer(new(locationRenderer, parentGroup));
            }
        }

        public void Unregister(DistanceCullerRenderer cullerRenderer) {
            _toRemoveRenderers.Add(cullerRenderer);
            _toRemoveIndices.Add(cullerRenderer.id);
        }

        int RegisterRenderer(ToRegisterRenderer renderer) {
            if (renderer.distanceIndex == -1) {
                return -1;
            }

            var bounds = new Bounds() {
                center = renderer.center,
                extents = renderer.extents
            };
            var index = _renderersCuller.Register(new(bounds.min, bounds.max), renderer.distanceIndex);
            _renderers[index] = DistanceCullerRendererData.Create(renderer.renderer);
            var rendererData = renderer.renderer.gameObject.AddComponent<DistanceCullerRenderer>();
            _distanceCullerRenderers[index] = rendererData;
            rendererData.id = index;
#if DEBUG
            rendererData.SetDebugData(this, renderer.distanceIndex, _cullDistancesSq[renderer.distanceIndex],
                ToRegisterRenderer.Volume(in renderer));
#endif
            return index;
        }
        void UpdateRendererBoundsData(Renderer renderer, int index) {
            var bounds = renderer.bounds;
            _renderersCuller.UpdateBoundsData(index, new BoundsCorners(bounds.min, bounds.max), GetDistanceIndex(bounds.VolumeOfAverage()));
            // Could also update sceneStaticMeshes but this data is only used once to setup the renderersCuller bounds data. 
        }

        void RegisterGroup(DistanceCullerGroup group) {
            if (!group) {
                return;
            }
            var distanceIndex = GetDistanceIndex(group.Volume);
            if (distanceIndex == -1) {
                return;
            }
            var index = _renderersGroupsCuller.Register(new(group.Min, group.Max), distanceIndex);
            group.id = index;
#if DEBUG
            group.SetDebugData(this, distanceIndex, _cullDistancesSq[distanceIndex], group.Volume);
#endif
        }

        public void BiasChanged() {
            UpdateState();
            for (int i = 0; i < Ranges.Length; i++) {
                _cullDistancesSq[i] = Ranges[i].distance * Ranges[i].distance / Bias;
            }
        }
        
        static void RemoveSwapBack<T>(T[] array, int toRemove, int lastElement) {
            array[toRemove] = array[lastElement];
            array[lastElement] = default;
        }

        static int GetDistanceIndex(float volume) {
            for (var i = 0; i < Ranges.Length; i++) {
                if (volume < Ranges[i].maxVolume) {
                    return i;
                }
            }
            return -1;
        }

        [Serializable]
        struct ToRegisterRenderer {
            public float3 extents;
            public int indexInRuntimeArrays;
            public float3 center;
            public int distanceIndex;
            public Renderer renderer;
            public ToRegisterRenderer(Renderer renderer) {
                this.renderer = renderer;
                var bounds = renderer.bounds;
                distanceIndex = GetDistanceIndex(bounds.VolumeOfAverage());

                center = bounds.center;
                extents = bounds.extents;

                indexInRuntimeArrays = -1;
            }

            public ToRegisterRenderer(Renderer renderer, DistanceCullerGroup parentGroup) {
                this.renderer = renderer;
                var bounds = renderer.bounds;
                if (parentGroup?.ManualVolume ?? false) {
                    distanceIndex = GetDistanceIndex(parentGroup.Volume);
                } else {
                    distanceIndex = GetDistanceIndex(bounds.VolumeOfAverage());
                }

                center = bounds.center;
                extents = bounds.extents;

                indexInRuntimeArrays = -1;
            }

            public static float Volume(in ToRegisterRenderer toRegisterRenderer) {
                return BoundsUtils.VolumeOfAverage(toRegisterRenderer.extents * 2);
            }
            
            public static float Volume(float3 boundsSize) {
                return BoundsUtils.VolumeOfAverage(boundsSize);
            }
        }

        // === Editor/Debug/Profiling
        // ReSharper disable once CognitiveComplexity
        [Button]
        public void EDITOR_FillFromScene(bool fromScene = true) {
#if UNITY_EDITOR
            var scene = gameObject.scene;
            var allRenderers = GameObjects.FindComponentsByTypeInScene<Renderer>(scene, false, 100);
            var validRenderers = new List<ToRegisterRenderer>();
            foreach (var rendererToCheck in allRenderers) {
                if (!IsPersistent(rendererToCheck.gameObject)) {
                    continue;
                }
                if (rendererToCheck.GetComponentInParent<LocationSpec>()) {
                    continue;
                }
                if (rendererToCheck.GetComponentInParent<DistanceCullerGroup>()) {
                    continue;
                }
                if (rendererToCheck.GetComponentInParent<WyrdNightControllerBase>()) {
                    continue;
                }
                if (!rendererToCheck.gameObject.isStatic) {
                    if (!Application.isPlaying && fromScene) {
                        Log.Important?.Warning($"{rendererToCheck} is not static", rendererToCheck);
                    }
                    continue;
                }

                if (rendererToCheck.TryGetComponent<IRenderingOptimizationSystemTarget>(out _)) {
                    continue;
                }
                
                var lodGroup = rendererToCheck.GetComponentInParent<LODGroup>();
                if (lodGroup) {
                    if (lodGroup.TryGetComponent<IRenderingOptimizationSystemTarget>(out _)) {
                        continue;
                    }
                    lodGroup.gameObject.AddComponent<DistanceCullerGroup>();
                    lodGroup.enabled = (lodGroup.GetLODs()?.Length ?? 0) > 1;
                    continue;
                }

                var toRegister = new ToRegisterRenderer(rendererToCheck);
                if (toRegister.distanceIndex == -1) {
                    if (!Application.isPlaying && fromScene) {
                        Log.Important?.Error(
                            $"{rendererToCheck} has distance index -1 [{ToRegisterRenderer.Volume(toRegister)}]",
                            rendererToCheck);
                    }
                    continue;
                }
                validRenderers.Add(toRegister);
            }
            sceneStaticMeshes = validRenderers.ToArray();

            renderersGroups = GameObjects.FindComponentsByTypeInScene<DistanceCullerGroup>(scene, false, 50)
                .Where(g => IsPersistent(g.gameObject) &&  g.EDITOR_BakeData())
                .ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        static bool IsPersistent(GameObject go) {
            return go.hideFlags == HideFlags.None;
        }

        void UpdateRecorders() {
#if ENABLE_PROFILER
            if (_showGUI) {
                ProfilerMarkerUtils.StartRecording(_updateAllMarker);
            } else {
                ProfilerMarkerUtils.StopRecording(_updateAllMarker);
            }
#endif
        }

        void OnGUI() {
            if (!ShowGUI) {
                return;
            }
            var hadChange = GUI.changed;
            GUI.changed = false;
            
            GUILayout.Label("Distance culling:");
            var allCulledMeshes = 0;
            var allCulledGroups = 0;
            for (var i = 0; i < Ranges.Length; i++) {
                var range = Ranges[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Range {i+1}:");
                var minRange = i - 1 > -1 ? Ranges[i - 1].maxVolume : 0;
                var maxRange = i + 1 < Ranges.Length ? Ranges[i + 1].maxVolume : float.MaxValue;
                range.maxVolume = GUILayout.HorizontalSlider(range.maxVolume, minRange, maxRange, GUILayout.Width(120));
                GUILayout.Label(range.maxVolume.ToString("0000.00"));
                GUILayout.Space(6);
                GUILayout.Label("-");
                GUILayout.Space(6);
                GUILayout.Label("distance: ");
                var minDist = i - 1 > -1 ? Ranges[i - 1].distance : 1;
                var maxDist = i + 1 < Ranges.Length ? Ranges[i + 1].distance : 5001f;
                range.distance = GUILayout.HorizontalSlider(range.distance, minDist, maxDist, GUILayout.Width(260));
                GUILayout.Label(range.distance.ToString("000000.0"));
                GUILayout.Space(6);
                GUILayout.Label("-");
                GUILayout.Space(6);
                var countMeshes = 0;
                var culledMeshes = 0;
                var cullDistanceIndices = _renderersCuller.CullDistanceIndices;
                var states = _renderersCuller.States;
                for (int j = 0; j < _renderersCuller.NextElementIndex; j++) {
                    if (cullDistanceIndices[j] == i) {
                        ++countMeshes;
                        if (!states[j].IsVisible()) {
                            ++culledMeshes;
                        }
                    }
                }
                allCulledMeshes += culledMeshes;

                var countGroups = 0;
                var culledGroups = 0;
                cullDistanceIndices = _renderersGroupsCuller.CullDistanceIndices;
                states = _renderersGroupsCuller.States;
                for (int j = 0; j < _renderersGroupsCuller.NextElementIndex; j++) {
                    if (cullDistanceIndices[j] == i) {
                        ++countGroups;
                        if (!states[j].IsVisible()) {
                            ++culledGroups;
                        }
                    }
                }
                allCulledGroups += culledGroups;

                GUILayout.Label($"Meshes - count: {countMeshes}; culled: {culledMeshes}");
                GUILayout.Label($"Groups - count: {countGroups}; culled: {culledGroups}");
                GUILayout.EndHorizontal();
                Ranges[i] = range;
            }

            if (GUI.changed) {
                ReInitRanges();
            }

            GUILayout.BeginHorizontal();
            var distanceCullerSettings = World.Only<DistanceCullingSetting>();
            GUILayout.Label("Bias: ");
            distanceCullerSettings.OnGUI();
            GUILayout.Label(distanceCullerSettings.Value.ToString("P0"));
            GUILayout.Label(" internal: ");
            GUILayout.Label(Bias.ToString("F2"));
            GUILayout.EndHorizontal();

            GUILayout.Label($"Registered meshes: {_renderersCuller.NextElementIndex}; culled meshes: {allCulledMeshes}");
            GUILayout.Label($"Registered groups: {_renderersGroupsCuller.NextElementIndex}; culled groups: {allCulledGroups}");
#if ENABLE_PROFILER
            GUILayout.Label($"Update.All {ProfilerMarkerUtils.GetTiming(_updateAllMarker):f2} ms");
#endif
            GUI.changed = hadChange || GUI.changed;
        }
        
        void ReInitRanges() {
            UpdateState();
            
            for (int i = 0; i < Ranges.Length; i++) {
                _cullDistancesSq[i] = Ranges[i].distance * Ranges[i].distance / Bias;
            }

            for (var i = 0; i < _renderersCuller.NextElementIndex; i++) {
                var bounds = _renderers[i].Renderer.bounds;
                var index = GetDistanceIndex(ToRegisterRenderer.Volume(bounds.size));
                if (index == -1) {
                    index = Ranges.Length - 1;
                }
                _renderersCuller.UpdateDistanceIndex(i, index);
            }

            for (var i = 0; i < _renderersGroupsCuller.NextElementIndex; i++) {
                var index = GetDistanceIndex(renderersGroups[i].Volume);
                if (index == -1) {
                    index = Ranges.Length - 1;
                }
                _renderersGroupsCuller.UpdateDistanceIndex(i, index);
            }
        }

        public DistanceCullerData State(int id, DistanceCullerEntity requester) {
            if (requester is DistanceCullerRenderer) {
                return _renderersCuller.States[id];
            }
            if (requester is DistanceCullerGroup) {
                return _renderersGroupsCuller.States[id];
            }
            return default;
        }
        
        public BoundsCorners Corners(int id, DistanceCullerEntity requester) {
            if (requester is DistanceCullerRenderer) {
                return _renderersCuller.Corners[id];
            }
            if (requester is DistanceCullerGroup) {
                return _renderersGroupsCuller.Corners[id];
            }
            return default;
        }

#if DEBUG
        [ShowInInspector] int UsedRenderers => _renderersCuller.NextElementIndex;

        [ShowInInspector] DistanceCullerEntity[] InvalidElements => _distanceCullerRenderers
            .Where(static r => r && !r.IsInValidState)
            .Cast<DistanceCullerEntity>()
            .Union(renderersGroups.Where(static r => r && !r.IsInValidState))
            .ToArray();
#endif
    }
}
