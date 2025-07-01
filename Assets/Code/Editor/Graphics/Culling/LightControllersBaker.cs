using System.Collections.Generic;
using Awaken.TG.Graphics.VFX;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Graphics.Culling {
    public class LightControllersBaker : SceneProcessor {
        public override int callbackOrder => 0;
        public override bool canProcessSceneInIsolation => true;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            List<LightController> lightControllers = GameObjects.FindComponentsByTypeInScene<LightController>(scene, true, 32);
            for (int i = 0; i < lightControllers.Count; i++) {
                var lightController = lightControllers[i];
                var lightControllerAccess =  new LightController.EditorAccess(lightController);

                lightControllerAccess.isStatic = lightControllerAccess.forceStaticIfInitiallyOnScene || lightController.gameObject.isStatic;
                lightControllerAccess.lightController.BakeNativeIntensity();
                EditorUtility.SetDirty(lightController);
            }
        }
    }
}