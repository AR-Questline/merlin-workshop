using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Graphics {
    public class TexturesMipmapStreamingConverter : EditorWindow {
        [SerializeField] List<Texture2D> _toConvert = new();
        [SerializeField] List<Texture2D> _notConverted = new();
        [SerializeField] List<Texture2D> _invalidTexturesList = new();
        [SerializeField] List<Texture> _allNonStreamingTextures = new();

        [SerializeField] List<Renderer> _whoUseThisTexture = new();
        [SerializeField] Texture2D _textureToBeUsed;

        HashSet<Texture2D> _invalidTextures = new();

        SerializedObject _serializedObject;
        SerializedProperty _toConvertProperty;
        SerializedProperty _notConvertedProperty;
        SerializedProperty _whoUseThisTextureProperty;
        SerializedProperty _textureToBeUsedProperty;
        SerializedProperty _invalidTexturesListProperty;
        SerializedProperty _allNonStreamingTexturesProperty;
        Vector2 _scroll;

        void OnEnable() {
            _serializedObject = new(this);
            _toConvertProperty = _serializedObject.FindProperty(nameof(_toConvert));
            _notConvertedProperty = _serializedObject.FindProperty(nameof(_notConverted));
            _whoUseThisTextureProperty = _serializedObject.FindProperty(nameof(_whoUseThisTexture));
            _textureToBeUsedProperty = _serializedObject.FindProperty(nameof(_textureToBeUsed));
            _invalidTexturesListProperty = _serializedObject.FindProperty(nameof(_invalidTexturesList));
            _allNonStreamingTexturesProperty = _serializedObject.FindProperty(nameof(_allNonStreamingTextures));
        }

        void OnGUI() {
            var dragRect = GUILayoutUtility.GetRect(position.width, EditorGUIUtility.singleLineHeight*3);
            ManageDragAndDrop(dragRect);

            if (_toConvert.IsNotNullOrEmpty() && GUILayout.Button("Convert to mipmaps streamable")) {
                AssetDatabase.StartAssetEditing();
                try {
                    for (int i = _toConvert.Count - 1; i >= 0; i--) {
                        Texture texture = _toConvert[i];
                        MakeTextureMipmapStreamable(texture);
                        _toConvert.RemoveAt(i);
                    }
                } finally {
                    AssetDatabase.StopAssetEditing();
                }
            }

            _serializedObject.Update();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.PropertyField(_toConvertProperty);
            if (Application.isPlaying) {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Find not converted")) {
                    FindAllNotConvertedTextures();

                    _serializedObject.ApplyModifiedProperties();
                    _serializedObject.Update();
                }
                if (GUILayout.Button("Add drake additional")) {
                    FindAllNotConvertedDrakeAdditionalTextures();

                    _serializedObject.ApplyModifiedProperties();
                    _serializedObject.Update();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.PropertyField(_notConvertedProperty);

            if (_notConvertedProperty.arraySize > 0 && GUILayout.Button("Add to convert")) {
                _serializedObject.ApplyModifiedProperties();
                _toConvert.AddRange(_notConverted);
                _serializedObject.Update();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_textureToBeUsedProperty);
            if (GUILayout.Button("Find who use this texture")) {
                _serializedObject.ApplyModifiedProperties();
                FindWhoUsesTexture();
                _serializedObject.Update();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_whoUseThisTextureProperty);

            EditorGUILayout.PropertyField(_invalidTexturesListProperty);
            EditorGUILayout.BeginHorizontal();
            if (_invalidTexturesListProperty.arraySize > 0 && GUILayout.Button("Convert to NON mipmaps streamable")) {
                _serializedObject.ApplyModifiedProperties();
                AssetDatabase.StartAssetEditing();
                try {
                    for (int i = _invalidTexturesList.Count - 1; i >= 0; i--) {
                        Texture texture = _invalidTexturesList[i];
                        MakeTextureNonMipmapStreamable(texture);
                    }
                } finally {
                    AssetDatabase.StopAssetEditing();
                }
                _serializedObject.Update();
            }
            if (GUILayout.Button("Find invalid textures")) {
                _serializedObject.ApplyModifiedProperties();
                CollectInvalidTextures();
                _serializedObject.Update();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Find all non streamable")) {
                _serializedObject.ApplyModifiedProperties();
                CollectAllNonStreamable();
                _serializedObject.Update();
            }
            EditorGUILayout.PropertyField(_allNonStreamingTexturesProperty);

            EditorGUILayout.EndScrollView();
            _serializedObject.ApplyModifiedProperties();
        }

        void FindAllNotConvertedTextures() {
            CollectInvalidTextures();
            _notConverted.Clear();

            var notConverted = new HashSet<Texture2D>();
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            // Find all textures that are not mipmaps streamable
            CollectTexturesFromRenderers(allRenderers, notConverted, t => !_invalidTextures.Contains(t) && !t.streamingMipmaps);
            CollectTexturesFromDrake(notConverted, t => !_invalidTextures.Contains(t) && !t.streamingMipmaps);
            _notConverted = notConverted.ToList();
        }

        void FindAllNotConvertedDrakeAdditionalTextures() {
            CollectInvalidTextures();

            var notConverted = new HashSet<Texture2D>();
            // Find all textures that are not mipmaps streamable
            CollectTexturesFromDrake(notConverted, t => !_invalidTextures.Contains(t) && !t.streamingMipmaps);
            notConverted.UnionWith(_notConverted);
            _notConverted = notConverted.ToList();
        }

        void CollectInvalidTextures() {
            _invalidTextures.Clear();

            // === Terrains
            var terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            _invalidTextures.AddRange(terrains
                    .SelectMany(static t => t.terrainData.terrainLayers)
                    .SelectMany(static l => new[] {
                        l.diffuseTexture, l.maskMapTexture, l.normalMapTexture,
                    })
                );

            // === Vegetation
            // var systems = VegetationStudioManager.Instance.VegetationSystemList;
            // var vegetationData = systems.SelectMany(static s => s.VegetationPackageProList)
            //     .SelectMany(static l => l.VegetationInfoList)
            //     .GroupBy(static vi => vi.PrefabType)
            //     .ToDictionary(static g => g.Key, static g => g.ToArray());
            // if (vegetationData.TryGetValue(VegetationPrefabType.Texture, out var texturesData)) {
            //     foreach (var itemInfo in texturesData) {
            //         if (itemInfo.VegetationTexture != null) {
            //             _invalidTextures.Add(itemInfo.VegetationTexture);
            //         }
            //     }
            // }
            // if (vegetationData.TryGetValue(VegetationPrefabType.Mesh, out var prefabData)) {
            //     foreach (var itemInfo in prefabData) {
            //         if (itemInfo.VegetationPrefab == null) continue;
            //         var renderers = itemInfo.VegetationPrefab.GetComponentsInChildren<Renderer>();
            //         CollectTexturesFromRenderers(renderers, _invalidTextures, static _ => true);
            //     }
            // }

            _invalidTexturesList = _invalidTextures.ToList();
        }

        void FindWhoUsesTexture() {
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            // Find all textures that are not mipmaps streamable
            foreach (var renderer in allRenderers) {
                var materials = renderer.sharedMaterials;
                foreach (var material in materials) {
                    if (material == null) continue;
                    var textures = CollectTexturesFromMaterial(material);
                    if (textures.Any(texture => texture == _textureToBeUsed)) {
                        _whoUseThisTexture.Add(renderer);
                        break;
                    }
                }
            }
        }

        void CollectAllNonStreamable() {
            for (var s = 0; s < SceneManager.sceneCount; s++) {
                var currentScene = SceneManager.GetSceneAt(s);
                var allObjects = currentScene.GetRootGameObjects();

                foreach (GameObject obj in allObjects) {
                    Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers) {
                        Material[] materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++) {
                            Material mat = materials[i];
                            if (mat == null) {
                                continue;
                            }

                            int propertyCount = ShaderUtil.GetPropertyCount(mat.shader);
                            for (int j = 0; j < propertyCount; j++) {
                                if (ShaderUtil.GetPropertyType(mat.shader, j) == ShaderUtil.ShaderPropertyType.TexEnv) {
                                    string propertyName = ShaderUtil.GetPropertyName(mat.shader, j);
                                    Texture texture = mat.GetTexture(propertyName);
                                    if (texture is Texture2D { streamingMipmaps: false } texture2D) {
                                        _allNonStreamingTextures.Add(texture2D);
                                    } else if (texture is Cubemap { streamingMipmaps: false } cubemap) {
                                        _allNonStreamingTextures.Add(cubemap);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            _allNonStreamingTextures = _allNonStreamingTextures.Distinct().ToList();
        }

        // === Helpers
        static void CollectTexturesFromRenderers(Renderer[] renderers, HashSet<Texture2D> result, Func<Texture2D, bool> predicate) {
            foreach (var renderer in renderers) {
                var materials = renderer.sharedMaterials;
                foreach (var material in materials) {
                    if (material == null) continue;
                    var textures = CollectTexturesFromMaterial(material);
                    foreach (var texture in textures) {
                        if (texture == null) continue;
                        if (predicate(texture)) {
                            result.Add(texture);
                        }
                    }
                }
            }
        }

        static void CollectTexturesFromDrake(HashSet<Texture2D> result, Func<Texture2D, bool> predicate) {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            List<RenderMeshArray> renderers = new List<RenderMeshArray>();
            entityManager.GetAllUniqueSharedComponentsManaged(renderers);
            foreach (var renderer in renderers) {
                var materials = renderer.Materials;
                if (materials == null) continue;
                foreach (var material in materials) {
                    if (material == null) continue;
                    var textures = CollectTexturesFromMaterial(material);
                    foreach (var texture in textures) {
                        if (texture == null) continue;
                        if (predicate(texture)) {
                            result.Add(texture);
                        }
                    }
                }
            }
        }

        static List<Texture2D> CollectTexturesFromMaterial(Material material) {
            // Gather all textures from material
            var textures = new List<Texture2D>();
            var shader = material.shader;
            var destinationPropertyCount = shader.GetPropertyCount();
            for (int i = 0; i < destinationPropertyCount; i++) {
                var propertyId = shader.GetPropertyNameId(i);
                if (material.HasTexture(propertyId)) {
                    var texture = material.GetTexture(propertyId);
                    if (texture is Texture2D texture2D) {
                        textures.Add(texture2D);
                    }
                }
            }
            return textures;
        }

        static void MakeTextureMipmapStreamable(Texture texture) {
            if (null == texture) return;

            var assetPath = AssetDatabase.GetAssetPath(texture);
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter == null) {
                return;
            }
            if (!textureImporter.streamingMipmaps) {
                textureImporter.streamingMipmaps = true;
                textureImporter.SaveAndReimport();
            }
        }

        static void MakeTextureNonMipmapStreamable(Texture texture) {
            if (null == texture) return;

            var assetPath = AssetDatabase.GetAssetPath(texture);
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter == null) {
                return;
            }
            if (textureImporter.streamingMipmaps) {
                textureImporter.streamingMipmaps = false;
                textureImporter.SaveAndReimport();
            }
        }

        // === Drag&Drop
        void ManageDragAndDrop(Rect position) {
            var oldColor = GUI.backgroundColor;
            var dragValidTargets = DraggingValidTarget();
            if (dragValidTargets) {
                GUI.backgroundColor = Color.blue;
            }
            GUI.Box(position, "Drag here");
            GUI.backgroundColor = oldColor;

            if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition)) {
                DragAndDrop.visualMode = dragValidTargets ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                Event.current.Use();
            } else if (Event.current.type == EventType.DragPerform && position.Contains(Event.current.mousePosition)) {
                if (dragValidTargets) {
                    _toConvert.AddRange(DragAndDrop.objectReferences.SelectMany(ExtractTextures));
                    DragAndDrop.AcceptDrag();
                }
            }
        }

        static IEnumerable<Texture2D> ExtractTextures(Object assetReference) {
            if (assetReference is Texture2D texture) {
                return texture.Yield();
            }
            var directoryPath = AssetDatabase.GetAssetPath(assetReference);
            return AssetDatabase.FindAssets("t:Texture2D", new[] { directoryPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>);
        }

        static bool DraggingValidTarget() {
            var references = DragAndDrop.objectReferences;
            return references.IsNotNullOrEmpty() && references.All(static r => r is Texture or DefaultAsset);
        }

        // === Show
        [MenuItem("TG/Assets/Mipmaps streaming converter")]
        static void ShowWindow() {
            var window = GetWindow<TexturesMipmapStreamingConverter>();
            window.titleContent = new GUIContent("Mipmaps streaming converter");
            window.Show();
        }
    }
}
