using System.Collections.Generic;
using System.IO;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Graphics;
using Awaken.TG.Graphics.Scene;
using Awaken.TG.Main.Saving.Utils;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Awaken.TG.Editor.Utility {
    public static class SimulateBuildTool {
        [MenuItem("TG/Build/Simulate Build mode/Setup from build data", false, 4000)]
        public static void SetupFromBuildData() {
            var gameExePath = EditorUtility.OpenFilePanel("Select game .exe", string.Empty, "exe");
            var exeDirectoryPath = Path.GetDirectoryName(gameExePath);
            var gameName = Path.GetFileNameWithoutExtension(gameExePath);
            var gameDataFolderName = gameName + "_Data";
            var gameDataDirectoryPath = Path.Combine(exeDirectoryPath, gameDataFolderName);
            if (!Directory.Exists(gameDataDirectoryPath)) {
                gameDataDirectoryPath = Path.Combine(exeDirectoryPath, "Data");
            }
            var gameStreamingAssetsPath = Path.Combine(gameDataDirectoryPath, "StreamingAssets");
            var gameStreamingAssetsFilesPaths = Directory.GetFiles(gameStreamingAssetsPath);
            var gameAddressablesDirectoryPath = Path.Combine(gameStreamingAssetsPath, "aa");
            var gameStreamingAssetsDirectoriesPaths = Directory.GetDirectories(gameStreamingAssetsPath);

            var editorApplicationPath = Application.dataPath.Remove(Application.dataPath.Length - 7, 7);
            var editorStreamingAssetsPath = Path.Combine(editorApplicationPath, @"Assets\StreamingAssets");

            CopyStreamingAssets(editorStreamingAssetsPath, gameStreamingAssetsFilesPaths, gameStreamingAssetsDirectoriesPaths, gameAddressablesDirectoryPath);

            var editorWindowsAddressablesPath = Path.Combine(editorApplicationPath, Addressables.BuildPath);
            CopyAddressables(editorWindowsAddressablesPath, gameAddressablesDirectoryPath);
            AddressableHelper.SetAddressablesToUseExistingBuild();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddStaticScenesConfigs(editorWindowsAddressablesPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddDefines();
        }

        static void AddStaticScenesConfigs(string editorWindowsAddressablesPath) {
            Addressables.ClearResourceLocators();
            Addressables.InitializeAsync().WaitForCompletion();
            Addressables.LoadContentCatalogAsync(Path.Combine(editorWindowsAddressablesPath, "catalog.json")).WaitForCompletion();
            Addressables.LoadResourceLocationsAsync("").WaitForCompletion();
            var allScenesPaths = BuildTools.GetAllScenes();
            var staticScenesPaths = new List<string>(20);
            foreach (var scenePath in allScenesPaths) {
                if (scenePath.EndsWith("_Static.unity")) {
                    staticScenesPaths.Add(scenePath);
                    Debug.Log(scenePath);
                }
            }

            var sceneConfigs = AssetDatabase.LoadAssetAtPath<SceneConfigs>(SceneConfigsWindow.SceneConfigAssetPath);
            var staticScenesConfigs = sceneConfigs.GetOrCreateConfigsCopy(staticScenesPaths);
            for (int i = 0; i < staticScenesConfigs.Length; i++) {
                var staticSceneConfig = staticScenesConfigs[i];
                var nonStaticSceneName = staticSceneConfig.sceneName.Substring(0, staticSceneConfig.sceneName.Length - SceneService.StaticSceneSuffix.Length);
                if (sceneConfigs.TryGetSceneConfigData(nonStaticSceneName, out var nonStaticSceneConfig) == false) {
                    Log.Important?.Error($"Cannot find scene config for scene {nonStaticSceneName}");
                    continue;
                }

                staticSceneConfig.bake = nonStaticSceneConfig.bake;
                staticSceneConfig.APV = nonStaticSceneConfig.APV;
                staticSceneConfig.additive = nonStaticSceneConfig.additive;
                staticSceneConfig.openWorld = nonStaticSceneConfig.openWorld;
            }

            sceneConfigs.ApplyAndAddConfigs(staticScenesConfigs);
            Addressables.ClearResourceLocators();
            var loadedBundles = AssetBundle.GetAllLoadedAssetBundles();
            foreach (var loadedBundle in loadedBundles) {
                loadedBundle.Unload(true);
            }
        }

        static void CopyAddressables(string editorWindowsAddressablesPath, string gameAddressablesDirectoryPath) {
            if (Directory.Exists(editorWindowsAddressablesPath)) {
                Directory.Delete(editorWindowsAddressablesPath, true);
            }

            IOUtil.DirectoryCopy(gameAddressablesDirectoryPath, editorWindowsAddressablesPath);
        }

        static void CopyStreamingAssets(string editorStreamingAssetsPath, string[] gameStreamingAssetsFilesPaths, string[] gameStreamingAssetsDirectoriesPaths,
            string gameAddressablesDirectoryPath) {
            if (Directory.Exists(editorStreamingAssetsPath)) {
                Directory.Delete(editorStreamingAssetsPath, true);
            }

            Directory.CreateDirectory(editorStreamingAssetsPath);
            for (int i = 0; i < gameStreamingAssetsFilesPaths.Length; i++) {
                var sourceFilePath = gameStreamingAssetsFilesPaths[i];
                ;
                var destFilePath = Path.Combine(editorStreamingAssetsPath, Path.GetFileName(sourceFilePath));
                File.Copy(sourceFilePath, destFilePath, true);
            }

            for (int i = 0; i < gameStreamingAssetsDirectoriesPaths.Length; i++) {
                var sourceDirectoryPath = gameStreamingAssetsDirectoriesPaths[i];
                if (sourceDirectoryPath == gameAddressablesDirectoryPath) {
                    continue;
                }

                var destDirectoryPath = Path.Combine(editorStreamingAssetsPath, Path.GetFileNameWithoutExtension(sourceDirectoryPath));
                IOUtil.DirectoryCopy(sourceDirectoryPath, destDirectoryPath);
            }
        }

        [MenuItem("TG/Build/Simulate Build mode/Add defines", false, 4000)]
        static void AddDefines() {
            SimulateBuildDefines.AddSimulateBuildDefine();
            SimulateBuildDefines.AddScenesProcessedDefine();
            SimulateBuildDefines.AddAddressablesBuildDefine();
            SimulateBuildDefines.AddArchivesProducedDefine();
        }
    }
}