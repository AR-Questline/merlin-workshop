using Awaken.CommonInterfaces;
using Awaken.TG.Main.Locations.Regrowables;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.ScenesUtilities {
    public class RegrowablesSceneMerger : SceneProcessor {
        public override int callbackOrder => ProcessSceneOrder.RegrowablesMerge;
        public override bool canProcessSceneInIsolation => false;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            var regrowableSpecs = GameObjects.FindComponentsByTypeInScene<VegetationRegrowableSpec>(scene, false);

            if (regrowableSpecs.Count == 0) {
                return;
            }

            var mergedGameObject = new GameObject("MergedRegrowables");
            EditorSceneManager.MoveGameObjectToScene(mergedGameObject, scene);

            mergedGameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            mergedGameObject.transform.localScale = Vector3.one;

            mergedGameObject.SetActive(false);
            MergedVegetationRegrowableSpec.Create(regrowableSpecs, mergedGameObject);

            foreach (var spec in regrowableSpecs) {
                if (spec.transform.IsLeafSingleComponent()) {
                    Object.DestroyImmediate(spec.gameObject);
                } else {
                    Object.DestroyImmediate(spec);
                }
            }
            mergedGameObject.SetActive(true);
        }
    }
}
