using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.Utility.Paths;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets {
    public class CreatePrefabWindow : OdinEditorWindow {
        public List<GameObject> models = new();

        [MenuItem("Assets/TG/Create Prefab")]
        static void OpenWindow() {
            var window = GetWindow<CreatePrefabWindow>();
            window.TryFillFromSelection();
            window.Show();
        }

        void TryFillFromSelection() {
            models.AddRange(Selection.objects.SelectMany(ExtractModelsFromSelected));
        }

        IEnumerable<GameObject> ExtractModelsFromSelected(Object selected) {
            if (selected is DefaultAsset folder) {
                var folderPath = AssetDatabase.GetAssetPath(folder);
                return AssetDatabase.FindAssets("t:Model", new[] { folderPath })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<GameObject>);
            }
            if (selected is GameObject gameObject && PrefabUtility.GetPrefabAssetType(gameObject) == PrefabAssetType.Model) {
                return gameObject.Yield();
            }
            return Array.Empty<GameObject>();
        }

        [Button]
        void Create() {
            var paths = models.Select(AssetDatabase.GetAssetPath)
                    .Where(PrefabCreation.IsModel)
                    .Where(PrefabCreation.IsNotAnimationModel)
                    .Select(PathUtils.AssetToFileSystemPath);
            foreach (var path in paths) {
                CreatePrefabFor(path);
            }
        }

        void CreatePrefabFor(string path) {
            string directory = PathUtils.ParentDirectory(Path.GetDirectoryName(path));
            
            var mesh = AssetDatabase.LoadAssetAtPath<GameObject>(PathUtils.FilesystemToAssetPath(path));
            var textures = MaterialCreation.GetTexturesIn(directory);

            OnDemandCache<string, Material> createdMaterials = new(materialName =>
                MaterialCreation.CreateMaterialFromTextures(materialName, textures[materialName]));

            foreach (var renderer in mesh.GetComponentsInChildren<Renderer>()) {
                var meshName = renderer switch {
                    MeshRenderer                            => renderer.GetComponent<MeshFilter>().sharedMesh.name,
                    SkinnedMeshRenderer skinnedMeshRenderer => skinnedMeshRenderer.sharedMesh.name,
                    _                                       => string.Empty,
                };

                Material[] materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < renderer.sharedMaterials.Length; i++) {
                    var material = renderer.sharedMaterials[i];
                    var modelMaterialName = material?.name ?? string.Empty;
                    var materialName = MaterialCreation.MaterialName(modelMaterialName, meshName);
                    if (textures.ContainsKey(materialName)) {
                        materials[i] = createdMaterials[materialName];
                    } else {
                        var sharedMaterial = renderer.sharedMaterials[i];
                        if (createdMaterials.Contains(sharedMaterial.name)) {
                            sharedMaterial = createdMaterials[sharedMaterial.name];
                        }
                        materials[i] = sharedMaterial;
                        createdMaterials[sharedMaterial.name] = sharedMaterial;
                        Log.Important?.Info($"<color=red>Cannot find textures for material {materialName} for renderer {renderer.name} for prefab {mesh.name}.</color>");
                    }
                }
                renderer.sharedMaterials = materials;
            }
            
            OnPostprocessModel(path, createdMaterials.Values.ToArray());
            
            PrefabCreation.CreatePrefabFromMesh(new PathSourcePair<GameObject>(path, mesh));
        }

        void OnPostprocessModel(string assetPath, Material[] setMaterials) {
            ModelImporter assetImporter = (ModelImporter)AssetImporter.GetAtPath(PathUtils.FilesystemToAssetPath(assetPath));
            ApplyMaterialsToImporter(setMaterials, assetImporter);
            assetImporter.importAnimation = false;
            assetImporter.importConstraints = false;

            assetImporter.SaveAndReimport();
        }

        // Probably this is not required any more, but there is no time to check this hypothesis.
        static void ApplyMaterialsToImporter(Material[] setMaterials, ModelImporter assetImporter) {
            using var so = new SerializedObject(assetImporter);
            var materials = so.FindProperty("m_Materials");
            var externalObjects = so.FindProperty("m_ExternalObjects");

            for (int materialIndex = 0; materialIndex < materials.arraySize; materialIndex++) {
                var id = materials.GetArrayElementAtIndex(materialIndex);
                var nameProperty = id.FindPropertyRelative("name").stringValue;
                var type = id.FindPropertyRelative("type").stringValue;
                var assembly = id.FindPropertyRelative("assembly").stringValue;

                SerializedProperty materialProperty = null;

                for (int externalObjectIndex = 0; externalObjectIndex < externalObjects.arraySize; externalObjectIndex++) {
                    var currentSerializedProperty = externalObjects.GetArrayElementAtIndex(externalObjectIndex);
                    var externalName = currentSerializedProperty.FindPropertyRelative("first.name").stringValue;
                    var externalType = currentSerializedProperty.FindPropertyRelative("first.type").stringValue;

                    if (externalType == type && externalName == nameProperty) {
                        materialProperty = currentSerializedProperty.FindPropertyRelative("second");
                        break;
                    }
                }

                if (materialProperty == null) {
                    var lastIndex = externalObjects.arraySize++;
                    var currentSerializedProperty = externalObjects.GetArrayElementAtIndex(lastIndex);
                    currentSerializedProperty.FindPropertyRelative("first.name").stringValue = nameProperty;
                    currentSerializedProperty.FindPropertyRelative("first.type").stringValue = type;
                    currentSerializedProperty.FindPropertyRelative("first.assembly").stringValue = assembly;
                    currentSerializedProperty.FindPropertyRelative("second").objectReferenceValue =
                        setMaterials[materialIndex];
                } else {
                    materialProperty.objectReferenceValue = setMaterials[materialIndex];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}