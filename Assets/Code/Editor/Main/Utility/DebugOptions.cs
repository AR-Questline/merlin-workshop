using System;
using System.Linq;
using Awaken.TG.Debugging;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Main.Utility {
    public class DebugOptions {
        
        // Load On Play
        const string LoadOnPlayMenuName = "TG/Debug/Load On Play";
        public static readonly ToggleOption LoadOnPlay = new ToggleOption("load.on.play", LoadOnPlayMenuName); 
        
        [MenuItem(LoadOnPlayMenuName, false, 5000)]
        static void LostOnPlayMenu() => LoadOnPlay.Switch();
        [MenuItem(LoadOnPlayMenuName, true)]
        static bool LoadOnPlayVal() => LoadOnPlay.Validate();
        
        // Fast Start
        const string FastMenuName = "TG/Debug/Fast Start";
        public static readonly ToggleOption FastStart = new ToggleOption("fast.start", FastMenuName); 
        
        [MenuItem(FastMenuName, false, 5000)]
        static void FastStartMenu() => FastStart.Switch();
        [MenuItem(FastMenuName, true)]
        static bool FastStartVal() => FastStart.Validate();
        
        // Fast Story
        const string FastStoryMenuName = "TG/Debug/Fast Story";
        public static readonly ToggleOption FastStory = new ToggleOption("fast.story", FastStoryMenuName); 
        
        [MenuItem(FastStoryMenuName, false, 5000)]
        static void FastStoryMenu() => FastStory.Switch();
        [MenuItem(FastStoryMenuName, true)]
        static bool FastStoryVal() => FastStory.Validate();
        
        // Fast Notification
        const string FastNotificationMenuName = "TG/Debug/Fast Notifications";
        public static readonly ToggleOption FastNotifications = new ToggleOption("fast.notifications", FastNotificationMenuName);
        [MenuItem(FastNotificationMenuName, false, 5000)]
        static void FastNotificationMenu() => FastNotifications.Switch();
        [MenuItem(FastNotificationMenuName, true)]
        static bool FastNotificationVal() => FastNotifications.Validate();
        
        // Story From Streaming Assets
        const string StoryFromStreamingAssetsMenuName = "TG/Debug/Story/From Streaming Assets";
        public static readonly ToggleOption StoryFromStreamingAssets = new ToggleOption("story_from_streaming_assets", StoryFromStreamingAssetsMenuName); 
        
        [MenuItem(StoryFromStreamingAssetsMenuName, false, 5000)]
        static void StoryFromStreamingAssetsMenu() => StoryFromStreamingAssets.Switch();
        [MenuItem(StoryFromStreamingAssetsMenuName, true)]
        static bool StoryFromStreamingAssetsVal() => StoryFromStreamingAssets.Validate();
        
        // Story Intermediate Assets
        const string StoryIntermediateAssetsMenuName = "TG/Debug/Story/Intermediate Assets";
        public static readonly ToggleOption StoryIntermediateAssets = new ToggleOption("story_intermediate_assets", StoryIntermediateAssetsMenuName); 
        
        [MenuItem(StoryIntermediateAssetsMenuName, false, 5000)]
        static void StoryIntermediateAssetsMenu() => StoryIntermediateAssets.Switch();
        [MenuItem(StoryIntermediateAssetsMenuName, true)]
        static bool StoryIntermediateAssetsVal() => StoryIntermediateAssets.Validate();
        
        // Cuanacht Cutscene
        const string CuanachtCutsceneMenuName = "TG/Debug/Disable Cuanacht Cutscene";
        public static readonly ToggleOption CuanachtCutscene = new ToggleOption("cuanacht.cutscene.disable", CuanachtCutsceneMenuName); 
        
        [MenuItem(CuanachtCutsceneMenuName, false, 5000)]
        static void CuanachtCutsceneMenu() => CuanachtCutscene.Switch();
        [MenuItem(CuanachtCutsceneMenuName, true)]
        static bool CuanachtCutsceneVal() => CuanachtCutscene.Validate();
        
        // Skill Machines On Separate Objects
        const string SkillMachinesOnSeparateObjectsMenuName = "TG/Debug/Skill Machines On Separate Objects";
        public static readonly ToggleOption SkillMachinesOnSeparateObjects = new ToggleOption("skill.machines.separate.objects", SkillMachinesOnSeparateObjectsMenuName);
        
                
        [MenuItem(SkillMachinesOnSeparateObjectsMenuName, false, 5000)]
        static void SkillMachinesOnSeparateObjectsMenu() => SkillMachinesOnSeparateObjects.Switch();
        [MenuItem(SkillMachinesOnSeparateObjectsMenuName, true)]
        static bool SkillMachinesOnSeparateObjectsVal() => SkillMachinesOnSeparateObjects.Validate();
        
        // Debug Story
        const string DebugStoryMenuName = "TG/Debug/Debug Story";
        public static readonly ToggleOption DebugStory = new ToggleOption("debug.story.start", DebugStoryMenuName); 
        
        [MenuItem(DebugStoryMenuName, false, 5000)]
        static void DebugStoryMenu() => DebugStory.Switch();
        [MenuItem(DebugStoryMenuName, true)]
        static bool DebugStoryVal() => DebugStory.Validate();
        
        // Debug GamePad
        const string DebugGamePadMenuName = "TG/Debug/Debug GamePad";
        public static readonly ToggleOption DebugGamePad = new ToggleOption("debug.game.pad", DebugGamePadMenuName); 
        
        [MenuItem(DebugGamePadMenuName, false, 5000)]
        static void DebugGamePadMenu() => DebugGamePad.Switch();
        [MenuItem(DebugGamePadMenuName, true)]
        static bool DebugGamePadVal() => DebugGamePad.Validate();
        
        // Debug Tutorials
        const string DebugTutorialMenuName = "TG/Debug/Debug Tutorial";
        public static readonly ToggleOption DebugTutorial = new ToggleOption("debug.tutorial", DebugTutorialMenuName);

        [MenuItem(DebugTutorialMenuName, false, 5000)]
        static void DebugTutorialMenu() => DebugTutorial.Switch();
        [MenuItem(DebugTutorialMenuName, true)]
        static bool DebugTutorialVal() => DebugTutorial.Validate();
        
        // Reset Tutorials
        const string ResetTutorialMenuName = "TG/Debug/Auto Reset Tutorial";
        public static readonly ToggleOption DebugResetTutorial = new ToggleOption("debug.reset.tutorial", ResetTutorialMenuName); 
        
        [MenuItem(ResetTutorialMenuName, false, 5000)]
        static void DebugResetTutorialMenu() => DebugResetTutorial.Switch();
        [MenuItem(ResetTutorialMenuName, true)]
        static bool DebugResetTutorialVal() => DebugResetTutorial.Validate();
        
        // Debug Proficiencies
        const string DebugProficiencyXPMenuName = "TG/Debug/Debug Proficiency";
        public static readonly ToggleOption DebugProficiency = new ToggleOption("debug.proficiency", DebugProficiencyXPMenuName);
        
        // Debug Thievery
        const string DebugThieveryMenuName = "TG/Debug/Debug Thievery";
        public static readonly ToggleOption DebugThievery = new ToggleOption("debug.thievery", DebugThieveryMenuName);
        
        // Debug Perception
        const string DebugPerceptionMenuName = "TG/Debug/Debug Perception";
        public static readonly ToggleOption DebugPerception = new ToggleOption("debug.perception", DebugPerceptionMenuName); 
        
        // Debug Circling
        const string DebugCirclingMenuName = "TG/Debug/Debug Circling";
        public static readonly ToggleOption DebugCircling = new ToggleOption("debug.circling", DebugCirclingMenuName);
        
        // Skills From Streaming Assets
        const string SkillsFromStreamingAssetsMenuName = "TG/Debug/Skills From Streaming Assets";
        public static readonly ToggleOption SkillsFromStreamingAssets = new ToggleOption("skills_from_streaming_assets", SkillsFromStreamingAssetsMenuName);
        
        // Debug CC Preset
        const string DebugCCPresetMenu = "TG/Debug/CC Preset/";
        const string DebugCCPresetMenuR = DebugCCPresetMenu + "Random";
        const string DebugCCPresetMenu0 = DebugCCPresetMenu + "Male 0";
        const string DebugCCPresetMenu1 = DebugCCPresetMenu + "Female 0";
        const string DebugCCPresetMenu2 = DebugCCPresetMenu + "Male 1";
        const string DebugCCPresetMenu3 = DebugCCPresetMenu + "Female 1";
        public static readonly SwitchOption DebugCCPreset = new SwitchOption("debug.cc-preset", -1);
        
        [MenuItem(DebugCCPresetMenuR, false)] static void DebugCCPresetSwitchR() => DebugCCPreset.Switch(-1);
        [MenuItem(DebugCCPresetMenu0, false)] static void DebugCCPresetSwitch0() => DebugCCPreset.Switch(0);
        [MenuItem(DebugCCPresetMenu1, false)] static void DebugCCPresetSwitch1() => DebugCCPreset.Switch(1);
        [MenuItem(DebugCCPresetMenu2, false)] static void DebugCCPresetSwitch2() => DebugCCPreset.Switch(2);
        [MenuItem(DebugCCPresetMenu3, false)] static void DebugCCPresetSwitch3() => DebugCCPreset.Switch(3);
        [MenuItem(DebugCCPresetMenuR, true)] static bool DebugCCPresetValR() => DebugCCPreset.Validate(DebugCCPresetMenuR, -1);
        [MenuItem(DebugCCPresetMenu0, true)] static bool DebugCCPresetVal0() => DebugCCPreset.Validate(DebugCCPresetMenu0, 0);
        [MenuItem(DebugCCPresetMenu1, true)] static bool DebugCCPresetVal1() => DebugCCPreset.Validate(DebugCCPresetMenu1, 1);
        [MenuItem(DebugCCPresetMenu2, true)] static bool DebugCCPresetVal2() => DebugCCPreset.Validate(DebugCCPresetMenu2, 2);
        [MenuItem(DebugCCPresetMenu3, true)] static bool DebugCCPresetVal3() => DebugCCPreset.Validate(DebugCCPresetMenu3, 3);
        
        
        // Story Debug Tool
        [MenuItem("TG/Debug/Story Debug Tool", false, -1000)]
        static void DebugStoryTool() {
            StoryDebugTool tool = Object.FindAnyObjectByType<StoryDebugTool>();
            if (tool == null) {
                GameObject go = new GameObject("Story Debug Tool");
                go.hideFlags = HideFlags.DontSave;
                tool = go.AddComponent<StoryDebugTool>();
                SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
            }

            Selection.activeObject = tool.gameObject;
        }
        
        // Memory Debug
        [MenuItem("TG/Debug/UnloadUnused", false, -1000)]
        static void UnloadUnusedMemory() {
            Resources.UnloadUnusedAssets();
        }
        
        [MenuItem("TG/Debug/ForceReserializeSelected")]
        static void ForceReserializeSelected() {
            AssetDatabase.StartAssetEditing();
            try {
                AssetDatabase.ForceReserializeAssets(Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath));
            } finally {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
        }
        
        // Vegetation
        const string VegetationSwitchKey = "TG/Debug/Vegetation Disabled";
        public static readonly ToggleOption DebugVegetationDisabled = new ToggleOption("debug.vegetation.disabled", VegetationSwitchKey);
        
        [MenuItem(VegetationSwitchKey, false, 5000)]
        static void DebugVegetationMenu() => DebugVegetationDisabled.Switch();
        [MenuItem(VegetationSwitchKey, true)]
        static bool DebugVegetationVal() => DebugVegetationDisabled.Validate();

        const string LeshySwitchKey = "TG/Debug/Leshy Disabled";
        public static readonly ToggleOption DebugLeshyDisabled = new ToggleOption("debug.leshy.disabled", LeshySwitchKey);

        [MenuItem(LeshySwitchKey, false, 5001)]
        static void DebugLeshyMenu() => DebugLeshyDisabled.Switch();
        [MenuItem(LeshySwitchKey, true)]
        static bool DebugLeshyVal() => DebugLeshyDisabled.Validate();
        
        //Skip Tutorial
        const string SkipTutorial = "TG/Debug/Skip Tutorial";
        public static readonly ToggleOption DebugSkipTutorial = new ToggleOption("debug.skip.tutorial", SkipTutorial);
                                                                                                                      
        [MenuItem(SkipTutorial, false, 5000)]
        static void DebugSkipTutorialMenu() => DebugSkipTutorial.Switch();
        [MenuItem(SkipTutorial, true)]
        static bool DebugSkipTutorialVal() => DebugSkipTutorial.Validate();
        
        //Cloud Conflict Popup
        const string CloudConflict = "TG/Debug/Enable Cloud Conflict Popup at Start";
        public static readonly ToggleOption DebugCloudConflict = new("debug.cloud.conflict", CloudConflict);
                                                                                                                      
        [MenuItem(CloudConflict, false, 5000)]
        static void DebugEnableCloudConflictMenu() => DebugCloudConflict.Switch();
        [MenuItem(CloudConflict, true)]
        static bool DebugEnableCloudConflictVal() => DebugCloudConflict.Validate();

        //Log Movement Unsafe Overrides
        public static readonly ToggleOption LogMovementUnsafeOverrides = new ToggleOption("log.movement.unsafe.changes", null);
        //Log Animancer Using Fallback States
        public static readonly ToggleOption LogAnimancerUseFallbackState = new ToggleOption("log.animancer.fallback.state", null);

        const string AutoPlayAnimations = "TG/Animations/Autoplay AnimationClip Preview";
        [MenuItem(AutoPlayAnimations, false)]
        static void AutoplayAnimationClipPreviewVal() => AutoplayAnimationClipPreview.Switch();
        public static readonly ToggleOption AutoplayAnimationClipPreview = new ToggleOption("autoplay.animation.clip.preview", AutoPlayAnimations, 1);
        
        // GameModes
        const string GameModeMenu = "TG/Debug/GameMode/";
        const string GameModeDemoName = GameModeMenu + "Demo";
        [MenuItem(GameModeDemoName, false)]
        static void GameModeDemoMenu() => GameModeDemo.Switch();
        [MenuItem(GameModeDemoName, true)]
        static bool GameModeDemoVal() => GameModeDemo.Validate();
        public static readonly ToggleOption GameModeDemo = new("gamemode.demo", GameModeDemoName); 
    }

    public class ToggleOption {
        public string key;
        public string menuName;
        int _defaultValue;
        public bool Active => EditorPrefs.GetInt(key, _defaultValue) == 1;

        public ToggleOption(string key, string menuName, int defaultValue = 0) {
            this.key = key;
            this.menuName = menuName;
            _defaultValue = defaultValue;
        }
        
        public void Switch() {
            var newValue = (EditorPrefs.GetInt(key, _defaultValue) + 1) % 2;
            EditorPrefs.SetInt(key, newValue);
        }

        public bool Validate() {
            var isChecked = Menu.GetChecked(menuName);
            var isEnabled = Active;
            if (isChecked != isEnabled) {
                Menu.SetChecked(menuName, isEnabled);
            }

            return true;
        }
    }

    public class SwitchOption {
        string _key;
        int _defaultValue;
        
        public int Value => EditorPrefs.GetInt(_key, _defaultValue);
        
        public SwitchOption(string key, int defaultValue = 0) {
            this._key = key;
            _defaultValue = defaultValue;
        }

        public void Switch(int index) {
            EditorPrefs.SetInt(_key, index == Value ? _defaultValue : index);
        }

        public bool Validate(string menuName, int index) {
            var isChecked = Menu.GetChecked(menuName);
            var isEnabled = index == Value;
            if (isChecked != isEnabled) {
                Menu.SetChecked(menuName, isEnabled);
            }
            return true;
        }
    }
}