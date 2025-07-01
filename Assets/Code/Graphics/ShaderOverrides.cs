using System;
using Awaken.TG.Main.General;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Graphics {
    [Serializable]
    public class ShaderOverrides {
        public FloatOverride[] floats;
        public ColorOverride[] colors;
        public TextureOverride[] textures;
        public KeywordOverride[] keywords;
        public TagOverride[] tags;
        public ConditionalInt renderQueue;

        public void Apply(Material material, float duration = 0) {
            if (floats != null) {
                foreach (var f in floats) {
                    if (duration > 0) {
                        material.DOFloat(f.value, f.name, duration);
                    } else {
                        material.SetFloat(f.name, f.value);
                    }
                }
            }
            if (colors != null) {
                foreach (var c in colors) {
                    if (duration > 0) {
                        material.DOColor(c.value, c.name, duration);
                    } else {
                        material.SetColor(c.name, c.value);
                    }
                }
            }
            if (textures != null) {
                foreach (var t in textures) {
                    material.SetTexture(t.name, t.value);
                }
            }
            if (keywords != null) {
                foreach (var keyword in keywords) {
                    if (keyword.value) {
                        material.EnableKeyword(keyword.name);
                    } else {
                        material.DisableKeyword(keyword.name);
                    }
                }
            }
            if (tags != null) {
                foreach (var tag in tags) {
                    material.SetOverrideTag(tag.name, tag.value);
                }
            }

            if (renderQueue) {
                material.renderQueue = renderQueue.value;
            }
        }
        
        [Serializable]
        public class FloatOverride {
            public string name;
            public float value;
        }
        
        [Serializable]
        public class ColorOverride {
            public string name;
            public Color value;
        }
        
        [Serializable]
        public class TextureOverride {
            public string name;
            public Texture2D value;
        }
        
        [Serializable]
        public class KeywordOverride {
            public string name;
            public bool value;
        }
        
        [Serializable]
        public class TagOverride {
            public string name;
            public string value;
        }
    }
}