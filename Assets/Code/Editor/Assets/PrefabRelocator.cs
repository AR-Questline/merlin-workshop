using System;
using System.Collections.Generic;
using System.IO;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets {
    public class PrefabRelocator : EditorWindow {
        const string HDRPLitGUID = "6e4ae4064600d784cac1e41a9e6f2e59";
        
        readonly List<GameObject> _prefabs = new();
        Object _newPrefabFolder;
        Vector2 _scrollPosition;
        bool _useFolders = true;
        bool _moveFBX = true;
        bool _moveMaterials = true;
        bool _moveTextures = true;
        bool _moveOnlyHDRPLit;
        
        [MenuItem("ArtTools/Prefab Relocator")]
        static void Init() {
            PrefabRelocator window = (PrefabRelocator)EditorWindow.GetWindow(typeof(PrefabRelocator), false, "Prefab Relocator");
            window.Show();
        }

        void OnGUI() {
            GUILayout.Label("Select one or more prefabs and choose a new folder to move them to.", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Prefabs to Move:");

            if (_prefabs.Count > 0) {
                EditorGUI.indentLevel++;
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                for (int i = 0; i < _prefabs.Count; i++) {
                    EditorGUILayout.BeginHorizontal();

                    _prefabs[i] = (GameObject)EditorGUILayout.ObjectField(_prefabs[i], typeof(GameObject), true);

                    if (GUILayout.Button("Remove")) {
                        _prefabs.RemoveAt(i);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
                EditorGUI.indentLevel--;
                // --- Clear Button
                if (GUILayout.Button("Clear All")) {
                    _prefabs.Clear();
                }
            } else {
                EditorGUILayout.HelpBox("Drag new prefabs inside this window", MessageType.Info);
            }

            EditorGUILayout.Space();

            GUILayout.Label("Choose New Folder:");

            _newPrefabFolder = EditorGUILayout.ObjectField(_newPrefabFolder, typeof(Object), true);

            EditorGUILayout.Space();

            if (Event.current.type is EventType.DragUpdated or EventType.DragPerform) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (Event.current.type == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag();

                    foreach (string path in DragAndDrop.paths) {
                        GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (gameObject != null && !_prefabs.Contains(gameObject)) {
                            _prefabs.Add(gameObject);
                        }
                    }
                }
            }

            GUI.enabled = _prefabs.Count > 0 && _newPrefabFolder != null;
            _useFolders = EditorGUILayout.Toggle("Create separate folders", _useFolders);
            _moveFBX = EditorGUILayout.Toggle("Move FBX", _moveFBX);
            _moveMaterials = EditorGUILayout.Toggle("Move Materials", _moveMaterials);
            _moveTextures = EditorGUILayout.Toggle("Move Textures", _moveTextures);
            _moveOnlyHDRPLit = EditorGUILayout.Toggle("Move Only HDRP Lit materials & textures", _moveOnlyHDRPLit);
            if (GUILayout.Button("Move Prefabs") && _prefabs.Count > 0 && _newPrefabFolder != null) {
                string newPath = AssetDatabase.GetAssetPath(_newPrefabFolder);
                CreatePaths(newPath, out string newPrefabsPath, out string meshesPath, out string materialsPath, out string texturesPath);

                AssetDatabase.StartAssetEditing();

                foreach (GameObject prefab in _prefabs) {
                    if (prefab == null) {
                        continue;
                    }

                    // --- Move Prefab
                    string prefabPath = AssetDatabase.GetAssetPath(prefab);
                    AssetDatabase.MoveAsset(prefabPath, newPrefabsPath + "/" + prefab.name + ".prefab");

                    // --- Move Prefab Components
                    Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers) {
                        if (_moveFBX) {
                            MoveFbx(renderer, meshesPath);
                        }
                        MoveMaterialsAndTextures(renderer, materialsPath, texturesPath, _moveMaterials, _moveTextures, _moveOnlyHDRPLit);
                    }
                }

                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUI.enabled = true;
        }

        // === Helpers
        void CreatePaths(string basePath, out string newPrefabsPath, out string meshesPath, out string materialsPath, out string texturesPath) {
            newPrefabsPath = basePath;
            meshesPath = basePath;
            materialsPath = basePath;
            texturesPath = basePath;

            if (_useFolders) {
                bool anyFolderCreated = false;

                newPrefabsPath += "/Prefabs";
                if (!Directory.Exists(newPrefabsPath)) {
                    Directory.CreateDirectory(newPrefabsPath);
                    anyFolderCreated = true;
                }

                meshesPath += "/Meshes";
                if (!Directory.Exists(meshesPath)) {
                    Directory.CreateDirectory(meshesPath);
                    anyFolderCreated = true;
                }

                materialsPath += "/Materials";
                if (!Directory.Exists(materialsPath)) {
                    Directory.CreateDirectory(materialsPath);
                    anyFolderCreated = true;
                }

                texturesPath += "/Textures";
                if (!Directory.Exists(texturesPath)) {
                    Directory.CreateDirectory(texturesPath);
                    anyFolderCreated = true;
                }

                if (anyFolderCreated) {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
        }

        static void MoveFbx(Renderer renderer, string newMeshPath) {
            MeshFilter meshFilter = renderer.gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) {
                return;
            }

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) {
                return;
            }

            string fbxPath = AssetDatabase.GetAssetPath(mesh);
            fbxPath = Path.ChangeExtension(fbxPath, ".fbx");
            if (File.Exists(fbxPath)) {
                string fbxName = Path.GetFileName(fbxPath);
                AssetDatabase.MoveAsset(fbxPath, newMeshPath + "/" + fbxName);
            } else {
                Log.Important?.Error("No FBX file found for mesh: " + mesh.name);
            }
        }

        static void MoveMaterialsAndTextures(Renderer renderer, string newMaterialPath, string newTexturePath, bool moveMaterials, bool moveTextures, bool onlyHDRPLit) {
            Material material = renderer.sharedMaterial;
            if (material == null) {
                return;
            }

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material.shader, out string shaderGuid, out long _);
            if (onlyHDRPLit && !shaderGuid.Equals(HDRPLitGUID, StringComparison.InvariantCultureIgnoreCase)) {
                return;
            }

            string materialPath = AssetDatabase.GetAssetPath(material);

            // --- Move Textures
            if (moveTextures) {
                string[] texturePaths = AssetDatabase.GetDependencies(materialPath, true);
                foreach (string texturePath in texturePaths) {
                    if (texturePath.EndsWith(".meta")) {
                        return;
                    }

                    var result = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
                    if (result == null) {
                        continue;
                    }

                    string textureName = Path.GetFileName(texturePath);
                    AssetDatabase.MoveAsset(texturePath, newTexturePath + "/" + textureName);
                }
            }

            // --- Move material
            if (moveMaterials) {
                string materialName = Path.GetFileName(materialPath);
                AssetDatabase.MoveAsset(materialPath, newMaterialPath + "/" + materialName);
            }
        }
    }

    public class UniqueMissingGUIDsWindow : OdinEditorWindow {
        [MenuItem("TG/Assets/Missing Prefab GUIDs")]
        static void ShowWindow() {
            var window = GetWindow<UniqueMissingGUIDsWindow>();
            window.titleContent = new GUIContent("Missing Prefab GUIDs");
            window.Show();
        }
        
        public string result;
        
        [Button]
        public void GetAllMissingPrefabGUIDS() {
            HashSet<string> uniqueGuids = new(100);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                try {
                    if (t.gameObject.name.Contains("Missing Prefab with guid: ")) {
                        int index = t.gameObject.name.IndexOf("Missing Prefab with guid: ",
                            StringComparison.CurrentCultureIgnoreCase);
                        string guid = t.gameObject.name.Substring(index + 26);
                        guid = guid.Remove(guid.Length - 1, 1);
                        uniqueGuids.Add(guid);
                    }
                } catch (Exception e) {
                    Log.Important?.Error(e.ToString());
                }
            }

            this.result = string.Join('\n', uniqueGuids);
        }
    }
}