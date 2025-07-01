using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.TG.Graphics.Culling;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Awaken.TG.Editor.Graphics.Culling {
    [Preserve]
    public class DecalsCullingSceneBuilder : SceneProcessor {
        public override int callbackOrder => ProcessSceneOrder.DistanceCulling + 1;
        public override bool canProcessSceneInIsolation => true;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
#if SCENES_PROCESSED
            return;
#endif
            List<StaticDecalsCuller> cullers = GameObjects.FindComponentsByTypeInScene<StaticDecalsCuller>(scene, true, size: 2);
            if (cullers.IsEmpty()) {
                if (EditorApplication.isPlayingOrWillChangePlaymode) {
                    var mapSceneRoot = GameObjects.FindComponentByTypeInScene<IMapScene>(scene, false);
                    var decalCuller = new GameObject(nameof(StaticDecalsCuller));
                    SceneManager.MoveGameObjectToScene(decalCuller, scene);

                    if (mapSceneRoot is Component component) {
                        decalCuller.transform.SetParent(component.transform);
                    }

                    decalCuller.SetActive(false);
                    var cullerComponent = decalCuller.AddComponent<StaticDecalsCuller>();
                    cullerComponent.enabled = false;
                    decalCuller.SetActive(true);
                    EditorUtility.SetDirty(decalCuller);
                    cullers.Add(cullerComponent);
                } else {
                    return;
                }
            }
            if (cullers.Count > 1) {
                Log.Important?.Warning($"Scene [{scene.name}] contains more than one {nameof(StaticDecalsCuller)}.");
                for (int i = 1; i < cullers.Count; i++) {
                    Object.DestroyImmediate(cullers[i]);
                }
            }
            var culler = cullers[0];
            culler.EDITOR_FillFromScene();
        }
    }
}