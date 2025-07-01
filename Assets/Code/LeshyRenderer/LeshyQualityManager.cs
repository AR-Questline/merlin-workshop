using System;
using System.Linq;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.LeshyRenderer {
    public struct LeshyQualityManager {
        const float MaxViewDistance = 3f;
        
        LeshyManager _manager;
        IEventListener _vegetationSettingChangedListener;
        IEventListener _distanceSettingChangedListener;

        VegetationSettings.ItemPreset<DensityValue> _currentDensity;
        VegetationSettings.ItemPreset<ShadowCastingMode> _currentShadows;
        VegetationSettings.ItemPreset<SpawnDistanceValue> _spawnDistance;
        VegetationSettings.ItemPreset<BillboardDistanceValue> _currentBillboardDistance;

        float _viewDistance;

        public void Init(LeshyManager manager, VegetationSettings settings) {
            int qualityIndex = 3;

            var leshyVegetationSetting = World.Any<Vegetation>();
            if (leshyVegetationSetting != null) {
                _vegetationSettingChangedListener = leshyVegetationSetting.ListenTo(Setting.Events.SettingRefresh, manager.QualityChanged);
                qualityIndex = leshyVegetationSetting.QualityIndex;
            }

            var distanceSetting = World.Any<DistanceCullingSetting>();
            if (distanceSetting != null) {
                _distanceSettingChangedListener = distanceSetting.ListenTo(Setting.Events.SettingRefresh, manager.QualityChanged);
                _viewDistance = math.clamp(distanceSetting.BiasValue, 0f, MaxViewDistance);
            } else {
                _viewDistance = 1f;
            }
            
            var quality = qualityIndex switch {
                0 => PresetQuality.Low,
                1 => PresetQuality.Medium,
                2 => PresetQuality.High,
                3 => PresetQuality.Ultra,
                _ => throw new ArgumentOutOfRangeException(),
            };
            
            InitWithQuality(manager, settings, quality);
        }

        public void InitWithQuality(LeshyManager manager, VegetationSettings vegetationSettings, PresetQuality quality) {
            _manager = manager;
            
            _currentDensity = vegetationSettings.densities.CurrentValue(quality);
            _currentShadows = vegetationSettings.shadows.CurrentValue(quality);
            _spawnDistance = vegetationSettings.spawnDistances;
            _currentBillboardDistance = vegetationSettings.billboardDistances.CurrentValue(quality);
        }

        public void Dispose() {
            if (_vegetationSettingChangedListener != null) {
                World.EventSystem?.RemoveListener(_vegetationSettingChangedListener);
            }
            if (_distanceSettingChangedListener != null) {
                World.EventSystem?.RemoveListener(_distanceSettingChangedListener);
            }
        }

        public readonly float Density(LeshyPrefabs.PrefabType type) {
            return type switch {
                LeshyPrefabs.PrefabType.Grass       => _currentDensity.grass,
                LeshyPrefabs.PrefabType.Plant       => _currentDensity.plant,
                LeshyPrefabs.PrefabType.Object      => _currentDensity.@object,
                LeshyPrefabs.PrefabType.LargeObject => _currentDensity.largeObject,
                LeshyPrefabs.PrefabType.Tree        => _currentDensity.tree,
                LeshyPrefabs.PrefabType.Ivy         => _currentDensity.ivy,
                _                                   => 1f,
            };
        }

        public readonly float SpawnDistance(LeshyPrefabs.PrefabType type) {
            return type switch {
                LeshyPrefabs.PrefabType.Grass       => _spawnDistance.grass * _viewDistance,
                LeshyPrefabs.PrefabType.Plant       => _spawnDistance.plant * _viewDistance,
                LeshyPrefabs.PrefabType.Object      => _spawnDistance.@object * _viewDistance,
                LeshyPrefabs.PrefabType.LargeObject => _spawnDistance.largeObject * _viewDistance,
                LeshyPrefabs.PrefabType.Tree        => _spawnDistance.tree * _viewDistance,
                LeshyPrefabs.PrefabType.Ivy         => _spawnDistance.ivy * _viewDistance,
                _                                   => 0f,
            };
        }

        public readonly ShadowCastingMode Shadows(LeshyPrefabs.PrefabType type) {
            return type switch {
                LeshyPrefabs.PrefabType.Grass       => _currentShadows.grass,
                LeshyPrefabs.PrefabType.Plant       => _currentShadows.plant,
                LeshyPrefabs.PrefabType.Object      => _currentShadows.@object,
                LeshyPrefabs.PrefabType.LargeObject => _currentShadows.largeObject,
                LeshyPrefabs.PrefabType.Tree        => _currentShadows.tree,
                LeshyPrefabs.PrefabType.Ivy         => _currentShadows.ivy,
                _                                   => ShadowCastingMode.Off,
            };
        }

        public readonly float BillboardDistance(LeshyPrefabs.PrefabType type) {
            return type switch {
                LeshyPrefabs.PrefabType.Grass       => _currentBillboardDistance.grass,
                LeshyPrefabs.PrefabType.Plant       => _currentBillboardDistance.plant,
                LeshyPrefabs.PrefabType.Object      => _currentBillboardDistance.@object,
                LeshyPrefabs.PrefabType.LargeObject => _currentBillboardDistance.largeObject,
                LeshyPrefabs.PrefabType.Tree        => _currentBillboardDistance.tree,
                LeshyPrefabs.PrefabType.Ivy         => _currentBillboardDistance.ivy,
                _                                   => 0f,
            };
        }

        public void QualityChanged() {
            _manager.enabled = false;
            _manager.enabled = true;
        }

        [Serializable, InlineProperty]
        public struct DensityValue {
            [Range(0.01f, 1f), HideLabel] public float value;

            public static implicit operator float(DensityValue value) => value.value;
        }

        [Serializable, InlineProperty]
        public struct SpawnDistanceValue {
            [Range(1f, 800f), HideLabel] public float value;

            public static implicit operator float(SpawnDistanceValue value) => value.value;
        }

        [Serializable, InlineProperty]
        public struct BillboardDistanceValue {
            [Range(0f, 5000f), HideLabel] public float value;

            public static implicit operator float(BillboardDistanceValue value) => value.value;
        }

        public enum PresetQuality : byte {
            Low,
            Medium,
            High,
            Ultra,
        }
    }
}
