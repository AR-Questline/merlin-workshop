using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.SimpleTools;
using Awaken.TG.EditorOnly;
using Awaken.TG.Graphics.Scene;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.Animations.Gestures;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets {
    public abstract class ProjectScanner : OdinEditorWindow {
        [SerializeField, TableList(IsReadOnly = true, CellPadding = 5)] 
        SceneToScan[] scenes = Array.Empty<SceneToScan>();

        protected override void OnEnable() {
            scenes = CommonReferences.Get.SceneConfigs.AllScenes
                .Select(s => new SceneToScan(s))
                .ToArray();
        }

        [Button, HorizontalGroup("Selecting")]
        void SelectAll() {
            for (int i = 0; i < scenes.Length; i++) {
                scenes[i].include = true;
            }
        }

        [Button, HorizontalGroup("Selecting")]
        void DeselectAll() {
            for (int i = 0; i < scenes.Length; i++) {
                scenes[i].include = false;
            }
        }

        /// <summary>
        /// For all component T on selected scenes runs action. Save them if necessary.
        /// </summary>
        protected void Scan<T>(Action<T> action) where T : Component {
            foreach (var (scene, sceneSet, prefabSet) in IterativeScan<T>()) {
                if (sceneSet.Count > 0) {
                    Log.Important?.Info("------ Scene ------");
                    foreach (var component in sceneSet) {
                        RunActionSafe(component);
                    }
                    EditorSceneManager.SaveScene(scene);
                }
                if (prefabSet.Count > 0) {
                    Log.Important?.Info("------ Prefabs ------");
                    AssetDatabase.StartAssetEditing();
                    foreach (var component in prefabSet) {
                        RunActionSafe(component);
                    }
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                }
            }

            void RunActionSafe(T component) {
                try {
                    action(component);
                } catch (Exception e) {
                    Debug.LogException(e, component);
                }
            }
        }

        /// <summary>
        /// Scans selected scenes for components of type T.
        /// </summary>
        /// <param name="sceneSet">
        /// Iterator of selected scenes that gather all components instantiated directly on scene (not prefabs). <br/>
        /// While iterating given scene will be loaded. <br/>
        /// Returned HashSet will be reused so don't use it outside of iterator. 
        /// </param>
        /// <param name="prefabSet">
        /// Set of components in prefabs instantiated on selected scenes. <br/>
        /// Is valid only after iterating sceneSets. <br/>
        /// You need to save this changes before next step because unity looses reference to prefabs when scene is unloaded. <br/>
        /// Returned HashSet will be reused so don't use it outside of iterator. 
        /// </param>
        protected IEnumerable<(Scene, HashSet<T> sceneSet, HashSet<T> prefabSet)> IterativeScan<T>() where T : Component {
            HashSet<T> sceneSet = new();
            HashSet<T> prefabSet = new();

            foreach (var scene in SceneIterator()) {
                sceneSet.Clear();
                prefabSet.Clear();
                var componentsInScene = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
                foreach (var componentInScene in componentsInScene) {
                    try {
                        T original = EditorPrefabHelpers.GetOriginalObject(componentInScene, out bool isPrefab);
                        (isPrefab ? prefabSet : sceneSet).Add(original);
                    } catch(Exception e) {
                        Debug.LogException(e);
                    }
                }
                yield return (scene, sceneSet, prefabSet);
            }
        }

        protected IEnumerable<Scene> SceneIterator() {
            for (int i = 0; i < scenes.Length; i++) {
                if (!scenes[i].include) {
                    continue;
                }
                
                string path = $"Assets/{scenes[i].directory}{scenes[i].sceneName}.unity";
                Log.Important?.Info($"====== {scenes[i].sceneName} ======", AssetDatabase.LoadAssetAtPath<SceneAsset>(path));
                var scene = EditorSceneManager.OpenScene(path);

                yield return scene;
            }
        }

        [Serializable]
        struct SceneToScan {
            [HideLabel, ReadOnly, VerticalGroup("Scene")] public string sceneName;
            [HideLabel, VerticalGroup("Include")] public bool include;
            [HideInInspector] public string directory;

            public SceneToScan(SceneConfig config) : this() {
                sceneName = config.sceneName;
                directory = config.directory;
                include = true;
            }
        }
    }

    class ValidateGestureOverrides : ProjectScanner {
        [Button]
        void Validate() {
            foreach (var scene in SceneIterator()) {
                var interactions =  FindObjectsByType<SimpleInteraction>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var interaction in interactions) {
                    var prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(interaction.gameObject);
                    if (prefabParent != null) {
                        var instance = interaction.gameObject;
                        PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(instance);
                        if (prefabStage != null && prefabStage.prefabContentsRoot == instance) {
                            continue;
                        }
                        var prefabHandle = PrefabUtility.GetPrefabInstanceHandle(instance);
                        if (prefabHandle == null) {
                            continue;
                        }
                        var modifications = PrefabUtility.GetPropertyModifications(instance);
                        if (modifications == null || modifications.Length == 0) {
                            continue;
                        }

                        foreach (var modification in modifications) {
                            if (modification.propertyPath == "gestures.explicitOverrides._guid" ||
                                modification.propertyPath.Contains("gestures.embedOverrides.gestures.Array")) {
                                Debug.LogError($"Overriden Gestures in prefab interaction: {interaction} on scene: {scene}", interaction);
                                break;
                            }
                        }
                    }
                }

                var dialogues = FindObjectsByType<DialogueAttachment>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var dialogue in dialogues) {
                    var prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(dialogue.gameObject);
                    if (prefabParent != null) {
                        var instance = dialogue.gameObject;
                        PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(instance);
                        if (prefabStage != null && prefabStage.prefabContentsRoot == instance) {
                            continue;
                        }
                        var prefabHandle = PrefabUtility.GetPrefabInstanceHandle(instance);
                        if (prefabHandle == null) {
                            continue;
                        }
                        var modifications = PrefabUtility.GetPropertyModifications(instance);
                        if (modifications == null || modifications.Length == 0) {
                            continue;
                        }

                        foreach (var modification in modifications) {
                            if (modification.propertyPath == "gesturesWrapper.explicitOverrides._guid" ||
                                modification.propertyPath.Contains("gesturesWrapper.embedOverrides.gestures.Array")) {
                                Debug.LogError($"Overriden Gestures in dialogue attachment: {dialogue} on scene: {SceneManager.GetActiveScene().name}", instance);
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        [MenuItem("TG/Project Scanner/Validate Gesture Overrides")]
        public static void Create() {
            GetWindow<ValidateGestureOverrides>().Show();
        }
    }

    class PickItemAttachmentToPickableSpecConverter : ProjectScanner {
        [Button]
        void Convert() {
            HashSet<PickItemAttachment> attachmentsMet = new();
            foreach (var scene in SceneIterator()) {
                bool sceneDirty = false;
                
                bool process;
                do {
                    process = TryProcessNext(attachmentsMet, ref sceneDirty);
                } while (process);

                if (sceneDirty) {
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }

        static bool TryProcessNext(HashSet<PickItemAttachment> attachmentsMet, ref bool sceneDirty) {
            var attachments = FindObjectsByType<PickItemAttachment>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var attachment in attachments) {
                if (!attachmentsMet.Contains(attachment)) {
                    var originalAttachment = EditorPrefabHelpers.GetOriginalObject(attachment, out bool isPrefab);
                        
                    if (!attachmentsMet.Contains(originalAttachment)) {
                        Convert(originalAttachment);
                        if (isPrefab) {
                            AssetDatabase.SaveAssets();
                        } else {
                            sceneDirty = true;
                        }

                        if (originalAttachment != null) {
                            attachmentsMet.Add(attachment);
                            attachmentsMet.Add(originalAttachment);
                        }

                        return true;
                    } else {
                        attachmentsMet.Add(attachment);
                    }
                }
            }
            return false;
        }

        static void Convert(PickItemAttachment attachment) {
            var go = attachment.gameObject;
            attachment.ConvertToPickableSpec(true, false, out bool removeFromAddressables);
            if (removeFromAddressables) {
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(go));
                AddressableHelper.RemoveEntry(guid);
            }
            EditorUtility.SetDirty(go);
        }

        [MenuItem("TG/Project Scanner/Convert PickItemAttachments to PickableSpecs")]
        public static void Create() {
            GetWindow<PickItemAttachmentToPickableSpecConverter>().Show();
        }
    }
}