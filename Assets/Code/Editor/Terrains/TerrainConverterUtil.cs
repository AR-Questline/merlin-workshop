using Awaken.TG.Editor.Assets.FBX;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Editor.Terrains {
    public class TerrainConverterUtil : OdinEditorWindow {
        public Terrain terrain;
        public int vertexCount;
        
        [MenuItem("TG/Map/Terrain Converter")]
        public static void OpenWindow() {
            GetWindow<TerrainConverterUtil>().Show();
        }

        [Button]
        public void GenerateMesh() {
            var mesh = ToMesh(terrain.terrainData, (int)Mathf.Sqrt(vertexCount));
            mesh.name = "Mesh";
            GameObject go = new("Mesh");
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            FBXRenamer.ExportBinaryFBX("Assets/Mesh.fbx", go);
            Object.DestroyImmediate(go);
        }

        [Button]
        public void OpenMeshToTerrainTool() {
            EditorApplication.ExecuteMenuItem("Window/Infinity Code/Mesh to Terrain/Mesh to Terrain");
        }
        
        static Mesh ToMesh(TerrainData data, int density) {
            var verticesPerAxis = density + 1;
            float quadSize = data.size.x / density;
            float normalizedQuadSize = 1.0f / density;
    
            var vertices = new Vector3[(density + 1) * (density + 1)];
            var triangles = new int[density * density * 6];
            var heightmap = data.GetInterpolatedHeights(0, 0, verticesPerAxis, verticesPerAxis, normalizedQuadSize, normalizedQuadSize);
    
            int iv = 0;
            int it = 0;
            for (int x = 0; x <= density; x++) {
                for (int y = 0; y <= density; y++) {
                    vertices[iv] = new Vector3(x * quadSize, heightmap[y, x], y * quadSize);

                    if (x < density && y < density) {
                        int v0 = iv;
                        int v1 = iv + 1;
                        int v2 = iv + (verticesPerAxis);
                        int v3 = iv + (verticesPerAxis + 1);
                
                        triangles[it++] = v0;
                        triangles[it++] = v1;
                        triangles[it++] = v2;
                        triangles[it++] = v2;
                        triangles[it++] = v1;
                        triangles[it++] = v3;
                    }
                    iv++;
                }
            }

            return new Mesh {
                indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                vertices = vertices,
                triangles = triangles,
            };
        }
    }
}
