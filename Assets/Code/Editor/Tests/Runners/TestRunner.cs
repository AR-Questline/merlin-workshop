using System;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Code.Editor.Tests.Runners {
    public static class TestRunner {
        static string s_previousScenePath;
        static EnterPlayModeOptions s_playModeOptions;
        static PlayModeTest s_currentTest;
        public static event Action OnTestEnded;
        public static event Action OnInterrupted;

        public static void StartTest<T>(object data = null) where T : PlayModeTest {
            s_playModeOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            
            s_previousScenePath = SceneManager.GetActiveScene().path;
            Scene testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            testScene.name = "Test Scene";

            EditorApplication.playModeStateChanged += PlayModeStateChangedOnTest;

            GameObject testObject = new GameObject("Test");
            s_currentTest = testObject.AddComponent<T>();
            s_currentTest.data = data;
            
            EditorApplication.ExecuteMenuItem("Edit/Play");
        }

        public static void OnTestEnd() {
            s_currentTest = null;
            EditorApplication.ExecuteMenuItem("Edit/Play");
        }

        static void PlayModeStateChangedOnTest(PlayModeStateChange playModeStateChange) {
            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode) {
                if (s_currentTest != null) {
                    s_currentTest.AfterTestEnded();
                    Log.Important?.Info("Test interrupted by user");
                    s_currentTest = null;
                    OnInterrupted?.Invoke();
                }
            }

            if (playModeStateChange == PlayModeStateChange.EnteredEditMode) {
                EditorApplication.playModeStateChanged -= PlayModeStateChangedOnTest;
                if (s_previousScenePath != null) {
                    EditorSceneManager.OpenScene(s_previousScenePath);
                    s_previousScenePath = null;
                }
                EditorSettings.enterPlayModeOptions = s_playModeOptions;

                var endTestCallback = OnTestEnded;
                OnTestEnded = null;
                OnInterrupted = null;
                endTestCallback?.Invoke();
                
            }
        }
    }
}
