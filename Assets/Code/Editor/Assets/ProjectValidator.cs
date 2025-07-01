using System;
using System.IO;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Editor.Debugging.GUIDSearching;
using Awaken.TG.Editor.Validation;
using Awaken.TG.Graphics.Scene;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor;
using UnityEditor;
using UnityEditor.Localization.Addressables;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets {
    [InitializeOnLoad]
    public static class ProjectValidator {
        static SceneConfigs s_sceneConfigs;

        // same key is used in NewGameLoading.Load
        const string IntendedScene = TitleScreenUtils.IntendedScene;
        const string SceneConfigsGuid = "02a758c4b41df8e4294f4fddcb9adf19";
        
        public static SceneValidator SceneValidator { get; private set; }

        // === Static constructor (gets called when Unity is opened, or scripts reloaded)
        static ProjectValidator() {
            try {
                Validate();
            } catch (Exception e) {
                Debug.LogException(e);
            }

            LoadEditorSettings();

            s_sceneConfigs = AssetDatabase.LoadAssetAtPath<SceneConfigs>(AssetDatabase.GUIDToAssetPath(SceneConfigsGuid));

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            //EditorSceneManager.activeSceneChangedInEditMode += AdjustQualityToApvSettings;

            EditorApplication.projectWindowItemOnGUI += FirstOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += FirstOnGUI;

            SceneValidator = new SceneValidator(new SceneValidator.Config {
                displayDialogue = true,
            });
            SceneValidator.RegisterCallbacks();
        }

        static void FirstOnGUI(int _, Rect _1) => FirstOnGUI();
        static void FirstOnGUI(string _, Rect _1) => FirstOnGUI();

        static void FirstOnGUI() {
            EditorApplication.hierarchyWindowItemOnGUI -= FirstOnGUI;
            EditorApplication.projectWindowItemOnGUI -= FirstOnGUI;
            
            GameViewLocalePatch.ApplyPatch();
            TemplatesSearcher.EnsureInit();
            GUIDCache.Load();
            ARAssetReference.EditorAssignUnusedGuids(GUIDCache.Instance.UnusedCache);
        }

        static void LoadEditorSettings() {
            LightController.EditorPreviewUpdates = DynamicLightsActivePref;

            var defaultStringResolver = AddressableGroupRules.Instance.StringTablesResolver;
            AddressableGroupRules.Instance.StringTablesResolver =
                new OptimizedGroupResolver(defaultStringResolver.LocaleGroupNamePattern,
                    defaultStringResolver.SharedGroupName);
        }

        // === Validation
        public static void Validate() {
            ValidateGitConfigFile();
            GitUtils.Validate();
        }

        static void ValidateGitConfigFile() {
            string includeToAdd = "\n[include]\n\tpath = ..\\\\.gitconfig.txt";
            string pathToConfig = $@"{Application.dataPath}\..\.git\config";
            var config = File.ReadAllText(pathToConfig);
            if (!config.Contains("[include]")) {
                File.WriteAllText(pathToConfig, config + includeToAdd);
            }
        }
        
        // === Callbacks

        static void OnPlayModeStateChanged(PlayModeStateChange newPlayMode) {
            if (newPlayMode == PlayModeStateChange.ExitingEditMode) {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                    PrepareScenesOnExitEditMode();
                } else {
                    EditorApplication.isPlaying = false;
                }

            } else if (newPlayMode == PlayModeStateChange.EnteredPlayMode) {
                TemplatesSearcher.EnsureInit();
                PrepareScenesOnEnteredPlayMode();
                SetMipmapsStreaming();
            } else if (newPlayMode == PlayModeStateChange.ExitingPlayMode) {
                try {
                    World.Services.Get<SceneService>().MainSceneBehaviour.Unload(false);
                } catch {
                    // ignored
                }
            } else if (newPlayMode == PlayModeStateChange.EnteredEditMode) {
                RevertMipmapsStreaming();
            }
        }

        static void PrepareScenesOnExitEditMode() {
            if (SceneGlobals.Scene != null) {
                SaveIntendedScene();
                SetPlayModeStartScene($"Assets/Scenes/{nameof(ApplicationScene)}.unity");
            } else {
                ResetIntendedScene();
                bool isTitleScreen = Object.FindAnyObjectByType<TitleScreen>() != null; 
                if (isTitleScreen) {
                    SetPlayModeStartScene("Assets/Scenes/BuildInitialScene.unity");
                } else {
                    Log.Important?.Error("No main or titleScreen scene found! Should only happen if non game integrated scene is launched");
                }
            }
        }

        static void RevertMipmapsStreaming() {
            QualitySettings.streamingMipmapsActive = true;
        }

        static void PrepareScenesOnEnteredPlayMode() {
            if (EditorPrefs.HasKey(IntendedScene)) {
                string intendedScene = EditorPrefs.GetString(IntendedScene);
                SceneReference sceneRef = SceneReference.ByName(intendedScene);

                string sceneGuid = AssetDatabase.FindAssets($"t:Scene {sceneRef.Name}", new[] {"Assets/Scenes"}).FirstOrDefault();

                if (!AddressableHelper.IsAddressable(sceneGuid)) {
                    Object sceneObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(sceneGuid), typeof(Object));
                    AddressableHelper.AddEntry(new AddressableEntryDraft.Builder(sceneObject)
                        .WithAddressProvider((_,_) => sceneRef.Name)
                        .InGroup(AddressableGroup.ScenesEditor.ToString())
                        .WithLabel(SceneService.ScenesLabel)
                        .Build());
                }
                // TODO: Maybe load additive scene here instead of inside of loading
                ScenePreloader.EditorLoad(sceneRef);
            }
        }

        static void SetMipmapsStreaming() {
            QualitySettings.streamingMipmapsActive = !TGEditorPreferences.Instance.disableMipmapsStreaming;
        }

        static void SetPlayModeStartScene(string scenePath) {
            SceneAsset myWantedStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (myWantedStartScene != null) {
                EditorSceneManager.playModeStartScene = myWantedStartScene;
            } else {
                Log.Important?.Info("Could not find Scene " + scenePath);
            }
        }

        static void SaveIntendedScene() {
            EditorPrefs.SetString(IntendedScene, SceneManager.GetActiveScene().name);
        }

        static void ResetIntendedScene() {
            EditorPrefs.DeleteKey(IntendedScene);
            EditorSceneManager.playModeStartScene = null;
        }

        // Currently disabled, enable if we start using apv again
        static void AdjustQualityToApvSettings(Scene _, Scene sceneNew) {
            if (string.IsNullOrEmpty(sceneNew.path) || string.IsNullOrEmpty(sceneNew.name)) {
                return;
            }
            bool apvState = s_sceneConfigs != null && s_sceneConfigs.HasApvEnabled(SceneReference.ByScene(sceneNew));
            GeneralGraphicsWithAPV.RefreshQuality(apvState);
        }
        
        public static bool DynamicLightsActivePref { 
            get => EditorPrefs.GetBool(LightController.PrefsKey + "DynamicLights"); 
            set => EditorPrefs.SetBool(LightController.PrefsKey + "DynamicLights", value); 
        }
        
        public static bool DynamicLightsAllActivePref { 
            get => EditorPrefs.GetBool(LightController.PrefsKey + "DynamicLightsAll"); 
            set => EditorPrefs.SetBool(LightController.PrefsKey + "DynamicLightsAll", value); 
        }
    }

    public class AfterAssetValidator : AssetPostprocessor {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            EditorApplication.update += Update;
        }
        
        static void Update() {
            if (!Application.isPlaying) {
                try {
                    ProjectValidator.Validate();
                } finally {
                    EditorApplication.update -= Update;
                }
            }
        }
    }
}