using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.AdditiveScenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Graphics.Scene {
    public class SceneConfigs : ScriptableObject {
        public static bool DisableSceneRefresh { get; set; }
        
        [SerializeField, TableList(AlwaysExpanded = true, IsReadOnly = true, CellPadding = 5), Searchable(FuzzySearch = false, FilterOptions = SearchFilterOptions.ValueToString)]
        List<SceneConfig> configs;

        public IReadOnlyList<SceneConfig> AllScenes => configs;
        public IEnumerable<string> ScenesToBake => configs.Where(c => c.bake).Select(c => c.sceneName);
        string SceneName => World.Services?.TryGet<SceneService>()?.ActiveSceneRef.Name ?? SceneManager.GetActiveScene().name;
        SceneConfig CurrentConfig() {
            var config = configs.FirstOrDefault(c => c.sceneName == SceneName);
            if (config == null) {
                Log.Important?.Warning($"{SceneName} scene config not found!");
            }
            return config;
        }

        [Button, DisableInEditorMode] public bool Apv() => CurrentConfig().APV;
        public bool HasApvEnabled(SceneReference sceneRef) {
            var config = configs.FirstOrDefault(c => c.sceneName == sceneRef.Name);
            if (config == null) {
                Log.Important?.Error($"{sceneRef.Name} scene config not found!");
                return false;
            }
            
            return config.APV;
        }
        
        [Button, DisableInEditorMode] public bool ShouldBake(bool reset = true) {
            SceneConfig currentConfig = CurrentConfig();
            var result = currentConfig?.bake ?? false;
#if UNITY_EDITOR
            if (reset && result) {
                currentConfig.bake = false;
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
            return result;
        }

        public bool IsAdditive() => CurrentConfig().additive;
        public bool IsAdditive(SceneReference sceneRef) => configs.FirstOrDefault(c => c.sceneName == sceneRef.Name)?.additive ?? false;
        

        /// <summary>
        /// In runtime, this should be accessed through SceneService as the value is cached there
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public bool IsOpenWorld_EDITOR() => CurrentConfig().openWorld;
        /// <summary>
        /// Should usually be accessed through SceneService as the value is cached there
        /// </summary>
        public bool IsOpenWorld(SceneReference sceneRef) => configs.FirstOrDefault(c => c.sceneName == sceneRef.Name)?.openWorld ?? false;
        public bool IsPrologue(SceneReference sceneRef) => configs.FirstOrDefault(c => c.sceneName == sceneRef.Name)?.prologue ?? false;
        
#if UNITY_EDITOR
        [Button, PropertyOrder(-1)]
        public void UpdateSceneList() {
            string[] paths = Directory.GetFiles(Application.dataPath + "\\Scenes", "*.unity", SearchOption.AllDirectories);
            var newConfigs = new List<SceneConfig>(paths.Length);

            foreach (string path in paths) {
                // Disassemble path
                var sanitized = path.Replace('\\', '/');
                string cleanDirAndFile = sanitized.Replace(".unity", "").Replace(Application.dataPath + "/", "");
                string fileName = Path.GetFileNameWithoutExtension(sanitized);
                string directory = cleanDirAndFile.Replace(fileName, "");
                
                var assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID("Assets/" + cleanDirAndFile + ".unity");
                
                // Add new or transfer sceneConfig to final list
                var existingConfig = configs.FirstOrDefault(c => c.GUID == assetGuid);
                
                if (existingConfig == null) {
                    var newConfig = new SceneConfig() {
                        sceneName = fileName,
                        directory = directory,
                        GUID = assetGuid
                    };

                    newConfigs.Add(newConfig);
                } else {
                    // Make sure any change to folder and file name is reflected in new configs
                    existingConfig.directory = directory;
                    existingConfig.sceneName = fileName;
                    newConfigs.Add(existingConfig);
                }
            }

            // Override old configs. This also makes sure any removed scenes are cleaned up
            configs = GetSortedFinalConfigs(newConfigs);
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public SceneConfig[] GetOrCreateConfigsCopy(IList<string> scenesPaths) {
            int scenesToReturnCount = scenesPaths.Count;
            var configsCopy = new SceneConfig[scenesToReturnCount];
            for (int i = 0; i < scenesToReturnCount; i++) {
                var scenePath = scenesPaths[i];
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                if (TryGetSceneConfigData(sceneName, out var config)) {
                    configsCopy[i] = config.Clone();
                } else {
                    var newConfig = new SceneConfig() {
                        sceneName = sceneName,
                        directory = Path.GetDirectoryName(scenePath),
                    };
                    configsCopy[i] = newConfig;
                }
            }

            return configsCopy;
        }

        public void ApplyAndAddConfigs(SceneConfig[] updatedConfigs) {
            var newConfigs = new List<SceneConfig>(configs.Count + updatedConfigs.Length);
            newConfigs.AddRange(configs);
            for (int i = 0; i < updatedConfigs.Length; i++) {
                var updatedConfig = updatedConfigs[i];
                int matchingIndex = newConfigs.FindIndex((x) => x.sceneName == updatedConfig.sceneName);
                if (matchingIndex != -1) {
                    newConfigs[matchingIndex] = updatedConfig;
                } else {
                    newConfigs.Add(updatedConfig);
                }
            }
            configs = GetSortedFinalConfigs(newConfigs);
            UnityEditor.EditorUtility.SetDirty(this);
        }
        public bool TryGetSceneConfigData(string targetSceneName, out SceneConfig result) {
            int configsCount = configs.Count;
            for (int i = 0; i < configsCount; i++) {
                if (configs[i].sceneName == targetSceneName) {
                    result = configs[i];
                    return true;
                }
            }
            result = default;
            return false;
        }
        
        public void SetSceneConfigData(string targetSceneName, bool? bake = null, bool? APV = null, bool? additive = null) {
            int configsCount = configs.Count;
            for (int i = 0; i < configsCount; i++) {
                if (configs[i].sceneName == targetSceneName) {
                    var config = configs[i];
                    config.bake = bake ?? config.bake;
                    config.APV = APV ?? config.APV;
                    config.additive = additive ?? config.additive;
                    configs[i] = config;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                    return;
                }
            }
            Log.Debug?.Error($"{nameof(SceneConfig)} for scene {targetSceneName} not found");
        }
        
        [UnityEditor.InitializeOnLoadMethod]
        public static void InitOnLoad() {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.newSceneCreated += OnNewSceneCreated;
        }
        
        static List<SceneConfig> GetSortedFinalConfigs(IEnumerable<SceneConfig> newConfigs) {
            return newConfigs.OrderBy(c => c.directory).ThenBy(c => c.sceneName, StringComparer.InvariantCulture).ToList();
        }

        static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode) {
            if (DisableSceneRefresh) return;
            RefreshConfigs(scene);
        }

        static void OnNewSceneCreated(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.NewSceneSetup setup, UnityEditor.SceneManagement.NewSceneMode mode) {
            if (DisableSceneRefresh) return;
            RefreshConfigs(scene);
        }
        
        static void RefreshConfigs(UnityEngine.SceneManagement.Scene scene) {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;

            SceneConfigs sceneConfigs = CommonReferences.Get.SceneConfigs;
            sceneConfigs.UpdateSceneList();
            
            var findObjectOfType = FindAnyObjectByType<AdditiveScene>();
            bool shouldUpdateSetting = findObjectOfType != null && findObjectOfType.gameObject.scene == scene;
            
            if (shouldUpdateSetting) {
                SceneConfig firstOrDefault = sceneConfigs.configs.FirstOrDefault(c => c.sceneName == scene.name);
                if (firstOrDefault != null) {
                    firstOrDefault.additive = true;
                }
            }
            UnityEditor.EditorUtility.SetDirty(sceneConfigs);
        }
#endif
    }
}