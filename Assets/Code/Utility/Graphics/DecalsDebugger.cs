using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering.HighDefinition;
using Random = Unity.Mathematics.Random;

namespace Awaken.Utility.Graphics {
    public class DecalsDebugger : UGUIWindowDisplay<DecalsDebugger> {
        const int PageSize = 16;
        static readonly MaterialDecalsComparer Comparer = new MaterialDecalsComparer();

        DecalProjector[] _decalProjectors;
        Dictionary<Material, MaterialDecalsData> _decalProjectorsByMaterial;
        OnDemandCache<Material, bool> _materialDecalsEnabled = new OnDemandCache<Material, bool>((_) => true);
        HDRenderPipelineAsset _currentHDRenderPipelineAsset;
        Camera _mainCamera;

        float _randomSceneFillPercentage = 1f;
        float _decalsDrawDistance = 0f;
        int _page;

        protected override bool WithSearch => false;

        protected override void Initialize() {
            _decalProjectors = FindObjectsByType<DecalProjector>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            var pipelineAsset = (HDRenderPipelineAsset)QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel());
            var decalSettings = pipelineAsset.currentPlatformRenderPipelineSettings.decalSettings;
            _decalsDrawDistance = decalSettings.drawDistance;

            _decalProjectorsByMaterial = new Dictionary<Material, MaterialDecalsData>();
            foreach (var decalProjector in _decalProjectors) {
                if (!_decalProjectorsByMaterial.TryGetValue(decalProjector.material, out var decalsData)) {
                    decalsData = new MaterialDecalsData(decalProjector.material, new List<StaticDecalData>());
                    _decalProjectorsByMaterial[decalProjector.material] = decalsData;
                }

                decalsData.decals.Add(new StaticDecalData(decalProjector));
            }

            _mainCamera = Camera.main;
        }

        protected override void DrawWindow() {
            DrawDecalsCullers();

            _currentHDRenderPipelineAsset = (HDRenderPipelineAsset)QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel());
            if (_currentHDRenderPipelineAsset == null) {
                Log.Important?.Error("No HDRenderPipelineAsset found");
                return;
            }

            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            UpdateMaterialsDecalsDatas(_decalProjectorsByMaterial);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Rendered decals percent: {_randomSceneFillPercentage:P2}");
            using (var change = new TGGUILayout.CheckChangeScope()) {
                _randomSceneFillPercentage = GUILayout.HorizontalSlider(_randomSceneFillPercentage, 0f, 1f);
                if (change) {
                    var rng = new Random(69);
                    for (var i = 0; i < _decalProjectors.Length; i++) {
                        var projector = _decalProjectors[i];
                        if (projector == null) continue;
                        projector.enabled = rng.NextFloat() < _randomSceneFillPercentage;
                    }
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Decals draw distance: {_decalsDrawDistance:N0}");
            using (var change = new TGGUILayout.CheckChangeScope()) {
                _decalsDrawDistance = GUILayout.HorizontalSlider(_decalsDrawDistance, 0f, 100f);
                if (change) {
                    var renderPipelineSettings = _currentHDRenderPipelineAsset.currentPlatformRenderPipelineSettings;
                    var decalSettings = renderPipelineSettings.decalSettings;
                    decalSettings.drawDistance = (int)_decalsDrawDistance;
                    renderPipelineSettings.decalSettings = decalSettings;
                    _currentHDRenderPipelineAsset.currentPlatformRenderPipelineSettings = renderPipelineSettings;
                }
            }

            GUILayout.EndHorizontal();
            if (GUILayout.Button("Disable not rendered decals")) {
                DestroyOrDisableNotRenderedDecals(_decalProjectorsByMaterial, false);
            }

            if (GUILayout.Button("Destroy not rendered decals")) {
                DestroyOrDisableNotRenderedDecals(_decalProjectorsByMaterial, true);
            }

            if (GUILayout.Button("Enable disabled decals")) {
                EnableDisabledDecals();
            }

            ListPool<MaterialDecalsData>.Get(out var collectionElements);
            collectionElements.Clear();
            collectionElements.AddRange(_decalProjectorsByMaterial.Values);
            collectionElements.Sort(Comparer);
            _page = TGGUILayout.PagedList(collectionElements, DrawMaterialDecals, _page, PageSize);
            ListPool<MaterialDecalsData>.Release(collectionElements);
        }

        void DrawDecalsCullers() {
            GUILayout.Label("Decals controllers:");
            foreach (var culler in IDecalsCuller.DecalsCullers) {
                culler.enabled = GUILayout.Toggle(culler.enabled, culler.DescriptiveName);
            }
        }

        void UpdateMaterialsDecalsDatas(Dictionary<Material, MaterialDecalsData> decalProjectorsByMaterial) {
            var drawDistanceFromSettingsSq = _decalsDrawDistance * _decalsDrawDistance;

            float3 cameraPosition = _mainCamera.transform.position;
            ListPool<Material>.Get(out var keys);
            keys.Clear();
            foreach (var key in decalProjectorsByMaterial.Keys) {
                keys.Add(key);
            }

            foreach (var key in keys) {
                UpdateMaterialDecalsData(decalProjectorsByMaterial[key], cameraPosition, drawDistanceFromSettingsSq,
                    out var updatedValue);
                decalProjectorsByMaterial[key] = updatedValue;
            }
        }

        void UpdateMaterialDecalsData(MaterialDecalsData data,
            float3 cameraPosition, float drawDistanceFromSettingsSq, out MaterialDecalsData updatedMaterialDecalsData) {
            var decals = data.decals;
            int decalsCount = decals.Count;
            int inDrawDistanceCount = 0;
            int enabledCount = 0;
            for (int i = 0; i < decalsCount; i++) {
                var decalData = decals[i];
                if (decalData.projector == null) {
                    decalData.isInDrawDistance = false;
                    decalData.isProjectorEnabled = false;
                } else {
                    var decalDrawDistanceSq = math.min(drawDistanceFromSettingsSq, decalData.drawDistanceSq);
                    decalData.isInDrawDistance =
                        math.distancesq(decalData.position, cameraPosition) < decalDrawDistanceSq;
                    if (decalData.isInDrawDistance) {
                        inDrawDistanceCount++;
                    }
                    decalData.isProjectorEnabled = decalData.projector.enabled;
                    if (decalData.isProjectorEnabled) {
                        enabledCount++;
                    }
                }

                decals[i] = decalData;
            }

            data.inDrawDistanceDecalsCount = inDrawDistanceCount;
            data.enabledDecalsCount = enabledCount;
            updatedMaterialDecalsData = data;
        }

        void DestroyOrDisableNotRenderedDecals(Dictionary<Material, MaterialDecalsData> decalProjectorsByMaterial, bool destroy) {
            foreach (var decalsData in decalProjectorsByMaterial.Values) {
                foreach (var decal in decalsData.decals) {
                    if (decal.projector == null || (decal.isInDrawDistance && decal.isProjectorEnabled)) {
                        continue;
                    }

                    if (destroy)
                        Destroy(decal.projector);
                    else
                        decal.projector.enabled = false;
                }
            }
        }

        void EnableDisabledDecals() {
            for (var i = 0; i < _decalProjectors.Length; i++) {
                var projector = _decalProjectors[i];
                if (projector == null) continue;
                projector.enabled = true;
            }

            ListPool<Material>.Get(out var decalsMaterials);
            decalsMaterials.Clear();
            foreach (var decalsMaterial in _materialDecalsEnabled.Keys) {
                decalsMaterials.Add(decalsMaterial);
            }

            foreach (var decalsMaterial in decalsMaterials) {
                _materialDecalsEnabled[decalsMaterial] = true;
            }
        }

        void DrawMaterialDecals(int index, MaterialDecalsData materialDecals) {
            GUILayout.BeginHorizontal();
            var decals = materialDecals.decals;
            var decalsCount = decals.Count;
            var decalsInDrawDistanceCount = materialDecals.inDrawDistanceDecalsCount;
            var enabledDecalsCount = materialDecals.enabledDecalsCount;
            GUILayout.Label($"Material: {materialDecals.material?.name} Decals: {decalsCount}, Decals in draw distance: {decalsInDrawDistanceCount}, enabled : {enabledDecalsCount}");
            using (var change = new TGGUILayout.CheckChangeScope()) {
                var materialEnabled = GUILayout.Toggle(_materialDecalsEnabled[materialDecals.material], "Enabled");
                if (change) {
                    _materialDecalsEnabled[materialDecals.material] = materialEnabled;
                    foreach (var decal in decals) {
                        if (decal.projector == null) continue;
                        decal.projector.enabled = materialEnabled;
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowDecalsDebug() {
            DecalsDebugger.Toggle(UGUIWindowUtils.WindowPosition.TopLeft);
        }

        static bool IsDebugWindowShown() => DecalsDebugger.IsShown;

        struct StaticDecalData {
            public float3 position;
            public float drawDistanceSq;
            public bool isInDrawDistance;
            public bool isProjectorEnabled;
            public DecalProjector projector;

            public StaticDecalData(DecalProjector decalProjector) {
                position = decalProjector.transform.position;
                drawDistanceSq = decalProjector.drawDistance * decalProjector.drawDistance;
                isInDrawDistance = true;
                projector = decalProjector;
                isProjectorEnabled = decalProjector.enabled;
            }
        }

        struct MaterialDecalsData {
            public Material material;
            public List<StaticDecalData> decals;
            public int inDrawDistanceDecalsCount;
            public int enabledDecalsCount;
            public MaterialDecalsData(Material material, List<StaticDecalData> decals) : this() {
                this.material = material;
                this.decals = decals;
            }
        }

        class MaterialDecalsComparer : IComparer<MaterialDecalsData> {
            public int Compare(MaterialDecalsData x, MaterialDecalsData y) {
                return y.enabledDecalsCount.CompareTo(x.enabledDecalsCount);
            }
        }
    }
}