using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awaken.TG.Utility.Graphics;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics {
    public class ShaderReplacer : OdinEditorWindow {
        
        [MenuItem("TG/Assets/Replace shader")]
        static void OpenWindow() {
            var window = GetWindow<ShaderReplacer>();
            window.Show();
        }

        [AssetSelector][OnValueChanged(nameof(FilterMaterials))]
        public Shader oldShader;
        [AssetSelector, ShowIf("@newMaterial == null")] public Shader newShader;
        [AssetSelector, ShowIf("@newShader == null")] public Material newMaterial;
        [AssetSelector][OnValueChanged(nameof(FilterMaterials))]
        public List<Material> affectedMaterials = new List<Material>();

        public List<string> forcedProperties = new List<string>();

        public bool iterateAllMaterialsAfter;

        [Button][ShowIf(nameof(ButtonActive))]
        async void Replace() {
            if (newShader != null) {
                for (int i = affectedMaterials.Count-1; i >= 0; i--) {
                    affectedMaterials[i].shader = newShader;
                }
            } else {
                for (int i = affectedMaterials.Count-1; i >= 0; i--) {
                    var createdMaterial = MaterialUtils.CopyMaterial(affectedMaterials[i], newMaterial, forcedProperties);
                    EditorUtility.CopySerialized(createdMaterial, affectedMaterials[i]);
                }
            }

            if (iterateAllMaterialsAfter) {
                await SelectAllMaterials(affectedMaterials);
            }

            FilterMaterials();
            AssetDatabase.SaveAssets();
        }

        void FilterMaterials() {
            for (int i = affectedMaterials.Count-1; i >= 0; i--) {
                if (affectedMaterials[i].shader.name != oldShader.name) {
                    affectedMaterials.RemoveAt(i);
                }
            }
        }

        bool ButtonActive() {
            var hasTarget = (newShader != null && oldShader != newShader) || (newMaterial != null);
            return oldShader != null && hasTarget && (affectedMaterials?.Count ?? 0) > 0;
        }
        
        
        Queue<Material> _materials = new Queue<Material>();
        async Task SelectAllMaterials(ICollection<Material> materials) {
            _materials = new Queue<Material>(materials);
            EditorApplication.update += IterateAllMaterials;
            while (_materials.Any()) {
                await Task.Delay(50);
            }
        }

        void IterateAllMaterials() {
            if (!_materials.Any()) {
                EditorApplication.update -= IterateAllMaterials;
                return;
            }
            Material material = _materials.Dequeue();
            Selection.activeObject = material;
        }
    }
}