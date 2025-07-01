using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics {
    public class EmptyMeshCollidersDetector : OdinEditorWindow {
        [MenuItem("TG/Assets/Search/Find Empty Mesh Colliders")]
        internal static void ShowEditor() {
            var window = EditorWindow.GetWindow<EmptyMeshCollidersDetector>();
            window.Show();
        }
        
        [ShowInInspector]
        public List<MeshCollider> emptyMeshColliders = new();

        [Button]
        void Search() {
            emptyMeshColliders = FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(mc => mc.sharedMesh == null).ToList();
        }
    }
}