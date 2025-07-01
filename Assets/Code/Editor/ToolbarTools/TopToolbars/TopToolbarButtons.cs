using System;
using System.Collections.Generic;
using System.Linq;
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

            new TopToolbarButton("Rendering Validator", "Open Rendering Validator", RenderingValidatorWindow.ShowWindow, 100, Side.Right, false),
            new TopToolbarSpace(Side.Right),
            new TopToolbarButton("Dev toolkit", "Open Dev toolkit", DevToolkit.DevToolkit.ShowWindow, 80, Side.Right, true),
            new TopToolbarDropdown("Editor Look At", "Teleports to various preset locations", GetTPPoints(), 93, Side.Right, false, static () => IsCampaignOpen()),
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
                new("Spawn", () => SceneLookAt(new Vector3(-1600, 45, -3680))),
                new("HOS Castle", () => SceneLookAt(new Vector3(-2137, 125, -3685))),
                new("All Mothers", () => SceneLookAt(new Vector3(-1240, 85, -3199))),
                new("Stonehenge", () => SceneLookAt(new Vector3(-1791, 85, -2910))),
                DropdownEntree.Separator(),
                new("Cuanacht", () => SceneLookAt(new Vector3(-953, 165, -2600))),
                new("Swamp", () => SceneLookAs(new Vector3(-1717, 308, -2624), new Vector3(39, 39, 0))),
                new("Tree", () => SceneLookAs(new Vector3(-1172, 137, -2822), new Vector3(31, 104, 0))),
                DropdownEntree.Separator(),
                new("Burnt Village", () => SceneLookAs(new Vector3(-181, 328, -2982), new Vector3(51, 98, 0))),
                new("Capital City", () => SceneLookAs(new Vector3(367, 486, -3597), new Vector3(34, 134, 0))),
                new("Highlands Stronghold", () => SceneLookAs(new Vector3(341, 443, -3176), new Vector3(37, 65, 0))),
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
        
        static bool IsCampaignOpen() {
            if (Application.isPlaying) {
                return SceneManager.GetActiveScene().name == "CampaignMap";
            } else {
                return SubdividedSceneTracker.TryGet(out _);
            }
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