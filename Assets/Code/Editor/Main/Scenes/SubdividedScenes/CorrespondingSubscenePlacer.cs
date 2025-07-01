using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Utility.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Main.Scenes.SubdividedScenes {
    public static class CorrespondingSubscenePlacer {

        class ChooseSubsceneWindow : EditorWindow {
            GameObject _go;
            List<(string, SceneReference)> _scenesHere;
            List<(string, SceneReference)> _nearbyScenes;

            (string, SceneReference)? _result;
            
            public static async UniTask<(string, SceneReference)?> ChooseFor(GameObject go, List<(string, SceneReference)> scenesHere, List<(string, SceneReference)> nearbyScenes) {
                var window = EditorWindow.GetWindow<ChooseSubsceneWindow>();
                window._go = go;
                window._scenesHere = scenesHere;
                window._nearbyScenes = nearbyScenes;
                window._result = null;
                window.titleContent = new GUIContent("Choose subscene");
                window.ShowPopup();
                await UniTask.WaitUntil(() => window == null);
                return window._result;
            }

            void OnGUI() {
                EditorGUILayout.ObjectField("Choose subscene for", _go, typeof(GameObject), true);
                
                using (new ColorGUIScope(Color.green)) {
                    foreach (var (path, sceneRef) in _scenesHere) {
                        DrawButtonFor(path, sceneRef);
                    }
                    GUILayout.Space(5);
                }
                using (new ColorGUIScope(Color.yellow)) {
                    foreach (var (path, sceneRef) in _nearbyScenes) {
                        DrawButtonFor(path, sceneRef);
                    }
                    GUILayout.Space(5);
                }
                using (new ColorGUIScope(Color.red)) {
                    if (GUILayout.Button("None")) {
                        _result = null;
                        Close();
                    }
                }
            }

            void DrawButtonFor(string scenePath, SceneReference sceneRef) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(scenePath)) {
                    _result = (scenePath, sceneRef);
                    Close();
                }

                if (GUILayout.Button("Load", GUILayout.Width(50))) {
                    new SceneReference.EditorAccess(sceneRef).LoadScene();
                }
                if (GUILayout.Button("Unload", GUILayout.Width(50))) {
                    new SceneReference.EditorAccess(sceneRef).UnloadScene(true);
                }

                GUILayout.EndHorizontal();
            }
        }
        static async UniTask PlaceOnCorrespondingSubscene(GameObject go) {
            List<(string, SceneReference)> sceneInBounds = new();
            List<(string, SceneReference)> nearbyScenes = new();
            SubsceneEditorData.GroupSubscenesByProximity(go.transform.position, sceneInBounds, 10, nearbyScenes, 250);

            var result = await ChooseSubsceneWindow.ChooseFor(go, sceneInBounds, nearbyScenes);
            if (result is ({ } scenePath, { } sceneRef)) {
                MoveGameObjectToScene(go, sceneRef);
                Log.Important?.Info($"[SubscenePlacer] Moving {go.name} to chosen scene {scenePath}", go);
            } else {
                Log.Important?.Info($"[SubscenePlacer] No subscene chosen for {go.name}", go);
            }
        }

        static void MoveGameObjectToScene(GameObject go, SceneReference sceneRef) {
            var scene = new SceneReference.EditorAccess(sceneRef).LoadScene();
            go.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        [MenuItem("GameObject/Subscenes/Place on corresponding subscene")]
        static void PlaceGameObjectOnCorrespondingSubscene(MenuCommand menuCommand) {
            GameObject go = menuCommand.context as GameObject;
            if (go != null) {
                PlaceOnCorrespondingSubscene(go).Forget();
            }
        }
        
        [MenuItem("GameObject/Subscenes/Place children on corresponding subscene")]
        static void PlaceChildrenOnCorrespondingSubscene(MenuCommand menuCommand) {
            GameObject go = menuCommand.context as GameObject;
            if (go != null) {
                var parent = go.transform;
                GameObject[] children = new GameObject[parent.childCount];
                for (int i = 0; i < children.Length; i++) {
                    children[i] = parent.GetChild(i).gameObject;
                }
                PlaceGameObjectsOnCorrespondingSubscene(children).Forget();
            }
        }
        
        static async UniTaskVoid PlaceGameObjectsOnCorrespondingSubscene(params GameObject[] gos) {
            foreach (var go in gos) {
                await PlaceOnCorrespondingSubscene(go);
            }
        }
    }
}