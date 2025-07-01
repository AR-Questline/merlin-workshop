using Awaken.TG.Graphics;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Graphics {
    public class ToggleWithRandomDelayBehaviourBaker : SceneProcessor {
        public override int callbackOrder => 0;
        public override bool canProcessSceneInIsolation => false;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            var behaviours = GameObjects.FindComponentsByTypeInScene<ToggleWithRandomDelayBehaviour>(scene, true, 24);
            var manager = GameObjects.FindComponentByTypeInScene<ToggleWithRandomDelayBehaviourManager>(scene, true);
            if (behaviours.Count == 0) {
                if (manager != null) {
                    GameObjects.DestroySafely(manager);
                }
                return;
            }
            if (manager == null) {
                var managerGO = new GameObject($"{nameof(ToggleWithRandomDelayBehaviourManager)}_{scene.name}", typeof(ToggleWithRandomDelayBehaviourManager));
                SceneManager.MoveGameObjectToScene(managerGO, scene);
                manager = managerGO.GetComponent<ToggleWithRandomDelayBehaviourManager>();
            }
            manager.EDITOR_Initialize(behaviours.Count);
            for (int i = 0; i < behaviours.Count; i++) {
                var behaviour = behaviours[i];
                manager.EDITOR_Add(behaviour);
                GameObjects.DestroySafely(behaviour);
            }
        }
    }
}