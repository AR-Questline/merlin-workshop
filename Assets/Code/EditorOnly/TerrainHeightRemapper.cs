using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.EditorOnly {
    public class TerrainHeightRemapper : MonoBehaviour {
        Terrain _terrain;

        Terrain Terrain => _terrain ??= GetComponent<Terrain>() ?? throw new Exception("Cannot find Terrain for TerrainRemapper");

        [ShowInInspector, HorizontalGroup("Current", order: -2), LabelWidth(50), LabelText("Low")] float CurrentLow => Terrain.transform.position.y;
        [ShowInInspector, HorizontalGroup("Current", order: -2), LabelWidth(50), LabelText("High")] float CurrentHigh => CurrentLow + Terrain.terrainData.size.y;

        [ShowInInspector, HorizontalGroup("New", order: -1), LabelWidth(50), LabelText("Low")] float _newLow;
        [ShowInInspector, HorizontalGroup("New", order: -1), LabelWidth(50), LabelText("High")] float _newHigh;
        
        #if UNITY_EDITOR
        [Button]
        void Remap() {
            if (_newLow == 0 && _newHigh == 0) {
                UnityEditor.EditorUtility.DisplayDialog(
                    "Terrain Height Remapper",
                    "New Low and new High are both unset. Abort remapping.",
                    "Ok"
                );
                return;
            }

            if (_newLow >= _newHigh) {
                UnityEditor.EditorUtility.DisplayDialog(
                    "Terrain Height Remapper",
                    "New Low is equal to or greater than new High. It will cause terrain to be flat. Abort remapping.",
                    "Ok"
                );
                return;
            }
            
            float currentLow = CurrentLow;
            float currentHigh = CurrentHigh;

            if (_newLow > currentLow) {
                bool proceed = UnityEditor.EditorUtility.DisplayDialog(
                    "Terrain Height Remapper",
                    "New Low is greater than old Low. It may cause data loss. Do you want to proceed?",
                    "Yas", "No"
                );
                if (!proceed) {
                    return;
                }
            }

            if (_newHigh < currentHigh) {
                bool proceed = UnityEditor.EditorUtility.DisplayDialog(
                    "Terrain Height Remapper",
                    "New High is lower than old High. It may cause data loss. Do you want to proceed?",
                    "Yas", "No"
                );
                if (!proceed) {
                    return;
                }
            }
            
            var data = Terrain.terrainData;
            float[,] heights = data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution);

            
            for (int i = 0; i < data.heightmapResolution; i++) {
                for (int j = 0; j < data.heightmapResolution; j++) {
                    float worldHeight = Mathf.LerpUnclamped(currentLow, currentHigh, heights[i, j]);
                    heights[i, j] = Mathf.InverseLerp(_newLow, _newHigh, worldHeight);
                }
            }

            data.size = new Vector3(data.size.x, _newHigh - _newLow, data.size.z);
            data.SetHeights(0, 0, heights);
            Terrain.transform.position = new Vector3(Terrain.transform.position.x, _newLow, Terrain.transform.position.z);
            
            UnityEditor.EditorUtility.SetDirty(Terrain);
            UnityEditor.EditorUtility.SetDirty(data);
        }
        #endif
    }
}