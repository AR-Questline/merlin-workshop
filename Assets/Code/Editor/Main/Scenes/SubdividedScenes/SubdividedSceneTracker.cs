using System;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Main.Scenes.SubdividedScenes {
    public static class SubdividedSceneTracker {
        static bool s_hadMotherScene;
        static SubdividedScene s_motherScene;
        static string s_errorMessage;
        
        public static event Action<SubdividedScene> OnSubdividedSceneChanged;

        [InitializeOnLoadMethod]
        static void Init() {
            EditorSceneManager.sceneOpened += static (_, _) => RefreshMotherScene();
            EditorSceneManager.sceneClosed += static _ => RefreshMotherScene();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += () => {
                if (s_motherScene == null) {
                    RefreshMotherScene();
                }
            };
        }

        static void OnPlayModeStateChanged(PlayModeStateChange obj) {
            if (obj == PlayModeStateChange.EnteredEditMode) {
                RefreshMotherScene();
            }
        }
        
        static void RefreshMotherScene() {
            var previousMotherScene = s_motherScene;
            
            var scenes = Object.FindObjectsByType<SubdividedScene>(FindObjectsSortMode.None);
            if (scenes.Length == 0) {
                s_motherScene = null;
                s_errorMessage = "No subdivided scenes";
            } else if (scenes.Length == 1) {
                s_motherScene = scenes[0];
                s_errorMessage = null;
            } else {
                s_motherScene = null;
                s_errorMessage = "Multiple subdivided scenes";
            }

            if (s_hadMotherScene && s_motherScene == null) {
                s_hadMotherScene = false;
                OnSubdividedSceneChanged?.Invoke(s_motherScene);
            } else if (!s_hadMotherScene && s_motherScene != null) {
                s_hadMotherScene = true;
                OnSubdividedSceneChanged?.Invoke(s_motherScene);
            } else if (previousMotherScene != s_motherScene) {
                OnSubdividedSceneChanged?.Invoke(s_motherScene);
            }
        }

        public static bool TryGet(out SubdividedScene subdividedScene, out string errorMessage) {
            subdividedScene = s_motherScene;
            errorMessage = s_errorMessage;
            return s_motherScene;
        }

        public static bool TryGet(out SubdividedScene subdividedScene) {
            return TryGet(out subdividedScene, out _);
        }
    }
}