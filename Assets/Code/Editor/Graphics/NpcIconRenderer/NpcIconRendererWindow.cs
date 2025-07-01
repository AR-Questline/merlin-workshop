using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Setup;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable InconsistentNaming

namespace Awaken.TG.Editor.Graphics.NpcIconRenderer {
    public class NpcIconRendererWindow : OdinEditorWindow {
        [ShowInInspector, ShowIf(nameof(IsPlayMode))]
        public NpcIconRendererSettings settings;

        [MenuItem("ArtTools/NPC Icon Renderer")]
        internal static void ShowEditor() {
            var window = EditorWindow.GetWindow<NpcIconRendererWindow>();
            window.Show();
        }

        // === Lifetime
        protected override void Initialize() {
            settings = NpcIconRendererSettings.instance;
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        protected override void OnDestroy() {
            EditorApplication.playModeStateChanged -= PlayModeChanged;
        }

        [Button, ShowIf(nameof(CanEnterPlayMode))]
        void Start() {
            EditorApplication.EnterPlaymode();
        }

        [Button, ShowIf(nameof(IsPlayMode)), PropertyOrder(-1)]
        void RenderAll() {
            RenderAllAsync().Forget();
        }

        async UniTask RenderAllAsync() {
            NpcIconRenderingUtils.locationIconRenderReady += OnLocationReadyToRender;
            NpcIconRenderingUtils.iconRenderComplete += OnRenderComplete;
            bool renderComplete;

            foreach (var entry in settings.entries) {
                renderComplete = false;
                NpcIconRenderingUtils.PreviewEntry(entry);
                await UniTask.WaitUntil(() => renderComplete);
            }

            return;

            void OnLocationReadyToRender(LocationTemplate locationTemplate) {
                NpcIconRenderingUtils.RenderAndAssignIcon(locationTemplate);
            }

            void OnRenderComplete() {
                renderComplete = true;
            }
        }

        // === Operations
        void PlayModeChanged(PlayModeStateChange playModeStateChange) {
            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode) {
                NpcIconRenderingUtils.CleanAll();
                EditorApplication.update -= Repaint;
            }

            if (playModeStateChange == PlayModeStateChange.EnteredPlayMode) {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            if (!IsRightSceneActive()) {
                return;
            }

            if (playModeStateChange == PlayModeStateChange.EnteredPlayMode) {
                EditorApplication.update -= Repaint;
                EditorApplication.update += Repaint;
            }
        }

        async void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (scene.name != NpcIconRenderingUtils.Scene) {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;

            await UniTask.WaitUntil(() => Hero.Current != null);

            // Set camera to clear color
            NpcIconRenderingUtils.TryUpdateBackgroundColor(settings.backgroundColor);
        }

        bool IsPlayMode() => Application.isPlaying;
        bool CanEnterPlayMode() => IsRightSceneActive() && !Application.isPlaying;
        bool IsRightSceneActive() => SceneManager.GetActiveScene().name == NpcIconRenderingUtils.Scene;
    }

    [InitializeOnLoad]
    public static class NpcIconRendererStartup {
        static NpcIconRendererStartup() {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode) {
            if (scene.name == NpcIconRenderingUtils.Scene) {
                if (!EditorWindow.HasOpenInstances<NpcIconRendererWindow>()) {
                    NpcIconRendererWindow.ShowEditor();
                }
            }
        }
    }
}