using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.FBX {
    public class MeshComparison : OdinEditorWindow {
        [SerializeField] Mesh[] meshes = Array.Empty<Mesh>();

        [MenuItem("TG/Assets/Mesh/Compare")]
        static void ShowEditor() {
            EditorWindow.GetWindow<MeshComparison>().Show();
        }

        /// <param name="vertices">each mesh vertices</param>
        /// <param name="indices">each mesh indices</param>
        /// <param name="meshCount">count of all meshes</param>
        /// <param name="vertexCount">min of vertex count of each mesh</param>
        /// <param name="indexCount">min of index count of each mesh</param>
        /// <returns>if all meshes has same number of vertices and indices</returns>
        bool Preprocess(out Vector3[][] vertices, out int[][] indices, out int meshCount, out int vertexCount, out int indexCount) {
            meshCount = meshes.Length;
            vertices = new Vector3[meshCount][];
            indices = new int[meshCount][];
            
            for (int i = 0; i < meshCount; i++) {
                vertices[i] = meshes[i].vertices;
                indices[i] = meshes[i].triangles;
            }
            
            vertexCount = vertices[0].Length;
            indexCount = indices[0].Length;

            bool result = true;
            
            for (int i = 1; i < meshCount; i++) {
                if (vertices[i].Length != vertexCount) {
                    Debug.LogError("Meshes has a different vertex count");
                    result = false;
                    vertexCount = Mathf.Min(vertexCount, vertices[i].Length);
                }
                if (indices[i].Length != indexCount) {
                    Debug.LogError("Meshes has a different triangle count");
                    result = false;
                    indexCount = Mathf.Min(indexCount, indices[i].Length);
                }
            }

            return result;
        }
        
        [Button]
        void CompareVertices() {
            if (!Preprocess(out var vertices, out _, out var meshCount, out var vertexCount, out _)) {
                return;
            }
            
            for (int i = 0; i < vertexCount; i++) {
                var vertex = vertices[0][i];
                for (int j = 1; j < meshCount; j++) {
                    if (vertices[j][i] != vertex) {
                        Debug.LogError($"Meshes has a different vertex at index {i}");
                        return;
                    }
                }
            }
                
            Debug.Log("Vertices are identical");
        }
        
        [Button]
        void CompareTriangles() {
            if (!Preprocess(out _, out var indices, out var meshCount, out _, out var indexCount)) {
                return;
            }

            for (int i = 0; i < indexCount; i++) {
                var index = indices[0][i];
                for (int j = 1; j < meshCount; j++) {
                    if (indices[j][i] != index) {
                        Debug.LogError($"Meshes has a different triangle at index {i}");
                        return;
                    }
                }
            }
            
            Debug.Log("Triangles are identical");
        }
    }
}