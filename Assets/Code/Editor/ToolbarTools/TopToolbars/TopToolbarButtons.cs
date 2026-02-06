﻿using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.BalanceTool;
using Awaken.TG.Editor.Debugging.DebugWindows;
using Awaken.TG.Editor.Debugging.RenderingValidations;
using Awaken.TG.Editor.Main.Scenes.SubdividedScenes;
using Awaken.TG.Editor.SimpleTools;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.StoryGraphs.Toolset.CustomWindow;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.ToolbarTools.TopToolbars {
    [InitializeOnLoad]
    public static class TopToolbarButtons {
        static readonly ITopToolbarElement[] OriginalElements = {
            new TopToolbarButton("★", "Open preferences", OpenPreferences, 20, Side.Left, true),
            // new TopToolbarButton("Chat", "Open conversation with AI", AIChatWindow.OpenWindow, 50, Side.Left, true),
            new TopToolbarButton("Patch Notes", "Ping Patch Notes Object", OpenPatchNotes, 80, Side.Left, true),
            new TopToolbarButton("Models Debug", "Open Models Debug", ModelsDebugWindow.ShowWindow, 100, Side.Left,
                true),
            new TopToolbarButton("Scenes", "Open Scenes", SceneWindow.ShowWindow, 80, Side.Left, true),
            new TopToolbarTimeScaleSlider(),
            
            new TopToolbarSpace(Side.Left),
            new TopToolbarButton("►►", "Play at camera position", StartGameWithEditorCameraPosition, 30, Side.Left, true),
            new TopToolbarButton("Rendering Validator", "Open Rendering Validator", RenderingValidatorWindow.ShowWindow, 100, Side.Right, false),
            new TopToolbarSpace(Side.Right),
            
            new TopToolbarButton("Dev toolkit", "Open Dev toolkit", DevToolkit.DevToolkit.ShowWindow, 80, Side.Right, true),
            new TopToolbarDropdown("Editor Look At", "Teleports to various preset locations", GetTPPoints(), 93, Side.Right, false, static () => IsCampaignOpen(), true),
            // new TopToolbarButton("Story Tool", "Open Story Toolkit", StoryGraphToolsetEditor.ShowWindow, 80, Side.Right, true),
            new TopToolbarButton("Balance Tool", "Open RPG Balance Tool", RPGBalanceTool.ShowWindow, 85, Side.Right, true),
        };

        public static readonly ITopToolbarElement[] Elements = OriginalElements.ToArray();

        static List<ITopToolbarElement> _leftElements = new();
        static List<ITopToolbarElement> _rightElements = new();

        static TopToolbarButtons() {
            ToolbarExtender.LeftToolbarGUI.Add(OnLeftToolbarGUI);
            ToolbarExtender.RightToolbarGUI.Add(OnRightToolbarGUI);
            AssignSides();
        }
        
        public static void AssignSides() {
            Array.Sort(Elements, static (l, r) => l.Order.CompareTo(r.Order));
            _leftElements.Clear();
            _rightElements.Clear();
            foreach (var element in Elements) {
                if (element.Side == Side.Right) {
                    element.Order = _rightElements.Count+1000;
                    _rightElements.Add(element);
                } else {
                    element.Order = _leftElements.Count;
                    _leftElements.Add(element);
                }
            }
            Array.Sort(Elements, static (l, r) => l.Order.CompareTo(r.Order));
        }

        public static void OnOrderReset() {
            for (int i = 0; i < OriginalElements.Length; i++) {
                Elements[i] = OriginalElements[i];
            }
            AssignSides();
        }

        static void OnLeftToolbarGUI() {
            OnToolbarGUI(_leftElements);
        }
        
        static void OnRightToolbarGUI() {
            OnToolbarGUI(_rightElements);
        }

        static void OnToolbarGUI(List<ITopToolbarElement> elements) {
            GUILayout.Space(TopToolbarSettings.Instance.ToolbarMargin);

            var spacing = TopToolbarSettings.Instance.ToolbarSpacing;
            for (int i = 0; i < elements.Count; i++) {
                if (!elements[i].Enabled) {
                    continue;
                }
                elements[i].OnGUI();
                if (i != elements.Count - 1) {
                    GUILayout.Space(spacing);
                }
            }
            GUILayout.Space(TopToolbarSettings.Instance.ToolbarMargin);
        }

        static void OpenPatchNotes() {
            PatchNotes patchNotes = AssetDatabase.LoadAssetAtPath<PatchNotes>("Assets/Data/PatchNotes.asset");
            EditorUtility.OpenPropertyEditor(patchNotes);
        }

        static void OpenPreferences() {
            SettingsService.OpenUserPreferences(TopToolbarSettings.PreferencesTgTopToolbarPath);
        }
        
        static DropdownEntree[] GetTPPoints() {
            return new[] {
                new("Clipboard", () => {
                    var input = GUIUtility.systemCopyBuffer;
                    
                    if (input.IsNullOrWhitespace()) {
                        Log.Important?.Error("Clipboard content is empty");
                        return;
                    }
                    var match = System.Text.RegularExpressions.Regex.Match(input, @"\((-?\d+[.,]\d+), (-?\d+[.,]\d+), (-?\d+[.,]\d+)\)");
                    if (!match.Success) {
                        Log.Important?.Error($"Clipboard content doesn't match the expected format: (x, y, z) of float values");
                        return;
                    }
                    var x = float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var y = float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var z = float.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    SceneLookAs(new Vector3(x, y + 4, z), Quaternion.LookRotation(Vector3.down).eulerAngles);
                }),
                DropdownEntree.Separator(),
                new("Spawn", () => SceneLookAt(new Vector3(-1600, 45, -3680)), isDisabled: NotHOSScene),
                new("HOS Castle", () => SceneLookAt(new Vector3(-2137, 125, -3685)), isDisabled: NotHOSScene),
                new("All Mothers", () => SceneLookAt(new Vector3(-1240, 85, -3199)), isDisabled: NotHOSScene),
                new("Stonehenge", () => SceneLookAt(new Vector3(-1791, 85, -2910)), isDisabled: NotHOSScene),
                
                new("Cuanacht", () => SceneLookAt(new Vector3(-953, 165, -2600)), isDisabled: NotCuanachtScene),
                new("Swamp", () => SceneLookAs(new Vector3(-1717, 308, -2624), new Vector3(39, 39, 0)), isDisabled: NotCuanachtScene),
                new("Tree", () => SceneLookAs(new Vector3(-1172, 137, -2822), new Vector3(31, 104, 0)), isDisabled: NotCuanachtScene),
                
                new("Burnt Village", () => SceneLookAs(new Vector3(-181, 328, -2982), new Vector3(51, 98, 0)), isDisabled: NotForlornScene),
                new("Capital City", () => SceneLookAs(new Vector3(367, 486, -3597), new Vector3(34, 134, 0)), isDisabled: NotForlornScene),
                new("Highlands Stronghold", () => SceneLookAs(new Vector3(341, 443, -3176), new Vector3(37, 65, 0)), isDisabled: NotForlornScene),
                
                new("Sarras Spawn", () => SceneLookAs(new Vector3(-127, 134.08f, -349.15f), new Vector3(43.12f, 60.58f, 0f)), isDisabled: NotSarrasScene),
                new("Hatchery", () => SceneLookAs(new Vector3(-327.44f, 144.99f, 41.66f), new Vector3(28.51f, 21.57f, 0f)), isDisabled: NotSarrasScene),
                new("Snake", () => SceneLookAs(new Vector3(389.43f, 184.55f, -221.51f), new Vector3(44.67f, 208.92f, -0f)), isDisabled: NotSarrasScene),
                new("Archive", () => SceneLookAs(new Vector3(195.66f, 126.60f, 108.99f), new Vector3(32.98f, 50.27f, -0f)), isDisabled: NotSarrasScene),
                new("Observatory", () => SceneLookAs(new Vector3(158.82f, 150.04f, 293.27f), new Vector3(31.78f, 227.21f, -0f)), isDisabled: NotSarrasScene),
                new("Geysers", () => SceneLookAs(new Vector3(-204.98f, 250.43f, -92.81f), new Vector3(47.25f, 225.32f, -0f)), isDisabled: NotSarrasScene),
            };
        }

        static void SceneLookAt(Vector3 pos) {
            SceneView scene = SceneView.lastActiveSceneView;

            if (scene == null) {
                throw new Exception($"{nameof(SceneView)}.{nameof(SceneView.lastActiveSceneView)} is {scene}");
            }
            scene.LookAtDirect(pos, Quaternion.LookRotation(Vector3.down + Vector3.forward), 50);
            scene.Repaint();
        }
        
        static void SceneLookAs(Vector3 pos, Vector3 rotation) {
            SceneView scene = SceneView.lastActiveSceneView;

            if (scene == null) {
                throw new Exception($"{nameof(SceneView)}.{nameof(SceneView.lastActiveSceneView)} is {scene}");
            }
            var reference = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            reference.position = pos;
            reference.rotation = Quaternion.Euler(rotation);
                
            scene.AlignViewToObject(reference);
            Object.DestroyImmediate(reference.gameObject);
            scene.Repaint();
        }
        
        static bool IsOnCorrectScene(string sceneName) {
            var activeScene = SceneManager.GetActiveScene();
            return activeScene.name == sceneName;
        }
        
        static bool NotSarrasScene() {
            return !IsOnCorrectScene("CampaignMap_Sarras");
        }
        
        static bool NotHOSScene() {
            return !IsOnCorrectScene("CampaignMap_HOS");
        }
        
        static bool NotCuanachtScene() {
            return !IsOnCorrectScene("CampaignMap_Cuanacht");
        }
        
        static bool NotForlornScene() {
            return !IsOnCorrectScene("CampaignMap_Forlorn");
        }
        
        static bool IsCampaignOpen() {
            if (Application.isPlaying) {
                return SceneManager.GetActiveScene().name == "CampaignMap";
            } else {
                return SubdividedSceneTracker.TryGet(out _);
            }
        }

        static void StartGameWithEditorCameraPosition() {
            ProjectValidator.SaveSpawnCoordsOverride(SceneView.lastActiveSceneView.camera.transform.position);
            EditorApplication.isPlaying = true;
        }

        public enum Side : byte {
            Right,
            Left,
        }
    }

    public static class TopToolbarButtonsSideExtensions {
        public static TopToolbarButtons.Side Other(this TopToolbarButtons.Side side) {
            return side == TopToolbarButtons.Side.Left ? TopToolbarButtons.Side.Right : TopToolbarButtons.Side.Left;
        }

        public static bool IsRight(this TopToolbarButtons.Side side) {
            return side == TopToolbarButtons.Side.Right;
        }

        public static bool IsLeft(this TopToolbarButtons.Side side) {
            return side == TopToolbarButtons.Side.Left;
        }
    }
}