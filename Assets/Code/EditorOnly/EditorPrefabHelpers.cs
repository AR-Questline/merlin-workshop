#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics.VisualsPickerTool;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.EditorOnly {
    public enum SourceAssetSearchMode {
        Exact = 0,
        InParent = 1,
        VisualPickers = 2
    }
    /// <summary>
    /// General purpose helpers for working with prefabs. With PrefabUtility documentation
    /// </summary>
    public static class EditorPrefabHelpers {
        #region PrefabUtility documentation
        /* 
         *  Root -> the last non null parent of the current context. (An added as override prefab counts as a separate context)
         *  Instance -> An instance of a prefab.
         *              The only case a prefab is not an instance is in scripting editing of prefab. The root object is a prefab but not an instance
         *  Context -> The "world" that you are in atm. Can be the scene, a prefab asset, prefab editing stage or a prefab added as an override.
         * 
         *  
         *  - PrefabUtility.IsAnyPrefabInstanceRoot()
         *      A grey box in hierarchy is a non root object. This checks if the provided object is a blue box or variant.
         * 
         *  - PrefabUtility.IsAddedGameObjectOverride()
         *      Use this when you want to know whether the object was added in current context
         * 
         *  - PrefabUtility.GetOriginalSourceRootWhereGameObjectIsAdded()
         *      Gets the source prefab where provided object is an override. Use this when you want to manipulate or delete (in combination with editContext)the original gameObject
         * 
         *  - PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot()
         *      Retrieves the root GameObject of the nearest Prefab instance the object is part of.
         *      The method searches the Transform hierarchy until it finds the root of any Prefab instance, regardless of whether that instance is an applied nested Prefab inside another Prefab, or not.
         *
         *  - PrefabUtility.IsPartOfAnyPrefab()
         *      Self explanatory except in prefab mode:  -  (though you probably shouldn't need to use this method when dealing with prefab mode)
         *          For Prefab contents loaded in Prefab Mode, this method will not check the Prefab Asset the loaded contents are loaded from,
         *          since these Prefab contents are loaded into a preview scene and are not part of an Asset while being edited in Prefab Mode.
         *          This means that for Prefab contents in Prefab Mode, the method will return true for Prefab instances, but not for GameObjects
         *          or components that are not part of a Prefab instance. To check if an object is part of Prefab contents in Prefab Mode, use PrefabStage.IsPartOfPrefabContents.
         *
         *  - PrefabUtility.ApplyPrefabInstance();
         *      Works same as apply all which means that it saves all changes to topmost prefab         
         * 
         *  - using var editContext = new PrefabUtility.EditPrefabContentsScope(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot())
         *      This structure is used to begin editing the actual asset file in a prefab contents stage style (with all of its overhead) which allows for destroying asset components and gameObjects. Does not work on fbx files or similar model sources
         *
         *  - PrefabUtility.RevertObjectOverride(this, InteractionMode.AutomatedAction);
         *      Reverts overrides to prefab state in current context. WARNING: large quantities of invocations are expensive
         *
         *
         * 
         * Other:
         *  - AssetDatabase.StartEditing()
         *      Disables unity importer. you have to guarantee (catch or finally block) that AssetDatabase.StopAssetEditing() + Refresh() runs once you finish editing files to prevent editor from becoming unresponsive to file changes.
        */
        #endregion

        public static T CreateTemplate<T>(string name, string path) where T : Template {
            GameObject template = new GameObject(name);
            template.AddComponent<T>();
            
            if (path.IsNullOrWhitespace()) {
                path = "Assets";
            } else if (!path.StartsWith("Assets")) {
                throw new NotSupportedException("Path must start with 'Assets'");
            }

            if (!path.EndsWith("/")) {
                path += "/";
            }

            string assetPath = path + name + ".prefab";
            
            PrefabUtility.SaveAsPrefabAsset(template, assetPath);
            GameObjects.DestroySafely(template);
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }
        /// <summary>
        /// To check whether the two instance of objects are similar enough to be recognised as equal. Does not account for all possible cases.
        /// </summary>
        public static bool IsTheSameObject(GameObject obj1, GameObject obj2, int checkParentsDepth = 0) {
            if (obj1 == obj2) return true;
            if (obj1 == null || obj2 == null) return false;
            
            bool recursiveResult = true;
            if (checkParentsDepth > 0) {
                checkParentsDepth--;
                recursiveResult = IsTheSameObject(obj1.transform.parent?.gameObject, obj2.transform.parent?.gameObject, checkParentsDepth);
            }
            return obj1.transform.GetSiblingIndex() == obj2.transform.GetSiblingIndex()
                   && obj1.name == obj2.name 
                   && recursiveResult;
        }

        /// <summary>
        /// Removes any suspected incorrect instances of Prefab instance as well as the provided gameObject if it is not an instance
        /// </summary>
        /// <param name="parent">The children of this parent will be searched</param>
        /// <param name="prefabInstance">The gameObject who's redundant or incorrect copies will be destroyed</param>
        /// <param name="ignore">Which objects to ignore during search</param>
        public static void RemoveAllRedundantInstances(Transform parent, GameObject prefabInstance, params GameObject[] ignore) {
            if (prefabInstance == null) return;
            List<GameObject> children = new();
            foreach (Transform child in parent) {
                children.Add(child.gameObject);
            }

            bool anyAssetChange = false;
            try {
                // Handle other children
                foreach (var child in children.Where(c => c != prefabInstance).Where(c => !ignore.Contains(c))) {
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(child) && prefabInstance.name.Contains(child.name.Replace("(Clone)", ""))) {
                        anyAssetChange = DestroyAnywhere_INTERNAL(child, anyAssetChange);

                        EditorUtility.SetDirty(parent.gameObject);
                    }

                }
                
                // provided gameObject is not a prefab instance and is therefore invalid
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(prefabInstance)) {
                    DestroyAnywhere_INTERNAL(prefabInstance, anyAssetChange);
                }

            } catch {
                if (anyAssetChange) {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.Refresh();
                    anyAssetChange = false;
                }
                throw;
            } finally {
                if (anyAssetChange) {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.Refresh();
                }
            }
        }

        /// <summary>
        /// Destroy anywhere that begins asset editing if there is an asset change and there was no previous asset change
        /// </summary>
        /// <returns>new state for anyAssetChange</returns>
        static bool DestroyAnywhere_INTERNAL(GameObject child, bool anyAssetChange) {
            if (PrefabUtility.IsOutermostPrefabInstanceRoot(child) || PrefabUtility.IsAddedGameObjectOverride(child)) {
                GameObjects.DestroySafely(child);
            } else {
                if (!anyAssetChange) {
                    AssetDatabase.StartAssetEditing();
                    anyAssetChange = true;
                }

                RemoveObjectFromPrefab(child);
            }

            return anyAssetChange;
        }

        /// <summary>
        /// Allows for asset level action on provided gameObject
        /// </summary>
        /// <param name="objectToGet">The object to find corresponding asset object</param>
        /// <param name="action">The action to invoke on found corresponding object asset</param>
        /// <param name="mode">Different target source prefabs to edit</param>
        public static void DoForObjectInSourceAsset(GameObject objectToGet, Action<GameObject> action, SourceAssetSearchMode mode = SourceAssetSearchMode.Exact) {
            if (objectToGet == null) return;

            if (InstantiatedHere(objectToGet)) {
                action.Invoke(objectToGet);
                return;
            }

            Transform toReplaceParent;
            switch (mode) {
                case SourceAssetSearchMode.Exact:
                    toReplaceParent = FindDeepestEditableAsset(objectToGet);
                    break;
                case SourceAssetSearchMode.InParent:
                    toReplaceParent = FindDeepestEditableAsset(objectToGet.transform.parent.gameObject);
                    break;
                case SourceAssetSearchMode.VisualPickers:
                    toReplaceParent = FindDeepestEditableAsset(PrefabUtility.GetOriginalSourceRootWhereGameObjectIsAdded(objectToGet.GetComponentInParent<VisualsPicker>().gameObject));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
            
             

            string assetPathOfToReplaceParent = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(toReplaceParent);
            if (assetPathOfToReplaceParent.IsNullOrWhitespace()) return;

            using var prefabEditing = new PrefabUtility.EditPrefabContentsScope(assetPathOfToReplaceParent);
            action.Invoke(prefabEditing.prefabContentsRoot
                                       .FindRecursively(x => IsTheSameObject(x, objectToGet, 2)));

        }

        public static void DoForComponentInAllDepth<T>(T componentToInvokeOn, Func<T, bool> action) where T : Component {
            if (componentToInvokeOn == null) return;
            
            while (true) {
                if (action.Invoke(componentToInvokeOn)) {
                    EditorUtility.SetDirty(componentToInvokeOn);
                }
                
                T c = PrefabUtility.GetCorrespondingObjectFromSource(componentToInvokeOn);
                if (c != null && PrefabUtility.IsPartOfPrefabAsset(c) && !PrefabUtility.IsPartOfImmutablePrefab(c) && !PrefabUtility.IsPartOfModelPrefab(c)) {
                    componentToInvokeOn = c;
                } else {
                    break;
                }
            }
        }
        
        public static void DoForComponentInAllDepthInPrefabScope<T>(T instanceToCrawl, Func<PrefabUtility.EditPrefabContentsScope, bool> action) where T : Component {
            if (instanceToCrawl == null) return;
            
            while (true) {
                string prefabAssetPathOfNearestInstanceRoot = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceToCrawl);
                if (prefabAssetPathOfNearestInstanceRoot.IsNullOrWhitespace()) break;
                
                using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabAssetPathOfNearestInstanceRoot)) {
                    if (action.Invoke(scope)) {
                        break;
                    }
                }
                
                T c = PrefabUtility.GetCorrespondingObjectFromSource(instanceToCrawl);
                if (c != null && PrefabUtility.IsPartOfPrefabAsset(c) && !PrefabUtility.IsPartOfImmutablePrefab(c) && !PrefabUtility.IsPartOfModelPrefab(c)) {
                    instanceToCrawl = c;
                } else {
                    break;
                }
            }
        }

        [UnityEngine.Scripting.Preserve]
        static void InvokeOnAllComponentsHere<T>(GameObject parent, Func<T, bool> action) where T : Component {
            bool anyChange = false;
            foreach (T component in parent.GetComponentsInChildren<T>()) {
                if (action.Invoke(component)) {
                    EditorUtility.SetDirty(component);
                    anyChange = true;
                }
            }
            if (PrefabUtility.IsPartOfPrefabAsset(parent)) {
                GameObject rootGameObject = parent.transform.root.gameObject;
                if (anyChange) {
                    PrefabUtility.SavePrefabAsset(rootGameObject);
                }
                PrefabUtility.UnloadPrefabContents(rootGameObject);
            }
        }

        /// <summary>
        /// Gets the last Prefab in sources that can be edited, returns root gameObject. Use this if the object is part of an FBX and you want to edit it in the closest prefab object.
        /// </summary>
        public static Transform FindDeepestEditableAsset(GameObject start) {
            GameObject result = start;
            while (true) {
                GameObject go = PrefabUtility.GetCorrespondingObjectFromSource(result);
                if (go != null && PrefabUtility.IsPartOfPrefabAsset(go) && !PrefabUtility.IsPartOfImmutablePrefab(go) && !PrefabUtility.IsPartOfModelPrefab(go)) {
                    result = go;
                } else {
                    break;
                }
            }

            return result.transform.root;
        }

        /// <summary>
        /// General purpose Destroy. EDITS ASSETS!
        /// </summary>
        public static void DestroyAnywhere(GameObject obj) {
            if (!PrefabUtility.IsPartOfPrefabInstance(obj) && (PrefabUtility.IsOutermostPrefabInstanceRoot(obj) || PrefabUtility.IsAddedGameObjectOverride(obj))) {
                GameObjects.DestroySafely(obj);
            } else {
                RemoveObjectFromPrefab(obj);
            }
        }

        /// <summary>
        /// Removes the corresponding gameObject from the asset that it belongs to. EDITS ASSETS!
        /// </summary>
        public static void RemoveObjectFromPrefab(GameObject objToRemove) {
            if (objToRemove == null) return;
            
            GameObject toReplaceParent = PrefabUtility.GetOriginalSourceRootWhereGameObjectIsAdded(objToRemove);
            if (toReplaceParent == null || !PrefabUtility.IsPartOfAnyPrefab(objToRemove) || PrefabUtility.IsAddedGameObjectOverride(objToRemove)) {
                GameObjects.DestroySafely(objToRemove);
                return;
            }
            
            var sourceObjToRemove = PrefabUtility.GetCorrespondingObjectFromOriginalSource(objToRemove);
            
            using var prefabEditingParent = new PrefabUtility.EditPrefabContentsScope(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(toReplaceParent));
            GameObject childInAssetEdit = prefabEditingParent.prefabContentsRoot
                                                             .FindRecursively(x => IsTheSameObject(x, sourceObjToRemove));
            
            if (childInAssetEdit != null && (!PrefabUtility.IsPartOfAnyPrefab(childInAssetEdit) || PrefabUtility.IsAddedGameObjectOverride(childInAssetEdit))) {
                GameObjects.DestroySafely(childInAssetEdit);
            } else {
                Log.Important?.Error("Could not destroy: " + childInAssetEdit?.name, objToRemove);
            }
        }

        /// <summary>
        /// Checks whether the gameObject has been instantiated in your current context. Your context can be the scene. A prefab asset or prefab scene.
        /// </summary>
        public static bool InstantiatedHere(GameObject visuals) {
            return PrefabUtility.IsOutermostPrefabInstanceRoot(visuals) || PrefabUtility.IsAddedGameObjectOverride(visuals);
        }

        // === Editor Tools
        [UsedImplicitly]
        public static void CleanupOverridesOnLights() {
            InvokeOnAllComponents<Light>(RevertLightIntensity);
        }

        static bool RevertLightIntensity(Light c) {
            if (!PrefabUtility.IsPartOfPrefabInstance(c)) return false;
            
            bool anyChange = false;
            SerializedObject serializedLight = new SerializedObject(c);
            SerializedProperty serializedIntensity = serializedLight.FindProperty("m_Intensity");

            if (serializedIntensity is {prefabOverride: true}) {
                SerializedObject serializedAdditionalData = new SerializedObject(c.GetComponent<HDAdditionalLightData>());
                SerializedProperty serializedIntensityInData = serializedAdditionalData.FindProperty("m_Intensity");
                
                if (serializedIntensityInData is {prefabOverride: true}) {
                    PrefabUtility.RevertPropertyOverride(serializedIntensityInData, InteractionMode.AutomatedAction);
                    serializedIntensityInData.Dispose();
                }
                PrefabUtility.RevertPropertyOverride(serializedIntensity, InteractionMode.AutomatedAction);
                
                serializedAdditionalData.Dispose();
                serializedIntensity.Dispose();
                anyChange = true;
            }
            
            serializedLight.Dispose();
            
            return anyChange;
        }
        
        [UsedImplicitly]
        public static void CleanupOverridesOnMeshRendererMaterials() {
            InvokeOnAllComponents<MeshRenderer>(RevertInstanceMeshRendererIfSourceHasTheSameValues);
        }
        
        static void InvokeOnAllComponents<T>(Func<T, bool> action) where T : Component {
            try {
                AssetDatabase.StartAssetEditing();
                T[] children = Selection.activeGameObject.GetComponentsInChildren<T>();
                for (var index = 0; index < children.Length; index++) {
                    if (EditorUtility.DisplayCancelableProgressBar("Cleaning Up Overrides: " + children[index].name, "Cleaning up MeshRenderer and MeshFilter unused overrides on materials and mesh", index / (float) children.Length)) break;
                    DoForComponentInAllDepth(children[index], action);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
        }

        static bool RevertInstanceMeshRendererIfSourceHasTheSameValues(MeshRenderer c) {
            return false;
        }

        /// <summary>
        /// For object on scene returns itself. <br/>
        /// For object instantiated from prefab returns the original object from the prefab asset.
        /// </summary>
        public static TObject GetOriginalObject<TObject>(TObject instance) where TObject : Object {
            TObject original;
            do {
                original = instance;
                instance = PrefabUtility.GetCorrespondingObjectFromSource(original);
            } while (instance != null);
            return original;
        }

        /// <inheritdoc cref="GetOriginalObject{TObject}(TObject)"/>
        public static TObject GetOriginalObject<TObject>(TObject instance, out bool isPrefab) where TObject : Object {
            TObject original = GetOriginalObject(instance);
            isPrefab = !ReferenceEquals(original, instance);
            return original;
        }
    }
}
#endif