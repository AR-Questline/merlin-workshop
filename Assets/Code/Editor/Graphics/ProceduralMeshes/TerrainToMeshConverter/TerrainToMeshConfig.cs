using UnityEngine;

namespace Awaken.TG.Editor.Graphics.ProceduralMeshes.TerrainToMeshConverter {
    [CreateAssetMenu(fileName = "TerrainToMeshConfig", menuName = "TG/Terrain/TerrainToMesh Config")]
    public class TerrainToMeshConfig : ScriptableObject {
        public TerrainToMesh.Config data;
    }
}