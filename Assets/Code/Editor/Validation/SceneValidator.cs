using System.Text;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.AdditiveScenes;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Validation {
    public class SceneValidator {
        readonly bool _displayDialogue;
        
        public SceneValidator(in Config config) {
            _displayDialogue = config.displayDialogue;
        }
        
        public void RegisterCallbacks() {
            EditorSceneManager.sceneSaved += ValidateScene;
        }

        public void UnregisterCallbacks() {
            EditorSceneManager.sceneSaved -= ValidateScene;
        }

        public void ValidateScene(Scene scene) {
            bool noErrors = true;
            StringBuilder sb = null;

            int terrainBakerCount = 0;
            int groundBoundsCount = 0;
            bool isMapScene = false;
            bool isAdditiveScene = false;
            
            foreach (var root in scene.GetRootGameObjects()) {
                foreach (var terrain in root.GetComponentsInChildren<Terrain>()) {
                    var baker = terrain.GetComponentInParent<TerrainGroundBoundsBaker>();
                    if (baker is null) {
                        Error($"Terrain '{terrain.name}' is missing GroundBoundsBaker parent", terrain);
                    }
                }
                foreach (var terrainBaker in root.GetComponentsInChildren<TerrainGroundBoundsBaker>()) {
                    ++terrainBakerCount;
                    if (terrainBakerCount > 1) {
                        Error("Multiple TerrainGroundBoundsBakers found in scene", terrainBaker);
                    }
                }

                if (root.GetComponentInChildren<MapScene>()) {
                    isMapScene = true;
                }

                if (root.GetComponentInChildren<AdditiveScene>()) {
                    isAdditiveScene = true;
                }

                var groundBounds = root.GetComponentInChildren<GroundBounds>();
                if (groundBounds != null) {
                    ++groundBoundsCount;
                    var accessor = new GroundBounds.EditorAccess(groundBounds);
                    if (accessor.GameBoundsPolygon == null) {
                        Error("GroundBounds is missing GameBoundsPolygon", groundBounds);
                    }
                    if (accessor.VegetationBoundsPolygon == null) {
                        Error("GroundBounds is missing VegetationBoundsPolygon", groundBounds);
                    }
                    if (accessor.TerrainForegroundBoundsPolygon == null) {
                        Error("GroundBounds is missing TerrainForegroundBoundsPolygon", groundBounds);
                    }
                    if (accessor.TerrainBackgroundBoundsPolygon == null) {
                        Error("GroundBounds is missing TerrainBackgroundBoundsPolygon", groundBounds);
                    }
                    if (accessor.MedusaBoundsPolygon == null) {
                        Error("GroundBounds is missing MedusaBoundsPolygon", groundBounds);
                    }
                }
            }

            if (isMapScene || isAdditiveScene) {
                if (groundBoundsCount == 0) {
                    Error("No GroundBounds found in scene", null);
                } else if (groundBoundsCount > 1) {
                    Error("Multiple GroundBounds found in scene", null);
                }
            } else {
                if (groundBoundsCount > 0) {
                    Error("GroundBounds found in non MapScene", null);
                }
            }

            if (_displayDialogue && !noErrors) {
                sb.Append("(errors are printed to the console)");
                EditorUtility.DisplayDialog($"{scene.name} Validation Failed!", sb.ToString(), "I'll fix it!");
            }
            void Error(string error, Object context) {
                if (noErrors) {
                    noErrors = false;
                    Log.Important?.Error($"=== Scene {scene.name} Validation Below ===", AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path));
                }
                if (_displayDialogue) {
                    sb ??= new StringBuilder("Please fix the following errors:\n");
                    sb.Append("- ");
                    sb.AppendLine(error);
                }
                Log.Important?.Error(error, context);
            }
        }

        [MenuItem("TG/Scene Tools/Validate All Scenes")]
        static void ValidateAllScenes() {
            var validator = new SceneValidator(new Config() {
                displayDialogue = false,
            });
            foreach (var path in BuildTools.GetAllScenes()) {
                var scene = EditorSceneManager.OpenScene(path);
                validator.ValidateScene(scene);
            }
        }

        public struct Config {
            public bool displayDialogue;
        }
    }
}