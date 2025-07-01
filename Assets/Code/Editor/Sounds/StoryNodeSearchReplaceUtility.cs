using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using XNode;

namespace Awaken.TG
{
    public static class StoryNodeSearchReplaceUtility 
    {
        /*[MenuItem("TG/Story Node Search And Replace/Search for Story Nodes Ports Field and Type Names")]
        public static void SearchForNodePorts() {
            Debug.Log("Node ports types names");
            foreach (var node in IterateThroughScriptableObjects<Node>()) {
                foreach (var port in node.Ports) {
                    Debug.Log("type: " + port._typeQualifiedName);
                    var fieldNameParts = port._fieldName.Split(":");
                    for (int i = 0; i < fieldNameParts.Length; i++) {
                        Debug.Log($"f{i}: " + fieldNameParts[i]);
                    }
                }
            }
        }
        
        [MenuItem("TG/Story Node Search And Replace/Recalculate Story Nodes Ports Compressed Field and Type Name")]
        public static void RecalculateNodePortsFieldNameAndTypeQualifiedName() {
            foreach (var node in IterateThroughScriptableObjects<Node>()) {
                var portsNew = new Node.NodePortDictionaryNew();
                if (node.ports.Count == 0) {
                    continue;
                }
                foreach (var (portName, port) in node.ports) {
                    port.Recalculate();
                    portsNew.Add((NodePort.FieldNameCompressed)portName, port.CreateCopyFromOld());
                }

                node.portsNew = portsNew;
                node.portsNew.OnBeforeSerialize();
                EditorUtility.SetDirty(node);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        [MenuItem("TG/Story Node Search And Replace/Verify Story Nodes Ports Compressed Field and Type Name")]
        public static void VerifyNodePortsFieldNameAndTypeQualifiedName() {
            foreach (var node in IterateThroughScriptableObjects<Node>()) {
                foreach (var port in node.Ports) {
                    var fieldName = port._fieldNameCompressed.ToFieldNameString();
                    if (fieldName != port._fieldName && string.IsNullOrEmpty(port._fieldName) == false) {
                        Debug.LogError($"No match in {node.name}. Compressed {port._fieldNameCompressed.nameCode}, {port._fieldNameCompressed.countValue}: {fieldName} != {port._fieldName}", node);
                    }

                    var typeName = port._typeQualifiedNameCompressed.ToTypeName();
                    if (typeName != port._typeQualifiedName && string.IsNullOrEmpty(port._typeQualifiedName) == false) {
                        Debug.LogError($"No match in {node.name}. Compressed {port._typeQualifiedNameCompressed.nameCode}: {typeName} != {port._typeQualifiedName}", node);
                    }
                }
            }
        }
        */
        
        [MenuItem("TG/Story Node Search And Replace/Refresh Story Nodes")]
        public static void RefreshNodePorts() {
            foreach (var node in IterateThroughScriptableObjects<Node>()) {
                EditorUtility.SetDirty(node);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        public static void SearchForComponentInPrefabs<T>(out Dictionary<string, List<T>> prefabPathToComponentsMap) where T: Component {
            if (TryGetSelectedFolderPath(out var selectedFolderPath) == false) {
                Log.Important?.Error($"No valid folder selected");
                prefabPathToComponentsMap = new Dictionary<string, List<T>>(0);
                return;
            }
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { selectedFolderPath });
            prefabPathToComponentsMap = new Dictionary<string, List<T>>(64);
            for (int i = 0; i < guids.Length; i++) {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) {
                    continue;
                }

                ListPool<T>.Get(out var components);
                prefab.GetComponentsInChildren(true, components);
                if (components.Count == 0) {
                    ListPool<T>.Release(components);
                    continue;
                }
                prefabPathToComponentsMap.Add(path, components);
            }
        }
        
        public static void SearchForScriptableObjects<T>(List<string> scriptableObjectsPaths) where T: ScriptableObject {
            if (TryGetSelectedFolderPath(out var selectedFolderPath) == false) {
                Log.Important?.Error($"No valid folder selected");
                return;
            }
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).FullName}", new[] { selectedFolderPath });
            for (int i = 0; i < guids.Length; i++) {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                scriptableObjectsPaths.Add(path);
            }
        }

        public static IEnumerable<T> IterateThroughScriptableObjects<T>() where T: ScriptableObject {
            var paths = new List<string>(64);
            SearchForScriptableObjects<T>(paths);
            int pathsCount = paths.Count;
            for (int i = 0; i < pathsCount; i++) {
                var path = paths[i];
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                yield return asset;
                Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                foreach (Object subAsset in subAssets)
                {
                    if (subAsset is T typedSubAsset) {
                        yield return typedSubAsset;
                    }
                }
            }
        }
        
        static bool TryGetSelectedFolderPath(out string selectedPath)
        {
            selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            return string.IsNullOrEmpty(selectedPath) == false && AssetDatabase.IsValidFolder(selectedPath);
        }
    }
}
