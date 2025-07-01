using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace Awaken.Utility.Debugging {
    public class ScenesDebuggingWindow : UGUIWindowDisplay<ScenesDebuggingWindow> {
        public static readonly List<ButtonDefinition> DefinedButtons = new();
        public static readonly List<Action<Scene?>> AdditionalHeaderDrawers = new();

        static OnDemandCache<int, bool> s_MeshRenderersEnabled = new(_ => true);
        static OnDemandCache<int, bool> s_SkinnedRenderersEnabled = new(_ => true);
        static OnDemandCache<int, bool> s_LightsEnabled = new(_ => true);
        static OnDemandCache<int, bool> s_VFXsEnabled = new(_ => true);
        static OnDemandCache<int, bool> s_DecalsEnabled = new(_ => true);
        static OnDemandCache<int, bool> s_TerrainsEnabled = new(_ => true);
        static OnDemandCache<int, bool> s_OceansEnabled = new(_ => true);
        static OnDemandCache<int, bool> s_WatersEnabled = new(_ => true);
        static OnDemandCache<int, bool> s_ShadowsEnabled = new(_ => true);

        bool _drawAdditionalHeaders;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterButtons() {
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl mesh renderers", ToggleMeshRenderers, true, true, GetButtonColor(s_MeshRenderersEnabled)));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl shadows", ToggleShadows, true, true, GetButtonColor(s_ShadowsEnabled)));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl skinned renderers", ToggleSkinnedRenderers, true, true, GetButtonColor(s_SkinnedRenderersEnabled)));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl lights", ToggleLights, true, true, GetButtonColor(s_LightsEnabled)));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl vfxs", ToggleVfxs, true, true, GetButtonColor(s_VFXsEnabled)));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl decals", ToggleDecals, true, true, GetButtonColor(s_DecalsEnabled)));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl terrains", ToggleTerrains, false, true, GetButtonColor(s_TerrainsEnabled)));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl ocean", ToggleOcean, false, true, GetButtonColor(s_OceansEnabled)));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl waters", ToggleWaters, false, true, GetButtonColor(s_WatersEnabled)));
        }

        protected override void DrawWindow() {
            _drawAdditionalHeaders = GUILayout.Toggle(_drawAdditionalHeaders, "Draw additional headers");

            DrawBulkOperations();

            var loadedScenes = SceneManager.loadedSceneCount;
            for (int i = 0; i < loadedScenes; i++) {
                var scene = SceneManager.GetSceneAt(i);
                DrawScene(scene);
            }
        }

        void DrawBulkOperations() {
            GUILayout.BeginVertical("box");

            GUILayout.Label("Bulk operations:");
            if (_drawAdditionalHeaders) {
                foreach (var drawer in AdditionalHeaderDrawers) {
                    drawer(null);
                }
            }

            GUILayout.BeginHorizontal();

            for (int buttonIndex = 0; buttonIndex < DefinedButtons.Count; buttonIndex++) {
                // New line every 5 buttons
                if (buttonIndex % 5 == 0) {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                ButtonDefinition buttonDefinition = DefinedButtons[buttonIndex];
                if (buttonDefinition.BulkEnabled) {
                    if (DrawButton(buttonDefinition, default)) {
                        var loadedScenes = SceneManager.loadedSceneCount;
                        for (int i = 0; i < loadedScenes; i++) {
                            var scene = SceneManager.GetSceneAt(i);
                            buttonDefinition.OnClick(scene);
                        }
                    }
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        void DrawScene(Scene scene) {
            var sceneName = scene.name;

            if (!SearchContext.HasSearchInterest(sceneName)) {
                return;
            }

            GUILayout.BeginVertical("box");

            GUILayout.Label($"Scene {scene.name}");
            if (_drawAdditionalHeaders) {
                foreach (var drawer in AdditionalHeaderDrawers) {
                    drawer(scene);
                }
            }

            GUILayout.BeginHorizontal();

            for (int buttonIndex = 0; buttonIndex < DefinedButtons.Count; buttonIndex++) {
                // New line every 5 buttons
                if (buttonIndex % 5 == 0) {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                ButtonDefinition buttonDefinition = DefinedButtons[buttonIndex];
                if (buttonDefinition.SingleEnabled) {
                    if (DrawButton(buttonDefinition, scene)) {
                        buttonDefinition.OnClick(scene);
                    }
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        static void ToggleMeshRenderers(Scene scene) {
            Toggle<MeshRenderer>(scene, s_MeshRenderersEnabled, (mr, enabled) => mr.enabled = enabled);
        }

        static void ToggleSkinnedRenderers(Scene scene) {
            Toggle<SkinnedMeshRenderer>(scene, s_SkinnedRenderersEnabled, (smr, enabled) => smr.enabled = enabled);
        }

        static void ToggleLights(Scene scene) {
            Toggle<Light>(scene, s_LightsEnabled, (light, enabled) => {
                if (light.type != LightType.Directional) {
                    light.enabled = enabled;
                }
            });
        }

        static void ToggleVfxs(Scene scene) {
            Toggle<VisualEffect>(scene, s_VFXsEnabled, (visualEffect, enabled) => visualEffect.enabled = enabled);
        }

        static void ToggleDecals(Scene scene) {
            Toggle<DecalProjector>(scene, s_DecalsEnabled, (decal, enabled) => decal.enabled = enabled);
        }

        static void ToggleTerrains(Scene scene) {
            Toggle<Terrain>(scene, s_TerrainsEnabled, (terrain, enabled) => terrain.enabled = enabled);
        }

        static void ToggleOcean(Scene scene) {
            Toggle<WaterSurface>(scene, s_OceansEnabled, (water, enabled) => {
                if (water.geometryType == WaterGeometryType.Infinite) {
                    water.enabled = enabled;
                }
            });
        }

        static void ToggleWaters(Scene scene) {
            Toggle<WaterSurface>(scene, s_WatersEnabled, (water, enabled) => {
                if (water.geometryType != WaterGeometryType.Infinite) {
                    water.enabled = enabled;
                }
            });
        }

        static void ToggleShadows(Scene scene) {
            Toggle<MeshRenderer>(scene, s_ShadowsEnabled, (mr, enabled) => {
                if (enabled && mr.shadowCastingMode == ShadowCastingMode.Off) {
                    mr.shadowCastingMode = ShadowCastingMode.On;
                }else if (!enabled && mr.shadowCastingMode == ShadowCastingMode.On) {
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }
            });
        }

        static void Toggle<T>(Scene scene, OnDemandCache<int, bool> state, Action<T, bool> action) {
            var rootGameObjects = scene.GetRootGameObjects();
            var nextState = !state[scene.handle];
            foreach (var rootGameObject in rootGameObjects) {
                var targets = rootGameObject.GetComponentsInChildren<T>();
                foreach (var target in targets) {
                    action(target, nextState);
                }
            }
            state[scene.handle] = nextState;
        }

        static bool DrawButton(in ButtonDefinition buttonDefinition, in Scene scene) {
            var oldColor = GUI.backgroundColor;
            if (buttonDefinition.ButtonColor != null) {
                GUI.backgroundColor = buttonDefinition.ButtonColor(scene);
            }

            var result = GUILayout.Button(buttonDefinition.Name);

            GUI.backgroundColor = oldColor;
            return result;
        }

        static Func<Scene, Color> GetButtonColor(OnDemandCache<int, bool> states) {
            return scene => {
                if (scene.handle == 0) {
                    var allEnabled = states.Values.All(e => e);
                    if (allEnabled) {
                        return Color.yellow;
                    }
                    var allDisabled = states.Values.All(e => !e);
                    if (allDisabled) {
                        return Color.blue;
                    }
                    return Color.cyan;
                }

                return states[scene.handle] ? Color.yellow : Color.blue;
            };
        }

        public readonly struct ButtonDefinition {
            public readonly string Name;
            public readonly Action<Scene> OnClick;
            public readonly bool SingleEnabled;
            public readonly bool BulkEnabled;
            public readonly Func<Scene, Color> ButtonColor;

            public ButtonDefinition(string name, Action<Scene> onClick, bool singleEnabled, bool bulkEnabled, Func<Scene, Color> buttonColor = null) {
                Name = name;
                OnClick = onClick;
                SingleEnabled = singleEnabled;
                BulkEnabled = bulkEnabled;
                ButtonColor = buttonColor;
            }
        }

        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowScenesDebugWindow() {
            ScenesDebuggingWindow.Toggle(UGUIWindowUtils.WindowPosition.TopLeft);
        }

        static bool IsDebugWindowShown() => ScenesDebuggingWindow.IsShown;
    }
}
