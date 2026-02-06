using System.Collections.Generic;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Editor.Terrains {
    public static class TerrainHolesDebugger {
        static readonly Dictionary<Terrain, Material> _originalMaterials = new();
        static readonly Dictionary<Terrain, Material> _debugMaterials = new();
        static Shader _debugShader;
        static bool _isDebugActive;

        const string DebugShaderName = "Hidden/TG/TerrainHolesDebug";
        const string MenuPath = "TG/Scene Tools/Terrain Holes/";

        static Shader DebugShader {
            get {
                if (_debugShader == null) {
                    _debugShader = Shader.Find(DebugShaderName);
                    if (_debugShader == null) {
                        Log.Critical?.Error($"Could not find shader: {DebugShaderName}");
                    }
                }
                return _debugShader;
            }
        }

        [MenuItem(MenuPath + "Toggle Holes Debug View %h", false, 1)] // Ctrl+H hotkey
        static void ToggleDebugView() {
            _isDebugActive = !_isDebugActive;

            if (_isDebugActive) {
                EnableDebugView();
            } else {
                DisableDebugView();
            }
        }

        [MenuItem(MenuPath + "Toggle Holes Debug View %h", true)]
        static bool ToggleDebugViewValidate() {
            Menu.SetChecked(MenuPath + "Toggle Holes Debug View", _isDebugActive);
            return true;
        }

        [MenuItem(MenuPath + "Enable Holes Debug View", false, 2)]
        static void EnableDebugView() {
            if (DebugShader == null) {
                return;
            }

            var terrains = Object.FindObjectsOfType<Terrain>();
            if (terrains.Length == 0) {
                Log.Important?.Warning("No terrains found in scene");
                return;
            }

            int count = 0;
            foreach (var terrain in terrains) {
                if (terrain.materialTemplate == null) {
                    continue;
                }

                // Store original material
                if (!_originalMaterials.ContainsKey(terrain)) {
                    _originalMaterials[terrain] = terrain.materialTemplate;
                }

                // Create or reuse debug material
                if (!_debugMaterials.TryGetValue(terrain, out var debugMaterial)) {
                    debugMaterial = new Material(DebugShader);
                    debugMaterial.name = $"TerrainHolesDebug_{terrain.name}";

                    // Copy holes texture from original material
                    if (terrain.materialTemplate.HasProperty("_TerrainHolesTexture")) {
                        var holesTexture = terrain.materialTemplate.GetTexture("_TerrainHolesTexture");
                        debugMaterial.SetTexture("_TerrainHolesTexture", holesTexture);
                    }

                    // Set default colors
                    debugMaterial.SetColor("_HoleColor", new Color(1, 0, 0, 1)); // Red for holes
                    debugMaterial.SetColor("_SolidColor", new Color(0, 1, 0, 0.3f)); // Green for solid
                    debugMaterial.SetFloat("_Opacity", 0.8f);

                    _debugMaterials[terrain] = debugMaterial;
                }

                terrain.materialTemplate = debugMaterial;
                count++;
            }

            _isDebugActive = true;
            Log.Important?.Info($"Terrain holes debug view enabled for {count} terrain(s)");
        }

        [MenuItem(MenuPath + "Disable Holes Debug View", false, 3)]
        static void DisableDebugView() {
            var terrains = Object.FindObjectsOfType<Terrain>();
            int count = 0;

            foreach (var terrain in terrains) {
                if (_originalMaterials.TryGetValue(terrain, out var originalMaterial)) {
                    terrain.materialTemplate = originalMaterial;
                    count++;
                }
            }

            _isDebugActive = false;

            if (count > 0) {
                Log.Important?.Info($"Terrain holes debug view disabled for {count} terrain(s)");
            }
        }

        [MenuItem(MenuPath + "Clear Debug Materials Cache", false, 20)]
        static void ClearCache() {
            foreach (var debugMaterial in _debugMaterials.Values) {
                if (debugMaterial != null) {
                    Object.DestroyImmediate(debugMaterial);
                }
            }

            _originalMaterials.Clear();
            _debugMaterials.Clear();
            _isDebugActive = false;

            Log.Important?.Info("Terrain holes debug cache cleared");
        }

        [InitializeOnLoadMethod]
        static void Initialize() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingEditMode && _isDebugActive) {
                DisableDebugView();
                Log.Important?.Warning("Terrain holes debug view disabled before entering Play Mode");
            }
        }
    }
}
