using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Main.Scenes.SubdividedScenes;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.Utility.Maths;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Awaken.TG.Editor.ToolbarTools {
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class SubscenesToolbarButton : EditorToolbarDropdown {
        public const string ID = "X/4";

        public SubscenesToolbarButton() {
            text = "Subscenes";
            tooltip = "List subscenes by proximity";
            clicked += OnClick;
            SubdividedSceneTracker.OnSubdividedSceneChanged += RefreshEnabled;
        }

        void RefreshEnabled(SubdividedScene scene) {
            SetEnabled(scene != null);
        }

        void OnClick() {
            if (!SubdividedSceneTracker.TryGet(out var scene)) {
                return;
            }

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) {
                return;
            }
            
            var camera = sceneView.camera;
            if (camera == null) {
                return;
            }

            List<(string, SceneReference)> sceneInBounds = new();
            List<(string, SceneReference)> nearbyScenes = new();
            List<(string, SceneReference)> farScenes = new();
            List<(string, SceneReference)> scenesWithoutGeometry = new();
            SubsceneEditorData.GroupSubscenesByProximity(camera.transform.position, sceneInBounds, 5, nearbyScenes, 50, farScenes, scenesWithoutGeometry);

            var menu = new GenericMenu();
            if (sceneInBounds.Count > 0) {
                foreach (var (path, sceneRef) in sceneInBounds) {
                    AddSceneItem("Scenes Here", path, sceneRef);
                }
            } else {
                menu.AddDisabledItem(new GUIContent("Scenes Here/No scenes here"));
            }

            if (nearbyScenes.Count > 0) {
                foreach (var (path, sceneRef) in nearbyScenes) {
                    AddSceneItem("Nearby Scenes", path, sceneRef);
                }
            } else {
                menu.AddDisabledItem(new GUIContent("Nearby Scenes/No nearby scenes"));
            }
            
            if (farScenes.Count > 0) {
                foreach (var (path, sceneRef) in farScenes) {
                    AddSceneItem("Far Scenes", path, sceneRef);
                }
            } else {
                menu.AddDisabledItem(new GUIContent("Far Scenes/No far scenes"));
            }
            
            if (scenesWithoutGeometry.Count > 0) {
                foreach (var (path, sceneRef) in scenesWithoutGeometry) {
                    AddSceneItem("Scenes Without Geometry", path, sceneRef);
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Open Scene Manager"), false, SubsceneEditorWindow.ShowWindow);
            
            var rect = worldBound;
            menu.DropDown(new Rect(new Vector2(rect.xMin, rect.yMax + 5), Vector2.zero));
            
            void AddSceneItem(string prefix, string path, SceneReference sceneRef) {
                if (sceneRef.LoadedScene.IsValid()) {
                    menu.AddItem(new GUIContent($"{prefix}/{path}"), true, () => new SceneReference.EditorAccess(sceneRef).UnloadScene(true));
                } else {
                    menu.AddItem(new GUIContent($"{prefix}/{path}"), false, () => new SceneReference.EditorAccess(sceneRef).LoadScene());
                }
            }
        }
    }
}