using Awaken.ECS.Editor.DrakeRenderer;
using Cysharp.Threading.Tasks;
using Unity.Entities;
using UnityEditor;

namespace Awaken.ECS.Editor {
    public static class EditorAwakenEcsBootstrap {
        [InitializeOnLoadMethod]
        static void InitializeEditor() {
            EditorApplication.playModeStateChanged += OnPlaymodeChanged;
            
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                // Need to wait a bit because Unity need to fully load scene at first
                CreateEditorWorld(true).Forget();
            }
        }
        
        static void OnPlaymodeChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.EnteredEditMode) {
                CreateEditorWorld().Forget();
            }
        }
        static async UniTaskVoid CreateEditorWorld(bool wait = false) {
            if (World.DefaultGameObjectInjectionWorld == null) {
                DefaultWorldInitialization.DefaultLazyEditModeInitialize();
            }

            AwakenEcsBootstrap.CreateEcsPlayerLoop();

            if (wait) {
                await UniTask.DelayFrame(1);
            }
            DrakeRendererManagerEditor.AfterBootstrap();
        }
    }
}
