using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Assets {
    public class ShadersUsageWindow : OdinEditorWindow {
        [MenuItem("TG/Assets/Shaders usage")]
        static void OpenWindow() {
            var window = GetWindow<ShadersUsageWindow>();
            window.Show();
        }

        protected override void OnEnable() {
            base.OnEnable();
            
            _allShaders = AssetDatabase.FindAssets("t:Shader").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Shader>).ToList();
            _allMaterials = AssetDatabase.FindAssets("t:Material").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).ToList();
            
            FilterChanged();
        }

        [ShowInInspector]
        ViewType _viewType = ViewType.AllShader;

        [ShowInInspector, OnValueChanged(nameof(FilterChanged)), Delayed]
        string _shaderFilter = "";
        [ShowInInspector, OnValueChanged(nameof(FilterChanged)), Delayed]
        string _materialFilter = "";
        
        [ListDrawerSettings(NumberOfItemsPerPage = 25, DraggableItems = false, HideAddButton = true, HideRemoveButton = true, ShowFoldout = false)]
        [DictionaryDrawerSettings(KeyLabel = "Shader", ValueLabel = "Materials", IsReadOnly = true)]
        [ShowIf("@_viewType==ViewType.AllShader"), ShowInInspector]
        Dictionary<Shader, List<Material>> _allShadersAndMaterials = new Dictionary<Shader, List<Material>>();
        
        [ListDrawerSettings(NumberOfItemsPerPage = 25, DraggableItems = false, HideAddButton = true, HideRemoveButton = true, ShowFoldout = false)]
        [DictionaryDrawerSettings(KeyLabel = "Shader", ValueLabel = "Materials", IsReadOnly = true)]
        [ShowIf("@_viewType==ViewType.WithMaterial"), ShowInInspector]
        Dictionary<Shader, List<Material>> _materialsByShaders = new Dictionary<Shader, List<Material>>();
        
        [ListDrawerSettings(NumberOfItemsPerPage = 25, DraggableItems = false, HideAddButton = true, HideRemoveButton = true, ShowFoldout = false)]
        [ShowIf("@_viewType==ViewType.UnusedShaders"), ShowInInspector]
        List<Shader> _unusedShaders = new List<Shader>();
        
        [ListDrawerSettings(NumberOfItemsPerPage = 25, DraggableItems = false, HideAddButton = true, HideRemoveButton = true, ShowFoldout = false)]
        [ShowIf("@_viewType==ViewType.InvalidShaders"), ShowInInspector]
        List<Shader> _invalidShaders = new List<Shader>();

        List<Shader> _allShaders;
        List<Material> _allMaterials;

        void FilterChanged() {
            IEnumerable<Material> filteredMaterials;
            if (string.IsNullOrWhiteSpace(_materialFilter)) {
                filteredMaterials = _allMaterials;
            } else {
                filteredMaterials = _allMaterials.Where(m => m.name.IndexOf(_materialFilter, StringComparison.InvariantCultureIgnoreCase) >= 0);
            }
            
            IEnumerable<Shader> filteredShaders;
            if (string.IsNullOrWhiteSpace(_shaderFilter)) {
                filteredShaders = _allShaders;
            } else {
                filteredShaders = _allShaders.Where(s => s.name.IndexOf(_shaderFilter, StringComparison.InvariantCultureIgnoreCase) >= 0);
            }
            
            _materialsByShaders.Clear();
            _allShadersAndMaterials.Clear();
            _unusedShaders.Clear();
            _invalidShaders.Clear();
            
            foreach (Material material in filteredMaterials) {
                if (material.shader == null) {
                    Log.Important?.Error($"Material {material} has no shader");
                    continue;
                }
                
                if (!string.IsNullOrWhiteSpace(_shaderFilter) && material.shader.name.IndexOf(_shaderFilter, StringComparison.InvariantCultureIgnoreCase) == -1) {
                    continue;
                }

                if (!_materialsByShaders.TryGetValue(material.shader, out var materials)) {
                    materials = new List<Material>();
                    _materialsByShaders[material.shader] = materials;
                }
                materials.Add(material);
            }

            _unusedShaders = filteredShaders.Except(_materialsByShaders.Keys).OrderBy(s => s.name).ToList();
            
            _allShadersAndMaterials = new Dictionary<Shader, List<Material>>(_materialsByShaders);
            foreach (Shader unusedShader in _unusedShaders) {
                _allShadersAndMaterials.Add(unusedShader, new List<Material>());
            }

            _invalidShaders = filteredShaders.Where(ShaderUtil.ShaderHasError).OrderBy(s => s.name).ToList();

            // Just for compiler
            if (_viewType == ViewType.AllShader) { }
        }
        
        enum ViewType {
            AllShader,
            WithMaterial,
            UnusedShaders,
            InvalidShaders,
        }
    }
}