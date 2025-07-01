using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Main.Utility;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.DevToolkit {
    public class DevToolkit : EditorWindow {
        Vector2 _scrollPos;

        static ToggleOption s_doubleColumn = new ToggleOption("dev.toolkit.double.column", "Double column");
        static ToggleOption s_showGameModes = new ToggleOption("dev.toolkit.show.gamemodes", "Show GameModes");

        [MenuItem("TG/Dev Toolkit")]
        public static void ShowWindow() {
            var window = GetWindow<DevToolkit>();
            window.titleContent = new GUIContent("Dev Toolkit");
            window.Show();
        }

        void OnGUI() {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            if (s_doubleColumn.Active) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
            }

            DrawMenuItemButton("Models Debug", "TG/Debug/Models Debug %u");
            DrawMenuItemButton("Selection History", "Window/General/Selection History");
            DrawMenuItemButton("Find by GUID", "TG/Assets/Find by GUID");
            DrawMenuItemButton("Scenes", "Window/Scenes");
            DrawMenuItemButton("Story Debug Tool", "TG/Debug/Story Debug Tool");
            DrawMenuItemButton("Debug UI", "TG/Debug/Debug UI");
            DrawMenuItemButton("Unload unused Assets", "TG/Debug/UnloadUnused");
            DrawMenuItemButton("Force Reserialize Selected", "TG/Debug/ForceReserializeSelected");
            
            EditorGUILayout.Space();
            GUILayout.Label("Log Filter");
            Log.Utils.LogType = (LogType) EditorGUILayout.EnumFlagsField(Log.Utils.LogType);
            EditorGUILayout.Space();

            DrawMenuItemToggle("Show GameModes", s_showGameModes);
            if (s_showGameModes.Active) {
                DrawMenuItemToggle("Demo", DebugOptions.GameModeDemo);
            }
            
            if (s_doubleColumn.Active) {
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
            }

            DrawMenuItemToggle("Load on Play", DebugOptions.LoadOnPlay);
            DrawMenuItemToggle("Fast Start", DebugOptions.FastStart);
            DrawMenuItemToggle("Skip Tutorial", DebugOptions.DebugSkipTutorial);
            DrawMenuItemToggle("Fast Story", DebugOptions.FastStory);
            DrawMenuItemToggle("Fast Notifications", DebugOptions.FastNotifications);
            DrawMenuItemToggle("Story From Streaming Assets", DebugOptions.StoryFromStreamingAssets);
            DrawMenuItemToggle("Story Intermediate Assets", DebugOptions.StoryIntermediateAssets);
            DrawMenuItemToggle("Disable Cuanacht Cutscene", DebugOptions.CuanachtCutscene);
            DrawMenuItemToggle("Spawn Skill Machines on separate objects", DebugOptions.SkillMachinesOnSeparateObjects);
            DrawMenuItemToggle("Vegetation Disabled", DebugOptions.DebugVegetationDisabled);
            DrawMenuItemToggle("Leshy Disabled", DebugOptions.DebugLeshyDisabled);
            //DrawMenuItemToggle("Debug Story", DebugOptions.DebugStory);
            DrawMenuItemToggle("Debug GamePad", DebugOptions.DebugGamePad);
            DrawMenuItemToggle("Debug Tutorial", DebugOptions.DebugTutorial);
            DrawMenuItemToggle("Debug Proficiencies", DebugOptions.DebugProficiency);
            DrawMenuItemToggle("Debug Thievery", DebugOptions.DebugThievery);
            DrawMenuItemToggle("Debug Perception", DebugOptions.DebugPerception);
            DrawMenuItemToggle("Debug Circling", DebugOptions.DebugCircling);
            DrawMenuItemToggle("Skills From Streaming Assets", DebugOptions.SkillsFromStreamingAssets);
            if (DrawFileBasedToggle("Debug Project Names", DebugProjectNames.DebugProjectNamesID)) {
                DebugProjectNames.SyncDebugNamesCache();
            }
            DrawMenuItemToggle("Debug Cloud Conflict Popup (test on Title Screen)", DebugOptions.DebugCloudConflict);
            DrawMenuItemToggle("Auto Reset Tutorial", DebugOptions.DebugResetTutorial);
            DrawMenuItemToggle("Log Movement Unsafe Overrides", DebugOptions.LogMovementUnsafeOverrides);
            DrawMenuItemToggle("Log Animancer Using Fallback States", DebugOptions.LogAnimancerUseFallbackState);
            DrawMenuItemToggle("Autoplay Animation Clip Preview", DebugOptions.AutoplayAnimationClipPreview);
            
            if (!Selection.objects.IsNullOrEmpty()) {
                GUILayout.Label($"Selection has {Selection.objects.Length} object");
                if (GUILayout.Button("Force Reserialize Selection")) {
                    AssetDatabase.StartAssetEditing();
                    try {
                        AssetDatabase.ForceReserializeAssets(GetSelectedAssetPaths());
                    } finally {
                        AssetDatabase.StopAssetEditing();
                        AssetDatabase.Refresh();
                    }
                }
            }

            if (s_doubleColumn.Active) {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            
            DrawMenuItemToggle("Double column", s_doubleColumn);

            EditorGUILayout.EndScrollView();
        }

        void DrawMenuItemButton(string text, string menuItemPath) {
            if (GUILayout.Button(text)) {
                EditorApplication.ExecuteMenuItem(menuItemPath);
            }
        }

        void DrawMenuItemToggle(string text, ToggleOption toggleOption) {
            var active = GUILayout.Toggle(toggleOption.Active, text);
            if (toggleOption.Active != active) {
                toggleOption.Switch();
            }
        }

        bool DrawFileBasedToggle(string text, string key) {
            if (Application.isPlaying && !CloudService.IsInitialized) return false;
            
            var active = FileBasedPrefs.GetBool(key, false);
            var newActive = GUILayout.Toggle(active, text);
            if (newActive != active) {
                FileBasedPrefs.SetBool(key, newActive, false);
                FileBasedPrefs.SaveAll();
                return true;
            }
            return false;
        }
        
        IEnumerable<string> GetSelectedAssetPaths() {
            return Selection.objects.Select(AssetDatabase.GetAssetPath);
        }
    }
}
