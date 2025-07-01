using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    public class BakeKandraRendererToMeshWindow : OdinEditorWindow {
        [MenuItem("TG/Assets/Kandra/Bake to mesh")]
        static void ShowWindow() {
            var window = GetWindow<BakeKandraRendererToMeshWindow>();
            window.titleContent = new GUIContent("Kandra bake to mesh");
            window.Show();
        }

        public KandraRenderer kandraRenderer;

        [Button]
        public void Bake() {
            var bakedMesh = kandraRenderer.BakePoseMesh(kandraRenderer.rendererData.rig.transform.worldToLocalMatrix.Orthonormal());
            bakedMesh.UploadMeshData(true);
            // Ask for path
            var path = EditorUtility.SaveFilePanelInProject("Save mesh", "KandraMesh", "mesh", "Save mesh");
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            // Save mesh
            AssetDatabase.CreateAsset(bakedMesh, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}