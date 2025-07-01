using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.LeshyRenderer {
    [CreateAssetMenu(menuName = "TG/Leshy/Vegetation settings", fileName = "VegetationSettings")]
    public class VegetationSettings : ScriptableObject {
        [SerializeField, ListDrawerSettings(IsReadOnly = true)]
        public QualityPresets<LeshyQualityManager.DensityValue> densities;

        [SerializeField, ListDrawerSettings(IsReadOnly = true)]
        public QualityPresets<ShadowCastingMode> shadows;

        public ItemPreset<LeshyQualityManager.SpawnDistanceValue> spawnDistances;

        [SerializeField, ListDrawerSettings(IsReadOnly = true)]
        public QualityPresets<LeshyQualityManager.BillboardDistanceValue> billboardDistances;

        [Serializable]
        public struct QualityPresets<T> {
            public ItemPreset<T> low;
            public ItemPreset<T> medium;
            public ItemPreset<T> high;
            public ItemPreset<T> ultra;

            public readonly ItemPreset<T> CurrentValue(LeshyQualityManager.PresetQuality quality) {
                return quality switch {
                    LeshyQualityManager.PresetQuality.Low    => low,
                    LeshyQualityManager.PresetQuality.Medium => medium,
                    LeshyQualityManager.PresetQuality.High   => high,
                    LeshyQualityManager.PresetQuality.Ultra  => ultra,
                    _                    => low,
                };
            }
        }

        [Serializable]
        public struct ItemPreset<T> {
            public T grass;
            public T plant;
            public T @object;
            public T largeObject;
            public T tree;
            public T ivy;
        }
    }
}
