using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics.ProceduralMeshes.TerrainToMeshConverter {
    public class TerrainToMeshWindow : OdinEditorWindow {
        [SerializeField] TerrainToMeshConfig config;
        [SerializeField] Terrain terrain;
        
        [MenuItem("TG/Assets/Mesh/Terrain To Mesh")]
        static void OpenWindow() {
            GetWindow<TerrainToMeshWindow>().Show();
        }

        [Button]
        void Convert() {
            var toCreate = new List<TerrainToMesh.AssetToCreate>();
            TerrainToMesh.Convert(toCreate, terrain, config.data);
            TerrainToMesh.AssetToCreate.Create(toCreate);
        }
    }
}