using Awaken.CommonInterfaces;
using System.Collections.Generic;
using Awaken.TG.Graphics.Culling;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Awaken.TG.Editor.Graphics.Culling {
    [Preserve]
    public class DistanceCullingSceneBuilder : SceneProcessor {
        public override int callbackOrder => ProcessSceneOrder.DistanceCulling;
        public override bool canProcessSceneInIsolation => true;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            List<DistanceCuller> cullers = GameObjects.FindComponentsByTypeInScene<DistanceCuller>(scene, false, size: 2);
            if (cullers.IsEmpty()) {
                return;
            }
            if (cullers.Count > 1) {
                Log.Important?.Warning($"Scene [{scene.name}] contains more than one {nameof(DistanceCuller)}.");
                for (int i = 1; i < cullers.Count; i++) {
                    Object.DestroyImmediate(cullers[i]);
                }
            }
            
            var culler = cullers[0];
            culler.EDITOR_FillFromScene(false);
        }
    }
}
