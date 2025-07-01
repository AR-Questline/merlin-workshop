using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Awaken.Utility.Editor.Assets {
    /// <summary>
    /// This scene processor will unfold/flatten scene hierarchy, as deep hierarchies are slow.
    /// It will find all <see cref="IFutureRootAfterUnfoldMarker"/> and make them roots.
    /// So if there is need you can add more components whose Transforms should act as roots in runtime.
    /// </summary>
    [Preserve]
    public class SceneUnfold : SceneProcessor {
        public override int callbackOrder => ProcessSceneOrder.SceneUnfold;
        public override bool canProcessSceneInIsolation => true;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            ProcessFutureRoots(scene);
            ProcessEditorOnlyTransforms(scene);
            ProcessEditorOnlyScripts(scene);
            ProcessEmptyColliders(scene);
        }

        void ProcessFutureRoots(Scene scene) {
            var roots = scene.GetRootGameObjects();
            var futureRoots = new List<GameObject>();
            for (int i = 0; i < roots.Length; ++i) {
                futureRoots.AddRange(roots[i]
                    .GetComponentsInChildren<IFutureRootAfterUnfoldMarker>(true)
                    .Select(static m => m.GameObject)
                    .Distinct()
                    .OrderBy(HierarchySortDepth));
            }

            for (var i = 0; i < futureRoots.Count; i++) {
                var futureRoot = futureRoots[i];
                var futureRootTransform = futureRoot.transform;
                RootTransform(futureRootTransform);
            }
        }

        static void ProcessEditorOnlyTransforms(Scene scene) {
            var editorOnlyTransforms = GameObjects.GameObjects.FindComponentsByTypeInScene<IEditorOnlyTransform>(scene, false);

            foreach (var editorOnlyTransform in editorOnlyTransforms) {
                if (editorOnlyTransform.PreserveChildren) {
                    AscentChildren(editorOnlyTransform.transform);
                }

                if (editorOnlyTransform.gameObject.GetComponentCount() == 2) {
                    Object.DestroyImmediate(editorOnlyTransform.gameObject);
                } else if (editorOnlyTransform is MonoBehaviour monoBehaviour) {
                    Object.DestroyImmediate(monoBehaviour);
                }
            }
        }

        void ProcessEditorOnlyScripts(Scene scene) {
            var editorOnlyMonoBehaviours = GameObjects.GameObjects.FindComponentsByTypeInScene<IEditorOnlyMonoBehaviour>(scene, false);

            foreach (var editorOnlyTransform in editorOnlyMonoBehaviours) {
                if (editorOnlyTransform is MonoBehaviour monoBehaviour) {
                    if (monoBehaviour.transform.IsLeafSingleComponent()) {
                        Object.DestroyImmediate(monoBehaviour.gameObject);
                    } else {
                        Object.DestroyImmediate(monoBehaviour);
                    }
                }
            }
        }

        void ProcessEmptyColliders(Scene scene) {
            var rootGameObjects = scene.GetRootGameObjects();
            foreach (var rootGameObject in rootGameObjects) {
                ProcessEmptyColliders(rootGameObject);
            }
        }

        static void ProcessEmptyColliders(GameObject parent) {
            var gameObjectName = parent.name;
            var isCollidersRoot = gameObjectName is "Collider" or "Colliders";
            if (isCollidersRoot) {
                AscentChildren(parent.transform);
                if (parent.GetComponentCount() == 1) {
                    Object.DestroyImmediate(parent.gameObject);
                }
            } else {
                var parentTransform = parent.transform;
                for (int i = parentTransform.childCount - 1; i >= 0; i--) {
                    ProcessEmptyColliders(parentTransform.GetChild(i).gameObject);
                }
            }
        }

        int HierarchySortDepth(GameObject subject) {
            var transform = subject.transform;
            var depth = 0;
            while (transform != null) {
                depth++;
                transform = transform.parent;
            }
            return 1000-depth;
        }

        static void AscentChildren(Transform transform) {
            var reparent = transform.parent;
            for (int i = transform.childCount - 1; i >= 0; i--) {
                transform.GetChild(i).SetParent(reparent, true);
            }
        }

        static void RootTransform(Transform futureRootTransform) {
            var wasActive = futureRootTransform.gameObject.activeInHierarchy;
            var siblingIndex = futureRootTransform.root.GetSiblingIndex();
            siblingIndex++;
            futureRootTransform.SetParent(null, true);
            futureRootTransform.SetSiblingIndex(siblingIndex);
            futureRootTransform.gameObject.SetActive(wasActive);
            EditorUtility.SetDirty(futureRootTransform.gameObject);
        }
    }
}
