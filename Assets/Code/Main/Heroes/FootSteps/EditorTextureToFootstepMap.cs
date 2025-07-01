using System;
using System.Collections.Generic;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.RichEnums;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.TG.Main.Heroes.FootSteps {
    public class EditorTextureToFootstepMap : ScriptableObject {
#if UNITY_EDITOR
        const string Guid = "acdf8e65f752004468acecf82096ba6b";
        public static EditorTextureToFootstepMap Get => AssetDatabase.LoadAssetAtPath<EditorTextureToFootstepMap>(AssetDatabase.GUIDToAssetPath(Guid));
        
        [InfoBox("Has duplicated Layers", InfoMessageType.Error, nameof(HasDuplicatedLayers))]
        [InfoBox("Has null Layer", InfoMessageType.Error, nameof(HasNullLayer))]
        [SerializeField] List<TerrainLayerToFootstep> mapping;

        public int FindFmodParameter(TerrainLayer layer) {
            var index = Array.IndexOf(SurfaceType.TerrainTypes, FindSurfaceType(layer));
            if (index == -1) {
                Debug.LogError($"Terrain layer {layer.name} is not mapped to any surface type!", layer);
            }
            return index;
        }

        public SurfaceType FindSurfaceType(TerrainLayer layer) {
            foreach (var map in mapping) {
                if (map.terrainLayer == layer) {
                    return map.SurfaceType;
                }
            }
            return SurfaceType.TerrainGrass;
        }

        bool HasDuplicatedLayers() {
            var layers = new HashSet<TerrainLayer>();
            foreach (var map in mapping) {
                if (map.terrainLayer == null) {
                    continue;
                }
                if (!layers.Add(map.terrainLayer)) {
                    return true;
                }
            }
            return false;
        }

        bool HasNullLayer() {
            foreach (var map in mapping) {
                if (map.terrainLayer == null) {
                    return true;
                }
            }
            return false;
        }
        
        [Serializable]
        struct TerrainLayerToFootstep {
            public TerrainLayer terrainLayer;
            [RichEnumExtends(typeof(SurfaceType)), SerializeField]
            RichEnumReference surfaceType;

            public SurfaceType SurfaceType => surfaceType.EnumAs<SurfaceType>();
        }
        
#endif
    }
    
}