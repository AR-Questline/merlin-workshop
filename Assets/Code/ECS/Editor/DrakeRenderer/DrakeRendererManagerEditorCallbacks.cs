using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public abstract class DrakeRendererManagerEditorCallbacks {
        static GameObject s_prefabStageRoot;
        static bool s_wasHiddenContext;

        public static event Action<PrefabStage, bool> PrefabStageOpened;
        public static event Action<PrefabStage, bool> PrefabStageChanged;
        public static event Action PrefabStageClosed;
        public static event Action<PrefabStage, bool> PrefabStageContextChanged;

        [InitializeOnLoadMethod]
        static void InitializeEditor() {
            SceneView.duringSceneGui += OnSceneView;
        }

        static void OnSceneView(SceneView sceneView) {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }
            CheckPrefabStage();
        }

        static void CheckPrefabStage() {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null) {
                if (ReferenceEquals(s_prefabStageRoot, null)) {
                    return;
                }
                s_prefabStageRoot = null;
                PrefabStageClosed?.Invoke();
                return;
            }

            if (s_prefabStageRoot == prefabStage.prefabContentsRoot) {
                var shouldBeHidden = CoreUtils.IsSceneViewPrefabStageContextHidden();
                if (s_wasHiddenContext != shouldBeHidden) {
                    s_wasHiddenContext = shouldBeHidden;
                    PrefabStageContextChanged?.Invoke(prefabStage, shouldBeHidden);
                }
                return;
            }

            if (ReferenceEquals(s_prefabStageRoot, null)) {
                s_prefabStageRoot = prefabStage.prefabContentsRoot;
                var shouldBeHidden = CoreUtils.IsSceneViewPrefabStageContextHidden();
                s_wasHiddenContext = shouldBeHidden;
                PrefabStageOpened?.Invoke(prefabStage, shouldBeHidden);
            } else {
                s_prefabStageRoot = prefabStage.prefabContentsRoot;
                PrefabStageChanged?.Invoke(prefabStage, s_wasHiddenContext);
            }
        }
    }
}
