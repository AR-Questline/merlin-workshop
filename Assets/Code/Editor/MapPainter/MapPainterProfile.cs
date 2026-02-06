using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Editor.MapPainter {
    [CreateAssetMenu(fileName = "MapPainterProfile", menuName = "Map Painter/Painter Profile", order = 1)]
    public class MapPainterProfile : ScriptableObject {
        [Header("Prefab Configuration")]
        public List<GameObject> prefabs = new List<GameObject>();
        public List<PrefabSettings> prefabSettings = new List<PrefabSettings>();
        public int selectedPrefabIndex = 0;
        
        [Header("Brush Settings")]
        public float brushSize = 5f;
        public int maxDensity = 20;
        public float spawnRate = 0.3f;
        public float minSpawnDistance = 0.5f;
        public float trimRate = 0.3f;
        public bool randomRotation = true;
        public bool randomScale = false;
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        public MapPainterUtility.DistributionPattern distributionPattern = MapPainterUtility.DistributionPattern.Random;
        
        [Header("Filtering")]
        public bool useSlopeFilter = false;
        public Vector2 slopeRange = new Vector2(0f, 30f);
        public bool useHeightFilter = false;
        public Vector2 heightRange = new Vector2(0f, 100f);
        
        [Header("Organization")]
        public bool useParentGroups = true;
        public string parentGroupName = "Painted Objects";
        public bool useManualGroups = true;
        public bool showAllGroups = false;
        public List<GameObject> manualGroups = new List<GameObject>();
        public int selectedGroupIndex = 0;
        
        [Header("UI State")]
        public bool showAdvancedSettings = false;

        public bool canPaintByDrag;

        /// <summary>
        /// Ensures the settings list matches the prefab list count
        /// </summary>
        public void ValidateSettings() {
            for (int i = prefabs.Count - 1; i >= 0; i--) {
                if (prefabs[i] == null) {
                    prefabs.RemoveAt(i);
                    if (i < prefabSettings.Count) {
                        prefabSettings.RemoveAt(i);
                    }
                }
            }
            
            for (int i = manualGroups.Count - 1; i >= 0; i--) {
                if (manualGroups[i] == null) {
                    manualGroups.RemoveAt(i);
                }
            }
            
            // Clamp indices
            selectedPrefabIndex = math.min(selectedPrefabIndex, prefabs.Count - 1);
            selectedGroupIndex = Mathf.Clamp(selectedGroupIndex, 0, Mathf.Max(0, manualGroups.Count - 1));
        }
        
        /// <summary>
        /// Resets all settings to defaults
        /// </summary>
        public void ResetToDefaults() {
            prefabs.Clear();
            prefabSettings.Clear();
            selectedPrefabIndex = 0;
            
            brushSize = 5f;
            maxDensity = 20;
            spawnRate = 0.3f;
            minSpawnDistance = 0.5f;
            trimRate = 0.3f;
            randomRotation = true;
            randomScale = false;
            scaleRange = new Vector2(0.8f, 1.2f);
            distributionPattern = MapPainterUtility.DistributionPattern.Random;
            
            useSlopeFilter = false;
            slopeRange = new Vector2(0f, 30f);
            useHeightFilter = false;
            heightRange = new Vector2(0f, 100f);
            
            useParentGroups = true;
            parentGroupName = "Painted Objects";
            useManualGroups = true;
            showAllGroups = false;
            manualGroups.Clear();
            selectedGroupIndex = 0;
            
            showAdvancedSettings = false;
            canPaintByDrag = false;
        }
        
        private void OnValidate() {
            ValidateSettings();
        }
    }
}