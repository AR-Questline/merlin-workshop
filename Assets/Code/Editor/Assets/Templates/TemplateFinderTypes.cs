using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets.Templates {
    [Serializable]
    public class TemplateResultEntry {
        [VerticalGroup("Select"), LabelText("", SdfIconType.Grid), TableColumnWidth(50, false)]
        public bool isSelected;

        [TableColumnWidth(300)]
        [VerticalGroup("Template")]
        [HorizontalGroup("Template/Name"), HideLabel, ReadOnly]
        public string templateTypeName;
        [HorizontalGroup("Template/Name", Width = 0.65f), HideLabel, ReadOnly]
        public string templateName;
        [VerticalGroup("Template"), ReadOnly, LabelText("Category"), PropertySpace]
        public TemplateType templateTypeEnum;
        [VerticalGroup("Template"), HideLabel, InlineButton(nameof(SelectTemplate), SdfIconType.FileEarmarkFill, "Goto Asset")]
        public Template template;
        [VerticalGroup("Template"), HideLabel, InlineButton(nameof(CopyGUID), SdfIconType.StickiesFill, "Copy GUID")]
        public string guid;
        [VerticalGroup("Template"), HideLabel, InlineButton(nameof(CopyPath), SdfIconType.StickiesFill, "Copy Path")]
        public string templateAssetPath;
        public Type templateType;

        [TableColumnWidth(500)]
        [VerticalGroup("Details"), ReadOnly, GUIColor(0.8f, 0.8f, 1f)]
        public int allUsageCount;
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false), PropertySpace]
        [VerticalGroup("Details")]
        public List<UsedAssetRef> usedInScenes = new();
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false)]
        [VerticalGroup("Details")]
        public List<UsedAssetRef> usedInPrefabs = new();
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false)]
        [VerticalGroup("Details")]
        public List<UsedAssetRef> usedInStoryGraphs = new();
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false)]
        [VerticalGroup("Details")]
        public List<UsedAssetRef> usedInLootTables = new();
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false)]
        [VerticalGroup("Details")]
        public List<UsedAssetRef> usedInOther = new();

        public bool UsedOnlyInScenes => allUsageCount > 0 && usedInScenes.Count == allUsageCount;
        bool UsedInSingleScene => usedInScenes.Count == 1;
        bool UsedInLoadedScene => usedInScenes.Any(IsSceneLoaded);
        public event Action<TemplateResultEntry> OnDeleted = delegate { };

        public string ToCsvLine() {
            var name = string.IsNullOrEmpty(templateName) ? (template?.name ?? "Unknown") : templateName;
            var typeName = templateType?.Name ?? "Unknown";
            var templateTypeEnumName = templateTypeEnum.ToString();

            return $"\"{name}\",\"{typeName}\",\"{templateTypeEnumName}\",\"{guid}\",{allUsageCount},\"{templateAssetPath}\"," +
                   $"\"{JoinPaths(usedInScenes)}\",\"{JoinPaths(usedInPrefabs)}\"," +
                   $"\"{JoinPaths(usedInStoryGraphs)}\",\"{JoinPaths(usedInLootTables)}\",\"{JoinPaths(usedInOther)}\"";

            static string JoinPaths(List<UsedAssetRef> refs) => string.Join("; ", refs.Select(r => r.path));
        }

        [VerticalGroup("Actions")]
        [Button("Load Scene", Icon = SdfIconType.FileEarmarkArrowUp), EnableIf(nameof(UsedInSingleScene))]
        void LoadSingleScene() {
            if (usedInScenes.Count != 1) {
                return;
            }

            var scenePath = usedInScenes[0].path;
            var sceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (EditorUtility.DisplayDialog("Load Scene",
                $"Do you want to load scene '{sceneName}'?\n\nPath: {scenePath}",
                "Load", "Cancel")) {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    Log.Important?.Info($"Loaded scene '{sceneName}'");
                }
            }
        }

        [VerticalGroup("Actions")]
        [Button("Find in Scene", Icon = SdfIconType.Search), EnableIf(nameof(UsedInLoadedScene))]
        void FindInLoadedScene() {
            if (template == null || string.IsNullOrEmpty(guid)) {
                EditorUtility.DisplayDialog("Error", "Template reference is invalid.", "OK");
                return;
            }

            var foundObjects = new List<GameObject>();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++) {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded || usedInScenes.All(s => s.name != scene.name)) {
                    continue;
                }

                foreach (var rootObj in scene.GetRootGameObjects()) {
                    FindTemplateInGameObject(rootObj, guid, foundObjects);
                }
            }

            if (foundObjects.Count == 0) {
                EditorUtility.DisplayDialog("Not Found",
                    $"Could not find any GameObjects using template '{template.name}' in loaded scenes.", "OK");
            } else if (foundObjects.Count == 1) {
                Selection.activeGameObject = foundObjects[0];
                EditorGUIUtility.PingObject(foundObjects[0]);
                Log.Important?.Info($"Found template '{template.name}' in GameObject '{foundObjects[0].name}'");
            } else {
                Selection.objects = foundObjects.ToArray();
                EditorGUIUtility.PingObject(foundObjects[0]);
                Log.Important?.Info($"Found {foundObjects.Count} GameObjects using template '{template.name}'");
            }
        }

        [VerticalGroup("Actions")]
        [Button("Delete Template", Icon = SdfIconType.TrashFill), PropertySpace]
        void DeleteTemplate() {
            if (template == null) {
                EditorUtility.DisplayDialog("Cannot Delete", "Template reference is null.", "OK");
                return;
            }

            var path = AssetDatabase.GetAssetPath(template);
            if (string.IsNullOrEmpty(path)) {
                EditorUtility.DisplayDialog("Cannot Delete", "Could not find asset path for the template.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Confirm Delete", $"Are you sure you want to delete the template '{template.name}'?", "Delete", "Cancel")) {
                if (AssetDatabase.DeleteAsset(path)) {
                    Log.Important?.Info($"Deleted template '{template.name}' at path '{path}'");
                    AssetDatabase.Refresh();
                    OnDeleted?.Invoke(this);
                } else {
                    EditorUtility.DisplayDialog("Delete Failed", "Failed to delete the template asset.", "OK");
                }
            }
        }

        void SelectTemplate() {
            var assetToSelect = template != null
                ? template
                : !string.IsNullOrEmpty(templateAssetPath)
                    ? AssetDatabase.LoadAssetAtPath<Object>(templateAssetPath)
                    : null;

            if (assetToSelect != null) {
                Selection.activeObject = assetToSelect;
            }
        }

        void CopyGUID() {
            if (!string.IsNullOrEmpty(guid)) {
                EditorGUIUtility.systemCopyBuffer = guid;
            }
        }

        void CopyPath() {
            if (!string.IsNullOrEmpty(templateAssetPath)) {
                EditorGUIUtility.systemCopyBuffer = templateAssetPath.Replace('\\', '/');
            }
        }

        static bool IsSceneLoaded(UsedAssetRef sceneRef) {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++) {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name == sceneRef.name) {
                    return true;
                }
            }
            return false;
        }

        static void FindTemplateInGameObject(GameObject obj, string templateGuid, List<GameObject> results) {
            var components = obj.GetComponents<Component>();
            foreach (var component in components) {
                if (component == null) {
                    continue;
                }

                var serializedObject = new SerializedObject(component);
                var property = serializedObject.GetIterator();

                while (property.Next(true)) {
                    if (property.propertyType == SerializedPropertyType.String && property.stringValue == templateGuid) {
                        if (!results.Contains(obj)) {
                            results.Add(obj);
                        }
                        break;
                    }
                }

                if (results.Contains(obj)) {
                    break;
                }
            }

            for (int i = 0; i < obj.transform.childCount; i++) {
                FindTemplateInGameObject(obj.transform.GetChild(i).gameObject, templateGuid, results);
            }
        }
    }

    [Serializable]
    public struct UsedAssetRef {
        [ShowInInspector, ReadOnly, HideLabel]
        public Object asset;

        [HideInInspector] public string path;
        [HideInInspector] public string guid;
        [HideInInspector] public string name;

        public UsedAssetRef(string assetPath) {
            path = assetPath;
            asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            guid = AssetDatabase.AssetPathToGUID(assetPath);
            name = asset != null && !string.IsNullOrEmpty(asset.name) ? asset.name : string.Empty;
        }

        public static implicit operator Object(UsedAssetRef reference) => reference.asset;
        public static implicit operator string(UsedAssetRef reference) => reference.path;
    }
}
