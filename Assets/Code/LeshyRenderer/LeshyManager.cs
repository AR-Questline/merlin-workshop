using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.CommonInterfaces;
using Awaken.TG.Main.Cameras;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Graphics.Mipmaps;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths.Data;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.IL2CPP.CompilerServices;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UniversalProfiling;
using LogType = Awaken.Utility.Debugging.LogType;
using Random = Unity.Mathematics.Random;

namespace Awaken.TG.LeshyRenderer {
    [ExecuteInEditMode, Il2CppEagerStaticClassConstruction]
    public sealed class LeshyManager : PlayerLoopBasedLifetimeMonoBehaviour, IDomainBoundService, IMainMemorySnapshotProvider, INavMeshBakingPreparer {
        static readonly UniversalProfilerMarker SpawnCellMarker = new UniversalProfilerMarker("Leshy.SpawnCell");
        static readonly UniversalProfilerMarker AddCellInstancesMarker = new UniversalProfilerMarker("Leshy.AddCellInstances");
        static readonly UniversalProfilerMarker RemoveInvisibleCellsMarker = new UniversalProfilerMarker("Leshy.RemoveInvisibleCells");
        static readonly UniversalProfilerMarker AddVisibleCellsMarker = new UniversalProfilerMarker("Leshy.AddVisibleCells");

        [SerializeField, OnValueChanged(nameof(EDITOR_ForceDisabledChanged))] bool forceDisable;
        [SerializeField, Required, InlineButton(nameof(EDITOR_CreateNew), ShowIf = nameof(EDITOR_PrefabEmpty), Label = "New")]
        LeshyPrefabs prefabs;
        [SerializeField, Required] VegetationSettings settings;
        [SerializeField, Required] ComputeShader fillGPUBuffer;
        [SerializeField, Range(1, 64)] ushort maxOngoingReads = 32;
        [SerializeField, Range(0.1f, 10f)] float graceTime = 1.5f;
        [SerializeField, Range(0.01f, 1f)] float maxFillValue = 1f;
        [ShowIf(nameof(EDITOR_ShowEditorOnly)), ShowInInspector] bool _forceGameCameraInSceneView;

        bool _initialized;
        string _basePath;

        LeshyRendering _rendering;
        LeshyCells _cells;
        LeshyLoadingManager _loading;
        LeshyCollidersManager _colliders;
        LeshyQualityManager _quality;

        UnsafeBitmask _spawnedCells;
        UnsafeArray<float> _despawnGraceTime;

        RegisteredPrefab[] _registeredPrefabs;

        GameCamera _gameCamera;

        public bool ForceDisable => forceDisable;
        public string CatalogPath => Path.Combine(BasePath, LeshyPersistence.CellsCatalogBinFile);
        public string MatricesPath => Path.Combine(BasePath, LeshyPersistence.MatricesBinFile);
        string BasePath => _basePath ??= LeshyPersistence.BasePath(gameObject.scene.name);

        // === Debugging
        [ShowInInspector]
        public bool EnabledRendering {
            get => _rendering is { Enabled: true };
            set {
                if (_rendering != null) {
                    _rendering.Enabled = value;
                }
            }
        }

        [ShowInInspector]
        public bool EnabledCells {
            get => _cells.Enabled;
            set => _cells.Enabled = value;
        }

        [ShowInInspector]
        public bool EnabledCollider {
            get => _colliders.Enabled;
            set => _colliders.Enabled = value;
        }

        [ShowInInspector]
        public bool EnabledLoading {
            get => _loading.Enabled;
            set => _loading.Enabled = value;
        }

        // === Services
        public Domain Domain => Domain.CurrentMainScene();
        public bool RemoveOnDomainChange() => true;

        // === Lifetime
        protected override void OnPlayerLoopEnable() {
            if (forceDisable || !this
#if UNITY_EDITOR && !SIMULATE_BUILD
            || UnityEditor.EditorPrefs.GetInt("debug.leshy.disabled", 0) == 1
#endif
            ) {
                return;
            }

            if (!prefabs || !settings || !fillGPUBuffer) {
                Log.Important?.Error("LeshyManager is missing required fields");
                return;
            }

            _quality.Init(this, settings);
            var runtimePrefabs = prefabs.RuntimePrefabs(_quality);
            _cells.Init(CatalogPath, runtimePrefabs);
            var allInstancesCount = 0u;
            var maxInstances = 0;
            for (var i = 0u; i < _cells.cellsCatalog.Length; i++) {
                allInstancesCount += _cells.cellsCatalog[i].instancesCount;
                maxInstances = math.max(maxInstances, (int)_cells.cellsCatalog[i].instancesCount);
            }

            if (allInstancesCount < 1) {
                _cells.Dispose();
                _quality.Dispose();
                return;
            }

            _rendering = new LeshyRendering();
            _rendering.Init(allInstancesCount, maxInstances, maxFillValue, this, fillGPUBuffer);
            _loading.Init(maxOngoingReads, MatricesPath, _cells);
            _colliders.Init(gameObject.scene, _cells, prefabs);

            InitRenderingData(runtimePrefabs);

            _spawnedCells = new UnsafeBitmask((uint)_cells.CellsCount, ARAlloc.Persistent);
            _despawnGraceTime = new UnsafeArray<float>((uint)_cells.CellsCount, ARAlloc.Persistent);
            _initialized = true;
            IMainMemorySnapshotProvider.RegisterProvider(this);
        }

        protected override void OnPlayerLoopDisable() {
            if (!_initialized) {
                return;
            }
            foreach (var index in _spawnedCells.EnumerateOnes()) {
                DespawnCell(index);
            }
            _spawnedCells.Dispose();
            _despawnGraceTime.Dispose();

            for (int i = 0; i < _registeredPrefabs.Length; i++) {
                var prefab = _registeredPrefabs[i];
                prefab.rendererIds.Dispose();
            }
            _registeredPrefabs = Array.Empty<RegisteredPrefab>();

            _loading.Dispose();
            _cells.Dispose();
            _rendering.Dispose();
            _rendering = null;
            _colliders.Dispose();
            _quality.Dispose();
            _initialized = false;
            IMainMemorySnapshotProvider.UnregisterProvider(this);
        }

        protected override void OnUnityEnable() {
            base.OnUnityEnable();
            EnabledRendering = true;
            EnabledCells = true;
            EnabledCollider = true;
            EnabledLoading = true;
        }

        protected override void OnUnityDisable() {
            base.OnUnityDisable();
            EnabledRendering = false;
            EnabledCells = false;
            EnabledCollider = false;
            EnabledLoading = false;
        }

        void InitRenderingData(LeshyPrefabs.PrefabRuntime[] runtimePrefabs) {
            _registeredPrefabs = new RegisteredPrefab[runtimePrefabs.Length];
            for (int i = 0; i < runtimePrefabs.Length; i++) {
                var renderers = runtimePrefabs[i].renderers;
                var rendererIds = new UnsafeArray<RendererId>((uint)renderers.Length, Allocator.Persistent);
                for (int j = 0; j < renderers.Length; j++) {
                    var (meshId, uvDistributionID) = _rendering.RegisterMesh(renderers[j].mesh);
                    var materialsCount = (byte)renderers[j].materials.Length;
                    var materialsIds = new UnsafeArray<BatchMaterialID>(materialsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    var mipmapsIds = new UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>(materialsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    for (uint k = 0; k < renderers[j].materials.Length; k++) {
                        (materialsIds[k], mipmapsIds[k]) = _rendering.RegisterMaterial(renderers[j].materials[k]);
                    }
                    var filterSettingsId = _rendering.RegisterFilterSettings(renderers[j].filterSettings);
                    var rendererData = new RendererDefinition {
                        meshID = meshId,
                        reciprocalUVDistributionID = uvDistributionID,
                        materialsID = materialsIds,
                        mipmapsID = mipmapsIds,
                        filterSettingsId = filterSettingsId,
                        lodMask = renderers[j].lodMask,
                    };
                    var rendererId = _rendering.RegisterRenderer(rendererData);

                    rendererIds[(uint)j] = rendererId;
                }
                _registeredPrefabs[i] = new RegisteredPrefab {
                    rendererIds = rendererIds,
                };
            }
        }

        // === Operations
        public JobHandle CullCameraInstances(in BatchCullingContext cullingContext, JobHandle dependency) {
            return LeshyInstancesCameraCulling.PerformCameraCulling(cullingContext, ref _cells, _spawnedCells, _loading.filteredData, _forceGameCameraInSceneView, dependency);
        }

        public JobHandle CullLightInstances(in BatchCullingContext cullingContext, JobHandle dependency) {
            return LeshyInstancesLightCulling.PerformLightCulling(cullingContext, ref _cells, _spawnedCells, _loading.filteredData, dependency);
        }

        public void RunMipmapsStreaming(in CameraData cameraData, in MipmapsStreamingMasterMaterials.ParallelWriter writer) {
            _spawnedCells.ToIndicesOfOneArray(ARAlloc.TempJob, out var spawnedCells);
            var calcHandle = new LeshyRendering.CalculateMipmapsJob {
                cameraData = cameraData,
                spawnedCellsIndices = spawnedCells,

                aabbCenterXs = _cells.aabbCenterXs,
                aabbCenterYs = _cells.aabbCenterYs,
                aabbCenterZs = _cells.aabbCenterZs,
                radii = _cells.prefabsRadii,
                cellTransforms = _loading.filteredData,

                mipmapsFactors = _cells.perInstanceMipmapsFactor,
            }.ScheduleParallel(spawnedCells.LengthInt, 32, default);
            calcHandle = spawnedCells.Dispose(calcHandle);

            _rendering.RunMipmapsDumpJob(calcHandle, writer);
        }

        public void QualityChanged() {
            _quality.QualityChanged();
        }

        void Update() {
            if (!_initialized) {
                return;
            }
            _gameCamera ??= World.Any<GameCamera>();
            var mainCamera = _gameCamera?.MainCamera;
            if (!mainCamera) {
#if UNITY_EDITOR
                mainCamera = Camera.main ?? UnityEditor.SceneView.lastActiveSceneView?.camera;
                if (!mainCamera) {
                    return;
                }
#else
                return;
#endif
            }

            // Update cells visibility
            _cells.CalculateCellsVisibility(mainCamera);
            var visibleCells = _cells.finalCellsVisibility;

            // Remove invisible cells
            RemoveInvisibleCellsMarker.Begin();
            var removed = false;
            for (uint i = 0; i < visibleCells.Length; i++) {
                if (!visibleCells.IsSet((int)i) && _spawnedCells[i]) {
                    var graceTime = _despawnGraceTime[i];
                    graceTime -= Time.deltaTime;
                    if (graceTime <= 0) {
                        removed = true;
                        DespawnCell(i);
                    } else {
                        _despawnGraceTime[i] = graceTime;
                    }
                }
            }
            if (removed) {
                _rendering.ConsolidateAfterRemovals();
            }
            RemoveInvisibleCellsMarker.End();

            // Update loading
            _loading.Update(_cells);

            // Register new cells
            AddVisibleCellsMarker.Begin();
            _rendering.BeginAdditions();
            var added = false;
            for (uint i = 0; i < visibleCells.Length; i++) {
                if (_loading.IsLoaded((int)i) && !_spawnedCells[i]) {
                    added = SpawnCell(i) || added;
                }
            }
            if (added) {
                _rendering.ConsolidateAfterAdditions();
            }
            _rendering.EndAdditions();
            AddVisibleCellsMarker.End();

            // Colliders
            if (Application.isPlaying) {
                _colliders.UpdateColliders(mainCamera.transform.position, _spawnedCells, _cells, _loading, prefabs);
            }
        }

        void DespawnCell(uint cellIndex) {
            _rendering.RemoveInstances(_cells.cellsInstances[cellIndex]);
            _cells.DespawnCell(cellIndex);
            _loading.DespawnCell(cellIndex);
            _spawnedCells.Down(cellIndex);
        }

        bool SpawnCell(uint cellIndex) {
            SpawnCellMarker.Begin();
            var transforms = _loading.loadedData[cellIndex];
            _loading.filteredData[cellIndex] = FilterTransformsByDensity(transforms, cellIndex);
            var registered = RegisterCellData(_loading.filteredData[cellIndex], cellIndex);
            if (registered) {
                _spawnedCells.Up(cellIndex);
                _despawnGraceTime[cellIndex] = graceTime;
            }
            SpawnCellMarker.End();
            return registered;
        }

        unsafe UnsafeArray<SmallTransform>.Span FilterTransformsByDensity(UnsafeArray<SmallTransform> transforms, uint cellIndex) {
            var prefab = CellPrefab(cellIndex);
            var density = math.min(_quality.Density(prefab.prefabType), 1 - prefab.hidePercent);
            uint removedItems = 0;
            var rng = new Random(69+cellIndex);

            for (uint i = transforms.Length-1; i > 0; i--) {
                var random = rng.NextFloat();
                if (random > density) {
                    // Swap back
                    var swapIndex = transforms.Length - removedItems - 1;
                    (transforms[i], transforms[swapIndex]) = (transforms[swapIndex], transforms[i]);
                    ++removedItems;
                }
            }

            return UnsafeArray<SmallTransform>.FromExistingData(transforms.Ptr, transforms.Length - removedItems);
        }

        bool RegisterCellData(UnsafeArray<SmallTransform>.Span transforms, uint cellIndex) {
            AddCellInstancesMarker.Begin();
            var prefabId = CellPrefabId(cellIndex);
            var prefab = _registeredPrefabs[prefabId];
            var rendererIds = prefab.rendererIds;
            var instancesHandle = _rendering.AddInstances(transforms, rendererIds);
            if (instancesHandle.IsCreated) {
                _cells.AddAllocatedCell(cellIndex, instancesHandle);
            }
            AddCellInstancesMarker.End();
            return instancesHandle.IsCreated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort CellPrefabId(uint cellIndex) {
            return _cells.cellsCatalog[cellIndex].prefabId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        LeshyPrefabs.PrefabAuthoring CellPrefab(uint cellIndex) {
            return prefabs.Prefabs[CellPrefabId(cellIndex)];
        }

        // === Editor
        // ReSharper disable InconsistentNaming
        public LeshyCells EDITOR_Cells => _cells;
        public LeshyPrefabs EDITOR_Prefabs => prefabs;
        public LeshyRendering EDITOR_Rendering => _rendering;
        public LeshyQualityManager EDITOR_Quality => _quality;
        public LeshyLoadingManager EDITOR_Loading => _loading;
        public UnsafeBitmask EDITOR_SpawnedCells => _spawnedCells;

        [ShowInInspector, ShowIf(nameof(EDITOR_ShowEditorOnly))] uint EDITOR_CellsLoadedCount => _spawnedCells.IsCreated ? _spawnedCells.CountOnes() : 0u;
        [ShowInInspector, ShowIf(nameof(EDITOR_ShowEditorOnly)), ListDrawerSettings(DefaultExpandedState = false, ShowIndexLabels = true)]
        CatalogCellData[] EDITOR_CellsData => _cells.cellsCatalog.IsCreated ? _cells.cellsCatalog.ToManagedArray() : Array.Empty<CatalogCellData>();
        [ShowInInspector, ShowIf(nameof(EDITOR_ShowEditorOnly))] uint EDITOR_SpawnedInstances {
            get {
                if (!_loading.filteredData.IsCreated) {
                    return 0;
                }
                var count = 0u;
                for (uint i = 0; i < _loading.filteredData.Length; i++) {
                    if (_loading.filteredData[i].IsValid) {
                        count += _loading.filteredData[i].Length;
                    }
                }
                if (count > EDITOR_MaxSpawnedInstances) {
                    EDITOR_MaxSpawnedInstances = count;
                }
                return count;
            }
        }

        [ShowInInspector, ShowIf(nameof(EDITOR_ShowEditorOnly))] uint EDITOR_LoadedInstances {
            get {
                if (!_loading.loadedData.IsCreated) {
                    return 0;
                }
                var count = 0u;
                for (uint i = 0; i < _loading.loadedData.Length; i++) {
                    if (_loading.loadedData[i].IsCreated) {
                        count += _loading.loadedData[i].Length;
                    }
                }
                return count;
            }
        }

        [ShowInInspector, ShowIf(nameof(EDITOR_ShowEditorOnly)), Sirenix.OdinInspector.ReadOnly] uint EDITOR_MaxSpawnedInstances { get; set; }

        [ShowInInspector, ShowIf(nameof(EDITOR_ShowEditorOnly))] int EDITOR_TakenRangesCount => _rendering?.TakenRanges.Length ?? -1;
        [ShowInInspector, ShowIf(nameof(EDITOR_ShowEditorOnly))] unsafe uint EDITOR_FreeRangesSlotsCount {
            get {
                if (_rendering == null) {
                    return 0;
                }
                var freeRanges = _rendering.FreeRanges;
                var count = 0u;
                for (int i = 0; i < freeRanges.Length; i++) {
                    count += (freeRanges.Ptr + i)->count;
                }
                return count;
            }
        }

        [ShowInInspector, ShowIf(nameof(EDITOR_ShowEditorOnly)), ListDrawerSettings(DefaultExpandedState = false, ShowIndexLabels = true)]
        LeshyRendering.InstancesRange[] EDITOR_FreeRanges => _rendering?.FreeRanges.ToArray();

        bool EDITOR_ShowEditorOnly => Application.isPlaying;
        bool EDITOR_PrefabEmpty => prefabs == null;

        void EDITOR_ForceDisabledChanged() {
            OnDisable();
            OnEnable();
        }

        public void EDITOR_SetDisabled(bool disabled) {
            forceDisable = disabled;
            EDITOR_ForceDisabledChanged();
        }

        void EDITOR_CreateNew() {
#if UNITY_EDITOR
            var sceneName = gameObject.scene.name;
            var path = $"Assets/Data/Leshy/{sceneName}Prefabs.asset";
            prefabs = UnityEditor.AssetDatabase.LoadAssetAtPath<LeshyPrefabs>(path);
            if (prefabs != null) {
                return;
            }
            UnityEditor.AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<LeshyPrefabs>(), path);
            prefabs = UnityEditor.AssetDatabase.LoadAssetAtPath<LeshyPrefabs>(path);
#endif
        }

        // ReSharper restore InconsistentNaming

        void OnDrawGizmos() {
            var gameCamera = World.Any<GameCamera>();
            var mainCamera = gameCamera?.MainCamera;
            if (!mainCamera) {
                return;
            }
            Gizmos.color = Color.magenta;
            var oldGizmosMatrix = Gizmos.matrix;
            Gizmos.matrix = mainCamera.transform.localToWorldMatrix;
            Gizmos.DrawFrustum(Vector3.zero, mainCamera.fieldOfView, mainCamera.farClipPlane, mainCamera.nearClipPlane, mainCamera.aspect);
            Gizmos.matrix = oldGizmosMatrix;
        }

        // IMainMemorySnapshotProvider
        public unsafe int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            if (!_initialized) {
                ownPlace.Span[0] = new MemorySnapshot("LeshyManager", 0, default);
                return 0;
            }
            var childrenCount = 3;

            var spawnedSize = _spawnedCells.SafeBucketsLength() / 8; // bits to bytes
            var graceTimeSize = _despawnGraceTime.Length * sizeof(float);
            var registeredPrefabsSize = (_registeredPrefabs?.Sum(static p => p.rendererIds.Length) ?? 0) * sizeof(RendererId);
            var ownSize = (ulong)(spawnedSize + graceTimeSize + registeredPrefabsSize);
            ownPlace.Span[0] = new MemorySnapshot("LeshyManager", ownSize, ownSize, memoryBuffer[..childrenCount]);

            var wholeAllocation = 0;

            var children = memoryBuffer[childrenCount..];
            var allocated = _rendering.GetMemorySnapshot(children, memoryBuffer[..1]);
            wholeAllocation += allocated;
            children = children[allocated..];
            allocated = _cells.GetMemorySnapshot(children, memoryBuffer.Slice(1, 1));
            wholeAllocation += allocated;
            children = children[allocated..];
            wholeAllocation += _loading.GetMemorySnapshot(children, memoryBuffer.Slice(2, 1));

            return wholeAllocation;
        }
        public int PreallocationSize => 10_000;
        
        INavMeshBakingPreparer.IReversible INavMeshBakingPreparer.Prepare() {
            return new LeshyNavMeshBakingPreparation(this, prefabs);
        }
    }

    public struct RegisteredPrefab {
        public UnsafeArray<RendererId> rendererIds;
    }
}
