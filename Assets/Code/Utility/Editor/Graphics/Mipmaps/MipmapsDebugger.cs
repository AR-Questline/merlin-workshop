using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Graphics.Mipmaps;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace Awaken.Utility.Editor.Graphics.Mipmaps {
    [ExecuteAlways]
    internal class MipmapsDebugger : OdinEditorWindow {
        [ShowInInspector, LabelText("@"+nameof(RegisteredNonStreamableTitle))]
        HashSet<Texture2D> RegisteredNonStreamingTextures => MipmapsStreamingMasterTextures.nonStreamingTextures;
        [ShowInInspector, SerializeField, LabelText("@"+nameof(RegisteredUnityTitle))]
        List<Texture> _unityNonStreamingTextures = new List<Texture>();

        [ShowInInspector]
        List<(Renderer, List<Texture>)> _unityRendererTexture = new List<(Renderer, List<Texture>)>();

        bool IsPlaying => EditorApplication.isPlaying;

        [MenuItem("TG/Assets/Textures/Mipmaps debugger")]
        static void ShowWindow() {
            EditorWindow.GetWindow<MipmapsDebugger>().Show();
        }

        [Button]
        void CollectUnityNonStreamingTextures() {
            var rootsBuffer = new List<GameObject>(1024);
            var renderersBuffer = new List<Renderer>(4096);
            for (var s = 0; s < SceneManager.sceneCount; s++) {
                var currentScene = SceneManager.GetSceneAt(s);
                currentScene.GetRootGameObjects(rootsBuffer);

                foreach (GameObject obj in rootsBuffer) {
                    obj.GetComponentsInChildren(true, renderersBuffer);
                    foreach (Renderer renderer in renderersBuffer) {
                        Material[] materials = renderer.sharedMaterials;

                        var localTexturesList = new List<Texture>();

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
                                    if (texture is Texture2D texture2D) {
                                        if (!texture2D.streamingMipmaps) {
                                            _unityNonStreamingTextures.Add(texture2D);
                                            localTexturesList.Add(texture2D);
                                        }
                                    } else if (texture is Cubemap cubemap) {
                                        if (!cubemap.streamingMipmaps) {
                                            _unityNonStreamingTextures.Add(cubemap);
                                            localTexturesList.Add(cubemap);
                                        }
                                    } else if (texture != null) {
                                        _unityNonStreamingTextures.Add(texture);
                                        localTexturesList.Add(texture);
                                    }
                                }
                            }
                        }

                        if (localTexturesList.Count > 0) {
                            _unityRendererTexture.Add((renderer, localTexturesList));
                        }
                    }
                    renderersBuffer.Clear();
                }

                rootsBuffer.Clear();
            }

            _unityNonStreamingTextures = _unityNonStreamingTextures.Distinct().ToList();
        }

        [Button, HideIf(nameof(IsPlaying))]
        void ConvertToStreamable() {
            var nonStreamingTextures = RegisteredNonStreamingTextures;
            foreach (var texture in nonStreamingTextures) {
                if (texture != null) {
                    ConvertToStreamable(texture);
                }
            }

            foreach (var texture in _unityNonStreamingTextures) {
                if (texture is Texture2D texture2D && texture2D != null) {
                    ConvertToStreamable(texture2D);
                }
            }
        }

        void ConvertToStreamable(Texture2D texture) {
            var path = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer) {
                importer.isReadable = false;
                importer.streamingMipmaps = true;
                importer.SaveAndReimport();
            } else {
                Log.Minor?.Error($"Texture {texture.name} has no importer");
            }
        }

        string RegisteredNonStreamableTitle {
            get {
                var texturesSize = 0L;
                foreach (var texture in RegisteredNonStreamingTextures) {
                    // Get texture size in bytes
                    texturesSize += Profiler.GetRuntimeMemorySizeLong(texture);
                }

                return $"Registered non-streamable textures: {RegisteredNonStreamingTextures.Count} ({M.HumanReadableBytes(texturesSize)})";
            }
        }

        string RegisteredUnityTitle {
            get {
                var texturesSize = 0L;
                foreach (var texture in _unityNonStreamingTextures) {
                    // Get texture size in bytes
                    texturesSize += Profiler.GetRuntimeMemorySizeLong(texture);
                }

                return $"Unity non-streamable textures: {_unityNonStreamingTextures.Count} ({M.HumanReadableBytes(texturesSize)})";
            }
        }
    }
}
