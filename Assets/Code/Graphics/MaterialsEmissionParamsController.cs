using System;
using System.Collections.Generic;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Awaken.TG.Graphics {
    [ExecuteAlways]
    public class MaterialsEmissionParamsController : MonoBehaviour {
        [SerializeField] MaterialToSettingsDictionary materialsWithSetting = new();
        
        Dictionary<string, int> _runtimeKeyToLoadedMaterialIndexMap;
        Material[] _loadedMaterials;
        MaterialEmissionDayTimeSettings[] _materialsSettings;
#if UNITY_EDITOR
        [SerializeField] bool previewChangesAndValidate;
        [SerializeField, Range(0f, 1f)] float testDayTimeValue;

        [ShowIf(nameof(IsPlaymode))] bool _forceSetFromTestDateTime;
        bool IsPlaymode => Application.isPlaying;
#endif

        void Awake() {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                return;
            }

            _forceSetFromTestDateTime = false;
#endif
            Initialize();
            LoadedMaterialsTracker.Instance.OnNewMaterialLoaded += OnNewMaterialLoaded;
            LoadedMaterialsTracker.Instance.OnMaterialUnloaded += OnMaterialUnloaded;
        }

        void Update() {
#if UNITY_EDITOR
            if (Application.isPlaying == false || _forceSetFromTestDateTime) {
                if (previewChangesAndValidate) {
                    EditorApplySettings();
                }
                return;
            }
#endif
            UpdateMaterials();
        }

        void Initialize() {
            var materialsToChangeCount = materialsWithSetting.Count;
            _runtimeKeyToLoadedMaterialIndexMap = new Dictionary<string, int>(materialsToChangeCount);
            _materialsSettings = new MaterialEmissionDayTimeSettings[materialsToChangeCount];
            _loadedMaterials = new Material[materialsToChangeCount];
            int i = 0;
            foreach (var (materialRef, settings) in materialsWithSetting) {
                _materialsSettings[i] = settings;
                _runtimeKeyToLoadedMaterialIndexMap.Add((string)materialRef.RuntimeKey, i);
                i++;
            }
#if !UNITY_EDITOR
            materialsWithSetting.Clear();
            materialsWithSetting = null;
#endif
            var loadedMaterialsMap = LoadedMaterialsTracker.Instance.MaterialKeyToLoadedMaterialMap;
            foreach (var (runtimeKey, material) in loadedMaterialsMap) {
                if (_runtimeKeyToLoadedMaterialIndexMap.TryGetValue(runtimeKey, out var loadedMaterialIndex) == false) {
                    continue;
                }

                _loadedMaterials[loadedMaterialIndex] = material;
            }
        }

        void UpdateMaterials() {
            var normalizedTimeOfDay = GetNormalizedTimeOfDay();
            int materialsCount = _loadedMaterials.Length;
            for (int i = 0; i < materialsCount; i++) {
                var material = _loadedMaterials[i];
                if (ReferenceEquals(material, null)) {
                    continue;
                }

                try {
                    _materialsSettings[i].ApplyToMaterial(in material, normalizedTimeOfDay);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        void OnNewMaterialLoaded(string runtimeKey, Material material) {
            if (_runtimeKeyToLoadedMaterialIndexMap.TryGetValue(runtimeKey, out var loadedMaterialIndex) == false) {
                return;
            }

            _loadedMaterials[loadedMaterialIndex] = material;
        }

        void OnMaterialUnloaded(string runtimeKey) {
            if (_runtimeKeyToLoadedMaterialIndexMap.TryGetValue(runtimeKey, out var loadedMaterialIndex) == false) {
                return;
            }

            _loadedMaterials[loadedMaterialIndex] = null;
        }

        static float GetNormalizedTimeOfDay() {
            var gameRealTime = World.Any<GameRealTime>();
            if (gameRealTime == null) {
                return 0;
            }

            return gameRealTime.WeatherTime.DayTime;
        }

#if UNITY_EDITOR
        void OnValidate() {
            if (previewChangesAndValidate == false) {
                return;
            }
            ValidateCurvesContinuity();
            EditorApplySettings();
        }

        void EditorApplySettings() {
            foreach (var (materialRef, settings) in materialsWithSetting) {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(materialRef.AssetGUID);
                var materialAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (materialAsset == null) {
                    continue;
                }

                settings.ApplyToMaterial(materialAsset, testDayTimeValue);
            }
        }

        void ValidateCurvesContinuity() {
            foreach (var (materialRef, settings) in materialsWithSetting) {
                const int NameInsertIndex = 18;
                const int CurveNameInsertIndex = 16;
                const string ErrorMessage =
                    "In Settings for   curve for values at 0 and 1 are not the same so there will be visible jump in intensity at midnight. Setting value at 1 to value at 0";
                if (settings.intensityByDayTime.Evaluate(0) != settings.intensityByDayTime.Evaluate(1)) {
                    Log.Minor?.Error(ErrorMessage.Insert(NameInsertIndex, "intensity").Insert(CurveNameInsertIndex,
                        materialRef?.editorAsset?.name ?? string.Empty));
                    SetCurveValueAt(settings.intensityByDayTime, 1, settings.intensityByDayTime.Evaluate(0));
                }

                if (settings.exposureWeightByDayTime.Evaluate(0) != settings.exposureWeightByDayTime.Evaluate(1)) {
                    Log.Minor?.Error(ErrorMessage.Insert(NameInsertIndex, "exposure weight")
                        .Insert(CurveNameInsertIndex, materialRef?.editorAsset?.name ?? string.Empty));
                    SetCurveValueAt(settings.exposureWeightByDayTime, 1, settings.exposureWeightByDayTime.Evaluate(0));
                }
            }
        }

        static void SetCurveValueAt(AnimationCurve curve, float curveTime, float curveValue) {
            var keys = curve.keys;
            int keyAtTimeIndex = -1;
            for (int i = 0; i < keys.Length; i++) {
                if (keys[i].time == curveTime) {
                    keyAtTimeIndex = i;
                    break;
                }
            }

            if (keyAtTimeIndex != -1) {
                curve.RemoveKey(keyAtTimeIndex);
            }

            curve.AddKey(curveTime, curveValue);
        }
#endif

        void OnDestroy() {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                return;
            }
#endif
            LoadedMaterialsTracker.Instance.OnNewMaterialLoaded -= OnNewMaterialLoaded;
            LoadedMaterialsTracker.Instance.OnMaterialUnloaded -= OnMaterialUnloaded;
        }

        [Serializable]
        class MaterialToSettingsDictionary : SerializedDictionary<AssetReferenceT<Material>,
            MaterialEmissionDayTimeSettings> { }
    }
}