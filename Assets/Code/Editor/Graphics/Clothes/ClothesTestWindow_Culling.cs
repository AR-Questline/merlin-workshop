using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.Kandra;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor;
using Awaken.Utility.GameObjects;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using ColorUtils = Awaken.Utility.Maths.ColorUtils;

namespace Awaken.TG.Editor.Graphics.Clothes {
    [BurstCompile]
    public partial class ClothesTestWindow {
        static readonly int AlphaID = Shader.PropertyToID("_Alpha");
        static readonly int DoubleSidedEnable = Shader.PropertyToID("_DoubleSidedEnable");

        [NonSerialized] bool _isDirty;
        [NonSerialized] KandraTrisCullee _cullee;
        [NonSerialized] KandraRenderer _culleeRenderer;

        [NonSerialized] List<KandraTrisCuller> _cullers = new List<KandraTrisCuller>();
        [NonSerialized] List<KandraRenderer> _cullerRenderers = new List<KandraRenderer>();
        [NonSerialized] List<GameObject> _prefabs = new List<GameObject>();
        [NonSerialized] List<Material[]> _originalMaterials = new List<Material[]>();
        [NonSerialized] List<Material[]> _cullingMaterials = new List<Material[]>();
        [NonSerialized] List<float[]> _originalDoubleSided = new List<float[]>();

        [NonSerialized] bool _wasTransparent;
        List<ToSaveData> _savedCulling = new List<ToSaveData>();

        [NonSerialized] bool _isPainting;
        [NonSerialized] bool _unculling;

        [NonSerialized] Vector3[] _wireframeVertices;
        [NonSerialized] int[] _visibleTrianglesSegments;
        [NonSerialized] int[] _culledTrianglesSegments;
        [NonSerialized] Optional<HandlesUtils.HandlesTriangle> _selectedTriangle;
        [NonSerialized] HandlesUtils.HandlesTriangle[] _brushTriangles;

        GUIStyle _labelStyle;
        GUIStyle LabelStyle => _labelStyle ??= new GUIStyle(EditorStyles.label) { richText = true, };

        [BoxGroup("PlayOnly/Culling", Order = CullingOrder)]
        [ShowInInspector, Range(-1f, 1f)] float HideBackTrianglesFactor {
            get => EditorPrefs.GetFloat(PreferenceKey(nameof(HideBackTrianglesFactor)), 0f);
            set => EditorPrefs.SetFloat(PreferenceKey(nameof(HideBackTrianglesFactor)), value);
        }

        [BoxGroup("PlayOnly/Culling")]
        [ShowInInspector] bool ShowTips {
            get => EditorPrefs.GetBool(PreferenceKey(nameof(ShowTips)), true);
            set => EditorPrefs.SetBool(PreferenceKey(nameof(ShowTips)), value);
        }

        [BoxGroup("PlayOnly/Culling/Painting")]
        [ShowInInspector, NonSerialized, OnValueChanged(nameof(PaintingModeChanged)), EnableIf(nameof(CanEnterPaintingMode))]
        PaintingMode _paintingMode = PaintingMode.None;

        [BoxGroup("PlayOnly/Culling/Painting")]
        [ShowInInspector, PropertyRange(0.001f, 0.25f)]
        float Radius {
            get => EditorPrefs.GetFloat(PreferenceKey(nameof(Radius)), 0.03f);
            set => EditorPrefs.SetFloat(PreferenceKey(nameof(Radius)), value);
        }

        [BoxGroup("PlayOnly/Culling/Painting")]
        [OnValueChanged(nameof(OnAlphaChanged)), Range(0, 1), ShowInInspector]
        float _clothesAlpha = 1f;

        [BoxGroup("PlayOnly/Culling/Painting")]
        [ShowInInspector]
        Color CullingColor {
            get => GetColorFromPreferences(PreferenceKey(nameof(CullingColor)), new Color(0.33f, 0.35f, 0.41f, 1f));
            set => SetColorToPreferences(PreferenceKey(nameof(CullingColor)), value);
        }
        [BoxGroup("PlayOnly/Culling/Painting")]
        [ShowInInspector]
        Color UncullingColor {
            get => GetColorFromPreferences(PreferenceKey(nameof(UncullingColor)), new Color(0.44f, 0.72f, 0.51f, 1f));
            set => SetColorToPreferences(PreferenceKey(nameof(UncullingColor)), value);
        }

        [BoxGroup("PlayOnly/Culling/Painting")]
        [ShowInInspector]
        bool PressToUncull {
            get => EditorPrefs.GetBool(PreferenceKey(nameof(PressToUncull)), false);
            set => EditorPrefs.SetBool(PreferenceKey(nameof(PressToUncull)), value);
        }

        [BoxGroup("PlayOnly/Culling/Wireframe")]
        [ShowInInspector, NonSerialized] bool _showWireframe;

        [BoxGroup("PlayOnly/Culling/Wireframe")]
        [ShowInInspector] Color VisibleTrianglesColor {
            get => GetColorFromPreferences(PreferenceKey(nameof(VisibleTrianglesColor)), Color.yellow);
            set => SetColorToPreferences(PreferenceKey(nameof(VisibleTrianglesColor)), value);
        }
        [BoxGroup("PlayOnly/Culling/Wireframe")]
        [ShowInInspector] Color CulledTrianglesColor {
            get => GetColorFromPreferences(PreferenceKey(nameof(CulledTrianglesColor)), Color.red);
            set => SetColorToPreferences(PreferenceKey(nameof(CulledTrianglesColor)), value);
        }

        [FoldoutGroup("PlayOnly/Culling/Autoculling")]
        [OnValueChanged(nameof(BakeAutoCulling)), Range(0, 0.1f), ShowInInspector]
        float CullerOffset {
            get => EditorPrefs.GetFloat(PreferenceKey(nameof(CullerOffset)), 0.01f);
            set => EditorPrefs.SetFloat(PreferenceKey(nameof(CullerOffset)), value);
        }

        [FoldoutGroup("PlayOnly/Culling/Autoculling")]
        [OnValueChanged(nameof(BakeAutoCulling)), Range(0.05f, 0.3f), ShowInInspector]
        float CullerDistance {
            get => EditorPrefs.GetFloat(PreferenceKey(nameof(CullerDistance)), 0.2f);
            set => EditorPrefs.SetFloat(PreferenceKey(nameof(CullerDistance)), value);
        }

        // === Callbacks
        void SpawnedPrefab() {
            _cullee = _previewInstance.GetComponentInChildren<KandraTrisCullee>();
            _culleeRenderer = _cullee.kandraRenderer;
        }

        void RemovedPrefab() {
            if (_paintingMode != PaintingMode.None) {
                _paintingMode = PaintingMode.None;
                PaintingModeChanged();
            }

            _cullee = null;
            _culleeRenderer = null;
        }

        void ClothSpawned(GameObject instance, GameObject prefab) {
            var renderers = instance.GetComponentsInChildren<KandraRenderer>();
            foreach (var renderer in renderers) {
                if (!renderer.TryGetComponent<KandraTrisCuller>(out var culler)) {
                    culler = renderer.gameObject.AddComponent<KandraTrisCuller>();
                    var culledMeshArray = new KandraTrisCuller.CulledMesh[1];
                    culledMeshArray[0] = new KandraTrisCuller.CulledMesh {
                        culleeId = _cullee.id,
                        culledRanges = Array.Empty<KandraTrisCuller.CulledRange>(),
                    };
                    culler.culledMeshes = culledMeshArray;
                }
                _cullers.Add(culler);
                _cullerRenderers.Add(renderer);
                _prefabs.Add(prefab);
                PrepareMaterials(renderer);
            }

            if (_paintingMode != PaintingMode.None && _cullers.Count != 1) {
                _paintingMode = PaintingMode.None;
                PaintingModeChanged();
            }
        }

        void ClothRemoved(GameObject instance) {
            var renderers = instance.GetComponentsInChildren<KandraRenderer>();
            foreach (var renderer in renderers) {
                var index = _cullerRenderers.IndexOf(renderer);
                renderer.UseOriginalMaterials();

                foreach (var cullingMaterial in _cullingMaterials[index]) {
                    DestroyImmediate(cullingMaterial);
                }

                _cullers.RemoveAt(index);
                _cullerRenderers.RemoveAt(index);
                _prefabs.RemoveAt(index);
                _originalMaterials.RemoveAt(index);
                _cullingMaterials.RemoveAt(index);
                _originalDoubleSided.RemoveAt(index);
            }

            if (_paintingMode != PaintingMode.None && _cullers.Count != 1) {
                _paintingMode = PaintingMode.None;
                PaintingModeChanged();
            }
        }

        void EnteredEditMode() {
            foreach (var saveData in _savedCulling) {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(saveData.prefabPath);
                var cullerRenderer = prefab.FindRecursively<KandraRenderer>(saveData.cullerName);
                if (!cullerRenderer.TryGetComponent<KandraTrisCuller>(out var culler)) {
                    culler = cullerRenderer.gameObject.AddComponent<KandraTrisCuller>();
                }
                culler.culledMeshes = saveData.culledMeshes;
                EditorUtility.SetDirty(culler);
                AssetDatabase.SaveAssetIfDirty(culler);
            }
            _savedCulling.Clear();
        }

        void EnteredPlayMode() {
            UpdateSceneGUI();
        }

        // === Autoculling
        [FoldoutGroup("PlayOnly/Culling/Autoculling")]
        [Button(ButtonSizes.Medium), EnableIf(nameof(CanAutoBake))]
        public void BakeAutoCulling() {
            if (!_cullee) {
                return;
            }

            for (int i = 0; i < _cullers.Count; i++) {
                BakeAutoCullingForCuller(i);
            }
        }

        void BakeAutoCullingForCuller(int cullerIndex) {
            var culler = _cullers[cullerIndex];
            var renderer = _cullerRenderers[cullerIndex];

            culler.Uncull(_cullee);

            var indices = KandraRendererManager.Instance.StreamingManager.LoadIndicesData(_culleeRenderer.rendererData.mesh);
            var trisCount = indices.Length / 3;

            var visibleTriangles = new UnsafeBitmask(trisCount, ARAlloc.Temp);
            visibleTriangles.All();

            foreach (var animation in ClothesTestSetup.clipsForCulling) {
                var samples = animation.frameRate * animation.length;
                samples = math.clamp(samples, 1, 24);
                for (var sample = 0; sample < samples; sample++) {
                    var time = animation.length * sample / samples;
                    animation.SampleAnimation(_animator.gameObject, time);
                    CullingUtilities.AppendAutoCulling(renderer, _culleeRenderer, trisCount, ref visibleTriangles, indices, CullerOffset, CullerDistance);
                }
            }

            KandraRendererManager.Instance.StreamingManager.UnloadIndicesData(_culleeRenderer.rendererData.mesh);

            var ranges = CullingUtilities.CalculateCulledRanges(trisCount, visibleTriangles);

            visibleTriangles.Dispose();

            var cullerId = Array.FindIndex(culler.culledMeshes, mesh => mesh.culleeId == _cullee.id);
            if (cullerId == -1) {
                cullerId = culler.culledMeshes.Length;
                Array.Resize(ref culler.culledMeshes, cullerId + 1);
            }

            culler.culledMeshes[cullerId] = new KandraTrisCuller.CulledMesh {
                culleeId = _cullee.id,
                culledRanges = ranges.ToArray()
            };

            culler.Cull(_cullee);

            _isDirty = true;
        }

        bool CanAutoBake() {
            return _cullee && _spawnedClothes.Count > 0;
        }

        // === Materials
        void OnAlphaChanged() {
            var transparent = _clothesAlpha < 0.9999f;
            var transparentChanged = _wasTransparent != transparent;
            for (int i = 0; i < _cullerRenderers.Count; i++) {
                var renderer = _cullerRenderers[i];
                var materials = _cullingMaterials[i];
                for (int j = 0; j < materials.Length; j++) {
                    if (transparentChanged) {
                        materials[j].SetFloat(DoubleSidedEnable, transparent ? 0 : _originalDoubleSided[i][j]);
                        HDMaterial.SetSurfaceType(materials[j], transparent);
                    }
                    materials[j].SetFloat(AlphaID, _clothesAlpha);
                }
                if (_paintingMode != PaintingMode.None) {
                    if (transparentChanged & (_paintingMode != PaintingMode.None)) {
                        renderer.MaterialsTransparencyChanged();
                    }
                }
            }
            _wasTransparent = transparent;
        }

        void PrepareMaterials(KandraRenderer stitchedRenderer) {
            var transparent = _clothesAlpha < 0.9999f;

            var instancedMaterials = stitchedRenderer.UseInstancedMaterials();

            var originalMaterials = instancedMaterials.ToArray();
            _originalMaterials.Add(originalMaterials);

            var cullingMaterials = originalMaterials.ToArray();
            _cullingMaterials.Add(cullingMaterials);

            var doubleSided = new float[originalMaterials.Length];
            _originalDoubleSided.Add(doubleSided);

            for (int i = 0; i < originalMaterials.Length; i++) {
                var newMaterial = new Material(originalMaterials[i]);
                newMaterial.name = $"{originalMaterials[i].name} (Culling)";
                newMaterial.shader = Shader.Find("TG/Debug/ClothCullingDebug");
                doubleSided[i] = newMaterial.GetFloat(DoubleSidedEnable);
                newMaterial.SetFloat(DoubleSidedEnable, transparent ? 0 : doubleSided[i]);
                HDMaterial.SetSurfaceType(newMaterial, transparent);
                newMaterial.SetFloat(AlphaID, _clothesAlpha);
                cullingMaterials[i] = newMaterial;
            }

            if (_paintingMode != PaintingMode.None) {
                instancedMaterials = stitchedRenderer.rendererData.materialsInstances;
                for (int i = 0; i < instancedMaterials.Length; i++) {
                    instancedMaterials[i] = cullingMaterials[i];
                }
                stitchedRenderer.UpdateRenderingMaterials();
            }

            _wasTransparent = transparent;
        }

        // === Painting
        void PaintingModeChanged() {
            if (_paintingMode == PaintingMode.None) {
                for (int i = 0; i < _originalMaterials.Count; i++) {
                    var instancedMaterials = _cullerRenderers[i].rendererData.materialsInstances;
                    for (int j = 0; j < instancedMaterials.Length; j++) {
                        instancedMaterials[j] = _originalMaterials[i][j];
                    }
                    _cullerRenderers[i].UpdateRenderingMaterials();
                }
            } else {
                for (int i = 0; i < _cullingMaterials.Count; i++) {
                    var instancedMaterials = _cullerRenderers[i].rendererData.materialsInstances;
                    for (int j = 0; j < instancedMaterials.Length; j++) {
                        instancedMaterials[j] = _cullingMaterials[i][j];
                    }
                    _cullerRenderers[i].UpdateRenderingMaterials();
                }
            }

            if (_npcAnimancer) {
                if (_paintingMode != PaintingMode.None) {
                    if (_npcAnimancer.IsPlayableInitialized) {
                        _npcAnimancer.Playable.PauseGraph();
                    }
                    AnimatorSpeed = 0;
                } else {
                    if (_npcAnimancer.IsPlayableInitialized) {
                        _npcAnimancer.Playable.UnpauseGraph();
                    }
                    AnimatorSpeed = 1;
                }
            }
        }

        void SaveCulling() {
            for (int i = 0; i < _cullers.Count; i++) {
                var toSaveData = new ToSaveData {
                    prefabPath = AssetDatabase.GetAssetPath(_prefabs[i]),
                    cullerName = _cullers[i].name,
                    culledMeshes = _cullers[i].culledMeshes.ToArray()
                };

                _savedCulling.RemoveSwapBack(s => s.prefabPath == toSaveData.prefabPath && s.cullerName == toSaveData.cullerName);
                _savedCulling.Add(toSaveData);
            }

            _isDirty = false;
        }

        void PaintCulling(Vector2 mousePos) {
            _isDirty = true;

            var ray = HandleUtility.GUIPointToWorldRay(mousePos);
            var rayOrigin = (float3)ray.origin;
            var rayDirection = (float3)ray.direction;
            var cameraNormal = -SceneView.lastActiveSceneView.camera.transform.forward;

            if (_paintingMode == PaintingMode.Brush) {
                BrushPaintCulling(cameraNormal, rayOrigin, rayDirection);
            } else if (_paintingMode == PaintingMode.Single) {
                SinglePaintCulling(cameraNormal, rayOrigin, rayDirection);
            }
        }

        void BrushPaintCulling(Vector3 cameraNormal, float3 rayOrigin, float3 rayDirection) {
            var culler = _cullers[0];

            culler.Uncull(_cullee);

            var indices = KandraRendererManager.Instance.StreamingManager.LoadIndicesData(_culleeRenderer.rendererData.mesh);
            var trisCount = indices.Length / 3;

            var visibleTriangles = new UnsafeBitmask(trisCount, ARAlloc.Temp);
            visibleTriangles.All();

            culler.DisableCulledTriangles(_cullee.id, ref visibleTriangles);

            var (vertices, additionalData) = _culleeRenderer.BakePoseVertices(ARAlloc.Temp);
            CullingUtilities.BrushPaint(cameraNormal, rayOrigin, rayDirection, ref visibleTriangles, indices, vertices, HideBackTrianglesFactor, Radius, !_unculling);

            KandraRendererManager.Instance.StreamingManager.UnloadIndicesData(_culleeRenderer.rendererData.mesh);
            vertices.Dispose();
            additionalData.Dispose();

            var ranges = CullingUtilities.CalculateCulledRanges(trisCount, visibleTriangles);

            visibleTriangles.Dispose();

            var cullerId = Array.FindIndex(culler.culledMeshes, mesh => mesh.culleeId == _cullee.id);
            if (cullerId == -1) {
                cullerId = culler.culledMeshes.Length;
                Array.Resize(ref culler.culledMeshes, cullerId + 1);
            }

            culler.culledMeshes[cullerId] = new KandraTrisCuller.CulledMesh {
                culleeId = _cullee.id,
                culledRanges = ranges.ToArray()
            };

            culler.Cull(_cullee);
        }

        void SinglePaintCulling(Vector3 cameraNormal, float3 rayOrigin, float3 rayDirection) {
            var culler = _cullers[0];

            culler.Uncull(_cullee);

            var indices = KandraRendererManager.Instance.StreamingManager.LoadIndicesData(_culleeRenderer.rendererData.mesh);
            var trisCount = indices.Length / 3;

            var visibleTriangles = new UnsafeBitmask(trisCount, ARAlloc.Temp);
            visibleTriangles.All();

            culler.DisableCulledTriangles(_cullee.id, ref visibleTriangles);

            var (vertices, additionalData) = _culleeRenderer.BakePoseVertices(ARAlloc.Temp);
            CullingUtilities.SinglePaint(cameraNormal, rayOrigin, rayDirection, ref visibleTriangles, indices, vertices, HideBackTrianglesFactor, !_unculling);

            KandraRendererManager.Instance.StreamingManager.UnloadIndicesData(_culleeRenderer.rendererData.mesh);
            vertices.Dispose();
            additionalData.Dispose();

            var ranges = CullingUtilities.CalculateCulledRanges(trisCount, visibleTriangles);

            visibleTriangles.Dispose();

            var cullerId = Array.FindIndex(culler.culledMeshes, mesh => mesh.culleeId == _cullee.id);
            if (cullerId == -1) {
                cullerId = culler.culledMeshes.Length;
                Array.Resize(ref culler.culledMeshes, cullerId + 1);
            }

            culler.culledMeshes[cullerId] = new KandraTrisCuller.CulledMesh {
                culleeId = _cullee.id,
                culledRanges = ranges.ToArray()
            };

            culler.Cull(_cullee);
        }

        bool CanEnterPaintingMode() {
            return _cullee && _cullers.Count == 1;
        }

        // === SceneView
        void UpdateSceneGUI() {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.ExitingPlayMode) {
                    SceneView.duringSceneGui -= OnSceneGUI;
                }
            };

            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnSceneGUI(SceneView sceneView) {
            CreateHandlesData(sceneView.camera);
            DrawHandles();
            ProcessInput();
            DrawInfo(sceneView);
            DrawTips(sceneView);
        }

        void ProcessInput() {
            Event e = Event.current;
            var mousePos = e.mousePosition;

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F1) {
                if (CanEnterPaintingMode()) {
                    _paintingMode = PaintingMode.Single;
                    PaintingModeChanged();
                }
                e.Use();
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F2) {
                if (CanEnterPaintingMode()) {
                    _paintingMode = PaintingMode.Brush;
                    PaintingModeChanged();
                }
                e.Use();
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F3) {
                _paintingMode = PaintingMode.None;
                PaintingModeChanged();
                e.Use();
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.S && e.modifiers == EventModifiers.Control) {
                SaveCulling();
                e.Use();
            }

            if (_paintingMode == PaintingMode.None) {
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0) {
                _isPainting = true;
                e.Use();
                PaintCulling(mousePos);
            } else if (_isPainting && e.type == EventType.MouseDrag && e.button == 0) {
                e.Use();
                PaintCulling(mousePos);
            } else if (e.type == EventType.MouseUp && e.button == 0) {
                _isPainting = false;
                e.Use();
            }

            if (PressToUncull) {
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Z) {
                    _unculling = !_unculling;
                    e.Use();
                }
            } else {
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Z) {
                    _unculling = true;
                    e.Use();
                }
                if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Z) {
                    _unculling = false;
                    e.Use();
                }
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Period) {
                _clothesAlpha = math.min(_clothesAlpha + 0.1f, 1);
                OnAlphaChanged();
                e.Use();
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Comma) {
                _clothesAlpha = math.max(_clothesAlpha - 0.1f, 0);
                OnAlphaChanged();
                e.Use();
            }

            if (e.type == EventType.ScrollWheel && e.modifiers == EventModifiers.Shift) {
                Radius += e.delta.x * 0.002f;
                Radius = math.clamp(Radius, 0.001f, 0.25f);
                e.Use();
            }
        }

        unsafe void CreateHandlesData(Camera sceneCamera) {
            if (!_culleeRenderer) {
                return;
            }
            var kandraMesh = _culleeRenderer.rendererData.mesh;
            var (vertices, additionalData) = _culleeRenderer.BakePoseVertices(ARAlloc.Temp);
            var indices = KandraRendererManager.Instance.StreamingManager.LoadIndicesData(kandraMesh);

            // Cache vertices
            var verticesCount = vertices.Length;
            if (_wireframeVertices == null || _wireframeVertices.Length < verticesCount) {
                _wireframeVertices = new Vector3[verticesCount];
            }

            fixed (Vector3* verticesPtr = &_wireframeVertices[0]) {
                var sourceVerticesPtr = vertices.Ptr;
                for (var i = 0; i < verticesCount; i++) {
                    verticesPtr[i] = sourceVerticesPtr[i].position;
                }
            }

            // Cache wireframe segments
            var trisCount = indices.Length / 3;

            var visibleTriangles = KandraTrisCullee.EditorAccess.GetVisibleTriangles(_cullee, ARAlloc.Temp);
            var visibleTrisCount = visibleTriangles.CountOnes();
            var culledTrisCount = trisCount - visibleTrisCount;

            var visibleSegments = new UnsafeList<int>((int)(visibleTrisCount * 6), ARAlloc.Temp);
            var culledSegments = new UnsafeList<int>((int)(culledTrisCount * 6), ARAlloc.Temp);

            var cameraNormal = -sceneCamera.transform.forward;
            CullingUtilities.FillWireframeLineSegments(trisCount, indices, vertices, cameraNormal, visibleTriangles, ref visibleSegments, ref culledSegments, HideBackTrianglesFactor);

            if (_visibleTrianglesSegments == null || _visibleTrianglesSegments.Length != visibleSegments.Length) {
                _visibleTrianglesSegments = new int[visibleSegments.Length];
            }
            if (_visibleTrianglesSegments.Length > 0) {
                fixed (int* visibleTrianglesSegmentsPtr = &_visibleTrianglesSegments[0]) {
                    var visibleSegmentsPtr = visibleSegments.Ptr;
                    for (var i = 0; i < visibleSegments.Length; i++) {
                        visibleTrianglesSegmentsPtr[i] = visibleSegmentsPtr[i];
                    }
                }
            }
            visibleSegments.Dispose();

            if (_culledTrianglesSegments == null || _culledTrianglesSegments.Length != culledSegments.Length) {
                _culledTrianglesSegments = new int[culledSegments.Length];
            }
            if (_culledTrianglesSegments.Length > 0) {
                fixed (int* culledTrianglesSegmentsPtr = &_culledTrianglesSegments[0]) {
                    var culledSegmentsPtr = culledSegments.Ptr;
                    for (var i = 0; i < culledSegments.Length; i++) {
                        culledTrianglesSegmentsPtr[i] = culledSegmentsPtr[i];
                    }
                }
            }
            culledSegments.Dispose();

            // Painting
            if (_paintingMode == PaintingMode.Single) {
                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                var rayOrigin = (float3)ray.origin;
                var rayDirection = (float3)ray.direction;

                var selectedTriangles = new UnsafeBitmask(trisCount, ARAlloc.Temp);
                CullingUtilities.SinglePaint(cameraNormal, rayOrigin, rayDirection, ref selectedTriangles, indices, vertices, HideBackTrianglesFactor, false);
                var selected = selectedTriangles.FirstOne();
                if (selected != -1) {
                    _selectedTriangle = new HandlesUtils.HandlesTriangle(
                        vertices[indices[(uint)(selected * 3 + 0)]].position,
                        vertices[indices[(uint)(selected * 3 + 1)]].position,
                        vertices[indices[(uint)(selected * 3 + 2)]].position
                        );
                } else {
                    _selectedTriangle = Optional<HandlesUtils.HandlesTriangle>.None;
                }
                selectedTriangles.Dispose();
            } else if (_paintingMode == PaintingMode.Brush) {
                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                var rayOrigin = (float3)ray.origin;
                var rayDirection = (float3)ray.direction;

                var selectedTriangles = new UnsafeBitmask(trisCount, ARAlloc.Temp);
                CullingUtilities.BrushPaint(cameraNormal, rayOrigin, rayDirection, ref selectedTriangles, indices, vertices, HideBackTrianglesFactor, Radius, false);
                _brushTriangles = new HandlesUtils.HandlesTriangle[selectedTriangles.CountOnes()];
                var brushIndex = 0;
                foreach (var triangleIndex in selectedTriangles.EnumerateOnes()) {
                    _brushTriangles[brushIndex++] = new HandlesUtils.HandlesTriangle(
                        vertices[indices[triangleIndex * 3 + 0]].position,
                        vertices[indices[triangleIndex * 3 + 1]].position,
                        vertices[indices[triangleIndex * 3 + 2]].position
                        );
                }
                selectedTriangles.Dispose();
            }

            KandraRendererManager.Instance.StreamingManager.UnloadIndicesData(kandraMesh);
            vertices.Dispose();
            additionalData.Dispose();
            visibleTriangles.Dispose();
        }

        void DrawHandles() {
            var oldZTest = Handles.zTest;
            var oldColor = Handles.color;

            Handles.zTest = CompareFunction.Always;

            if (_paintingMode == PaintingMode.Single) {
                if (_selectedTriangle) {
                    Handles.color = _unculling ? UncullingColor : CullingColor;
                    HandlesUtils.DrawTriangle(_selectedTriangle.Value);
                }
            } else if (_paintingMode == PaintingMode.Brush) {
                if (_brushTriangles != null) {
                    Handles.color = _unculling ? UncullingColor : CullingColor;
                    foreach (var brushTriangle in _brushTriangles) {
                        HandlesUtils.DrawTriangle(brushTriangle);
                    }
                }
            }
            if (_showWireframe) {
                if (_visibleTrianglesSegments.Length > 0) {
                    Handles.color = VisibleTrianglesColor;
                    Handles.DrawLines(_wireframeVertices, _visibleTrianglesSegments);
                }

                if (_culledTrianglesSegments.Length > 0) {
                    Handles.color = CulledTrianglesColor;
                    Handles.DrawLines(_wireframeVertices, _culledTrianglesSegments);
                }
            }

            Handles.color = oldColor;
            Handles.zTest = oldZTest;
        }

        void DrawInfo(SceneView sceneView) {
            Handles.BeginGUI();

            var sb = new StringBuilder();
            if (_paintingMode == PaintingMode.None) {
                sb.Append("Preview mode");
                if (_isDirty) {
                    sb.Append(" <color=#fd3987ff>ctrl+s to save changes</color>");
                }
            } else {
                sb.Append("<color=");
                sb.Append(_unculling ? UncullingColor.ToHex() : CullingColor.ToHex());
                sb.Append(">");
                sb.Append(_unculling ? "Unculling" : "Culling");
                sb.Append("</color> ");
                sb.Append(_paintingMode.ToString());
                sb.Append(" mode. Radius: ");
                sb.Append(Radius.ToString("F3"));
                sb.Append(" ");
                if (_isDirty) {
                    sb.Append("<color=#fd3987ff><i>");
                }
                sb.Append(_cullers[0].name);
                if (_isDirty) {
                    sb.Append("* ctrl+s to save changes</i></color>");
                }
            }

            var labelText = sb.ToString();

            var labelContent = new GUIContent(labelText);
            var labelSize = LabelStyle.CalcSize(labelContent);
            var labelX = (sceneView.position.width - labelSize.x) / 2;
            var guiRect = new Rect(labelX, 5, labelSize.x, labelSize.y);
            GUI.Box(guiRect, GUIContent.none);
            GUI.Label(guiRect, labelContent, LabelStyle);

            Handles.EndGUI();
        }

        void DrawTips(SceneView sceneView) {
            if (!ShowTips) {
                return;
            }

            Handles.BeginGUI();

            var sb = new StringBuilder();
            sb.AppendLine("F1 - Single paint");
            sb.AppendLine("F2 - Brush paint");
            sb.AppendLine("F3 - Preview mode");

            if (_cullers.Count > 1) {
                sb.AppendLine("Cannot paint with multiple cullers");
            } else if (_cullers.Count == 0) {
                sb.AppendLine("Cannot paint without a cullers");
            }
            if (!_cullee) {
                sb.AppendLine("Cannot paint without a cullee");
            }

            if (_isDirty) {
                sb.AppendLine("<color=#fd3987ff><i>Ctrl+S - Save changes</i></color>");
            } else {
                sb.AppendLine("Ctrl+S - Save changes");
            }
            if (PressToUncull) {
                sb.AppendLine("Press Z - Toggle unculling");
            } else {
                sb.AppendLine("Hold Z - Toggle unculling");
            }
            sb.AppendLine("Shift+Scroll - Change brush radius");
            sb.AppendLine("> - Increase alpha");
            sb.AppendLine("< - Decrease alpha");

            var labelText = sb.ToString();

            var labelContent = new GUIContent(labelText);
            var labelSize = LabelStyle.CalcSize(labelContent);
            var labelY = (sceneView.position.height - labelSize.y) / 2;
            var guiRect = new Rect(5, labelY, labelSize.x, labelSize.y);
            GUI.Box(guiRect, GUIContent.none);
            GUI.Label(guiRect, labelContent, LabelStyle);

            Handles.EndGUI();
        }

        // === Helpers
        string PreferenceKey(string field) {
            return $"ClothesTestWindow_{field}";
        }

        Color GetColorFromPreferences(string key, Color defaultColor) {
            var color = EditorPrefs.GetString(key, defaultColor.ToHex());
            return ColorUtils.HexToColor(color);
        }

        void SetColorToPreferences(string key, Color color) {
            EditorPrefs.SetString(key, color.ToHex());
        }

        enum PaintingMode : byte {
            None,
            Single,
            Brush
        }

        struct ToSaveData {
            public string prefabPath;
            public string cullerName;
            public KandraTrisCuller.CulledMesh[] culledMeshes;
        }
    }
}
