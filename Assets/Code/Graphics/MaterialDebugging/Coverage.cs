using System.Collections.Generic;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Graphics.MaterialDebugging {
    public class Coverage : IMaterialDebugMode {
        Dictionary<Texture2D, TextureData> _textureData = new();
        
        public void Init(Renderer[] renderers) {
            foreach (var renderer in renderers) {
                var mesh = renderer switch {
                    MeshRenderer => renderer.GetComponent<MeshFilter>().sharedMesh,
                    SkinnedMeshRenderer skinnedMeshRenderer => skinnedMeshRenderer.sharedMesh,
                    _ => null
                };
                if (mesh != null) {
                    var uv = mesh.uv;
                    var triangles = mesh.triangles;
                    foreach (var material in renderer.sharedMaterials) {
                        if (material != null && GetMainTexture(material) is Texture2D texture2D) {
                            if (!_textureData.TryGetValue(texture2D, out var data)) {
                                data = new TextureData();
                                _textureData.Add(texture2D, data);
                            }

                            for (int i = 0; i < triangles.Length; i+=3) {
                                data.AddTriangle(uv[triangles[i]], uv[triangles[i + 1]], uv[triangles[i + 2]]);
                            }
                        }
                    }
                }
            }

            var shader = IMaterialDebugMode.GetDebugShader();
            var notComputedMaterial = new Material(shader);
            notComputedMaterial.SetColor(IMaterialDebugMode.ColorID, Color.black);
            
            foreach (var renderer in renderers) {
                var sharedMaterials = renderer.sharedMaterials;
                var replacementMaterials = new Material[sharedMaterials.Length];
                for (int i = 0; i < sharedMaterials.Length; i++) {
                    if (sharedMaterials[i] == null) {
                        Log.Important?.Error($"Material {i} is null", renderer.gameObject);
                        replacementMaterials[i] = sharedMaterials[i];
                    } else if (GetMainTexture(sharedMaterials[i]) is Texture2D texture2D && _textureData.TryGetValue(texture2D, out var data)) {
                        replacementMaterials[i] = data.Material(shader);
                    } else {
                        replacementMaterials[i] = notComputedMaterial;
                    }
                }
                renderer.SetReplacementMaterials(replacementMaterials);
            }
        }

        public void Clear(Renderer[] renderers) {
            _textureData.Clear();
            foreach (var renderer in renderers) {
                renderer.ResetReplacements();
            }
        }
        
        static readonly int MainTexID = Shader.PropertyToID("_MainTex");
        static readonly int AlbedoID = Shader.PropertyToID("_Albedo");
        static Texture GetMainTexture(Material material) {
            if (material.HasTexture(AlbedoID)) return material.GetTexture(AlbedoID);
            if (material.HasTexture(MainTexID)) return material.GetTexture(MainTexID);
            return null;
        }
        
        static Color AccuracyGradient(float percent) {
            return new(Mathf.Clamp01(2.0f * percent), Mathf.Clamp01(2.0f * (1 - percent)), 0);
        }

        class TextureData {
            const int Precision = 100;

            bool[,] _map = new bool[Precision, Precision];
            Material _material;

            public Material Material(Shader shader) {
                if (_material == null) {
                    _material = new Material(shader);
                    _material.SetColor(IMaterialDebugMode.ColorID, AccuracyGradient(Coverage));
                }
                return _material;
            }

            float Coverage {
                get {
                    int coverage = 0;
                    for (int x = 0; x < Precision; x++) {
                        for (int y = 0; y < Precision; y++) {
                            if (_map[x, y]) {
                                coverage++;
                            }
                        }
                    }
                    return (float) coverage / (Precision * Precision);
                }
            }

            public void AddTriangle(Vector2 v0, Vector2 v1, Vector2 v2) {

                Vector2Int intV0 = ToMapCoords(v0);
                Vector2Int intV1 = ToMapCoords(v1);
                Vector2Int intV2 = ToMapCoords(v2);
                
                Vector2Int edge20 = intV0 - intV2;
                Vector2Int edge01 = intV1 - intV0;
                Vector2Int edge12 = intV2 - intV1;

                int xMin = Mathf.Max(Mathf.Min(intV0.x, intV1.x, intV2.x), 0);
                int xMax = Mathf.Min(Mathf.Max(intV0.x, intV1.x, intV2.x), Precision - 1);
                int yMin = Mathf.Max(Mathf.Min(intV0.y, intV1.y, intV2.y), 0);
                int yMax = Mathf.Min(Mathf.Max(intV0.y, intV1.y, intV2.y), Precision - 1);

                for (int x = xMin; x <= xMax; x++) {
                    for (int y = yMin; y <= yMax; y++) {
                        if (Inside(x, y)) {
                            _map[x, y] = true;
                        }
                    }
                }

                static Vector2Int ToMapCoords(Vector2 v) {
                    return new((int)(Precision * v.x), (int)(Precision * v.y));
                }
                
                bool Inside(int x, int y) {
                    Vector2Int point = new(x, y);
                
                    var s = edge20.x * (point.y - intV2.y) - edge20.y * (point.x - intV2.x);
                    var t = edge01.x * (point.y - intV0.y) - edge01.y * (point.x - intV0.x);

                    if (s < 0 != t < 0 && s != 0 && t != 0) {
                        return false;
                    }

                    var d = edge12.x * (point.y - intV1.y) - edge12.y * (point.x - intV1.x);
                    return d == 0 || d < 0 == s + t <= 0;
                }
            }
        }
    }
}