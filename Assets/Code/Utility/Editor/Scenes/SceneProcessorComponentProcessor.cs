using Awaken.Utility.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.Utility.Editor.Scenes {
    public class SceneProcessorComponentProcessor : SceneProcessor {
        public override int callbackOrder => 0;
        public override bool canProcessSceneInIsolation => true;

        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            // In build, build tools will process all pre-processor components and destroy them, so SceneProcessorComponents won't be pre-processors
            foreach (var processor in GameObjects.GameObjects.FindComponentsByTypeInScene<SceneProcessorComponent>(scene, true)) {
                processor.Process();
                Object.DestroyImmediate(processor);
            }
        }
    }
}