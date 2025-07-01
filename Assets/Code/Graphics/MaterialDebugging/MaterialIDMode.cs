using System.Collections.Generic;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Graphics.MaterialDebugging {
    public class MaterialIDMode : IMaterialDebugMode {

        Dictionary<Material, MaterialData> _materialData = new();

        public void Init(Renderer[] renderers) {
            int index = 0;
            var shader = IMaterialDebugMode.GetDebugShader();
            foreach (var renderer in renderers) {
                var sharedMaterials = renderer.sharedMaterials;
                var replacementMaterials = new Material[sharedMaterials.Length];
                for (int i = 0; i < sharedMaterials.Length; i++) {
                    if (sharedMaterials[i] != null) {
                        if (!_materialData.TryGetValue(sharedMaterials[i], out var data)) {
                            data = new MaterialData(IMaterialDebugMode.GetDistinctColor(index++), shader);
                            _materialData.Add(sharedMaterials[i], data);
                        }
                        replacementMaterials[i] = data.material;
                    } else {
                        Log.Important?.Error($"Material {i} is null", renderer.gameObject);
                        replacementMaterials[i] = sharedMaterials[i];
                    }
                }
                renderer.SetReplacementMaterials(replacementMaterials);
            }
        }

        public void Clear(Renderer[] renderers) {
            _materialData.Clear();
            foreach (var renderer in renderers) {
                renderer.ResetReplacements();
            }
        }

        class MaterialData {
            public int count;
            [UnityEngine.Scripting.Preserve] public Color color;
            public Material material;

            public MaterialData(Color color, Shader shader) {
                this.color = color;
                material = new Material(shader);
                material.SetColor(IMaterialDebugMode.ColorID, color);
            }
        }
    }
}