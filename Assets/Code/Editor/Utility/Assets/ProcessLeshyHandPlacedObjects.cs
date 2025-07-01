using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.TG.LeshyRenderer;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Utility.Assets {
    public class ProcessLeshyHandPlacedObjects : SceneProcessor {
        public override int callbackOrder => ProcessSceneOrder.LeshySceneObject;
        public override bool canProcessSceneInIsolation => true;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            List<LeshyObjectSettings> leshyObjects = GameObjects.FindComponentsByTypeInScene<LeshyObjectSettings>(scene, true);
            foreach (var leshyObject in leshyObjects) {
                Object.DestroyImmediate(leshyObject.gameObject);
            }
        }
    }
}