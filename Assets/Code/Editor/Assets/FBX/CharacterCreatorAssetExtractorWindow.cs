using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets.FBX {
    public class CharacterCreatorAssetExtractorWindow : OdinEditorWindow {
        [InfoBox("This tool extracts assets from character creator fbx and moves them to a new folder.\nAnything starting with CC except for the root will not be extracted.")]
        [ShowInInspector] GameObject _sourceFBX;
        [ShowInInspector] bool _moveMaterials = true;
        [ShowInInspector] bool _moveTextures = true;
        [ShowInInspector] bool _moveMeshes = true;
        [ShowInInspector, FolderPath] string _targetFolder = "Assets/3DAssets/Extracted";
        
        [MenuItem("ArtTools/Extract from Character Creator", priority = 100)]
        static void ShowEditor() {
            var editor = EditorWindow.GetWindow<CharacterCreatorAssetExtractorWindow>("CC FBX Extractor");
            editor.Show();
        }
        
        protected override void OnBeginDrawEditors() {
            base.OnBeginDrawEditors();
            _sourceFBX = Selection.activeGameObject;
        }
        
        protected override void OnImGUI() {
            base.OnImGUI();
            if (GUILayout.Button("Extract and Move")) {
                MoveOnly();
            }
        }
        void MoveOnly() {
            if (_sourceFBX == null) {
                Log.Important?.Error("No target to move");
                return;
            }
            var sourcePrefabInstance = PrefabUtility.InstantiatePrefab(_sourceFBX) as GameObject;

            if (sourcePrefabInstance == null) {
                Log.Important?.Error("source is not prefab");
                return;
            }
            
            // create parent folder
            var parentFolder = AssetDatabase.CreateFolder(_targetFolder, _sourceFBX.name);
            string parentFolderPath = AssetDatabase.GUIDToAssetPath(parentFolder);
            
            // We usually don't need the animator
            foreach (Animator animator in sourcePrefabInstance.GetComponentsInChildren<Animator>()) {
                DestroyImmediate(animator);
            }

            // Remove unnecessary gameObjects
            for (int i = sourcePrefabInstance.transform.childCount; i > 0; i--) {
                Transform transform = sourcePrefabInstance.transform.GetChild(i - 1);
                string originalName = transform.name;

                // For skinned mesh renderers we need to keep the rig
                if (originalName.Contains("root", StringComparison.InvariantCultureIgnoreCase)) {
                    continue;
                }

                // We want to remove all other CC objects
                if (originalName.StartsWith("CC", StringComparison.InvariantCultureIgnoreCase)) {
                    DestroyImmediate(transform.gameObject);
                }
            }

            // save the instance as a new prefab
            var prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(
                sourcePrefabInstance,
                $"{parentFolderPath}/{_sourceFBX.name}.prefab",
                InteractionMode.AutomatedAction);

            try {
                AssetDatabase.StartAssetEditing();
                
                if (_moveMaterials) {
                    MoveMaterials(sourcePrefabInstance, parentFolderPath);
                }

                if (_moveMeshes) {
                    MoveMeshesFromSkinned(sourcePrefabInstance, parentFolderPath, prefabAsset);
                }

                if (_moveTextures) {
                    MoveTextures(sourcePrefabInstance, parentFolderPath);
                }
            } finally {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                DestroyImmediate(sourcePrefabInstance);
            }
        }
        
        

        static void MoveMaterials(GameObject sourcePrefabInstance, string parentFolderPath) {
            var materials = sourcePrefabInstance.GetComponentsInChildren<Renderer>()
                .SelectMany(r => r.sharedMaterials)
                .Distinct();

            foreach (var originalMaterial in materials) {
                if (originalMaterial == null) continue;

                // If material is not embedded in the fbx, move it to the folder
                if (!AssetDatabase.IsSubAsset(originalMaterial)) {
                    AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(originalMaterial), $"{parentFolderPath}/{originalMaterial.name}.mat");
                } else {
                    Log.Important?.Error("Material is embedded in the fbx: " + originalMaterial.name, originalMaterial);
                }
            }
        }

        static void MoveMeshesFromSkinned(
            GameObject sourcePrefabInstance, string parentFolderPath, GameObject prefabAsset) {
            var skinnedMeshRenderers = sourcePrefabInstance.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var smr in skinnedMeshRenderers) {
                var mesh = smr.sharedMesh;
                if (mesh == null) continue;

                if (!AssetDatabase.IsSubAsset(mesh)) {
                    AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(mesh), $"{parentFolderPath}/{mesh.name}.asset");
                } else {
                    // save a copy of the mesh and change the reference in the prefab
                    var copy = Instantiate(mesh);
                    AssetDatabase.CreateAsset(copy, $"{parentFolderPath}/{mesh.name}.asset");

                    foreach (var r in prefabAsset.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                        if (r.sharedMesh == mesh) {
                            r.sharedMesh = copy;
                        }
                    }
                }
            }
        }

        static void MoveTextures(GameObject sourcePrefabInstance, string parentFolderPath) {
            var textures = sourcePrefabInstance.GetComponentsInChildren<Renderer>()
                .SelectMany(r => r.sharedMaterials)
                .SelectMany(GetTextures)
                .Distinct();

            foreach (var texture in textures) {
                if (!AssetDatabase.IsSubAsset(texture)) {
                    AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(texture), $"{parentFolderPath}/{texture.name}.png");
                } else {
                    Log.Important?.Error("Texture is embedded in the fbx: " + texture.name, texture);
                }
            }
        }

        static IEnumerable<Texture> GetTextures(Object targetObj) {
            foreach (Object dependency in EditorUtility.CollectDependencies(new Object[] { targetObj })) {
                if (dependency is Texture texture) {
                    yield return texture;
                }
            }
        }
    }
}
