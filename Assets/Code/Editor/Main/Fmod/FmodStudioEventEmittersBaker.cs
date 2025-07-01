using System.Collections.Generic;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using FMODUnity;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Main.Fmod {
    public class FmodStudioEventEmittersBaker : SceneProcessor {
        public override int callbackOrder => 0;
        public override bool canProcessSceneInIsolation => true;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            // List<StudioEventEmitter> eventEmitters = GameObjects.FindComponentsByTypeInScene<StudioEventEmitter>(scene, true, 32);
            // for (int i = 0; i < eventEmitters.Count; i++) {
            //     var eventEmitter = eventEmitters[i];
            //     eventEmitter.EDITOR_IsStatic = eventEmitter.gameObject.isStatic;
            //     EditorUtility.SetDirty(eventEmitter);
            // }
        }
    }
}