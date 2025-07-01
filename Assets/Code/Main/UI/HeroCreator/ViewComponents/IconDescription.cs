using Awaken.TG.Assets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.HeroCreator.ViewComponents {
    public class IconDescription {
        public Sprite sprite;
        public Color color = Color.white;
        public string text = null;
        
        [UnityEngine.Scripting.Preserve]
        public IconDescription(){}

        [UnityEngine.Scripting.Preserve]
        public IconDescription(Sprite sprite) {
            this.sprite = sprite;
        }

        [UnityEngine.Scripting.Preserve]
        public IconDescription(Color color) {
            this.color = color;
        }

        [UnityEngine.Scripting.Preserve]
        public IconDescription(Sprite sprite, Color color) {
            this.sprite = sprite;
            this.color = color;
        }

        [UnityEngine.Scripting.Preserve]
        public IconDescription(Color color, string text) {
            this.color = color;
            this.text = text;
        }

        [UnityEngine.Scripting.Preserve]
        public void Apply(Image image) {
            image.sprite = sprite;
            image.color = color;
        }

        [UnityEngine.Scripting.Preserve]
        public void Apply(SpriteRenderer renderer) {
            renderer.sprite = sprite;
            renderer.color = color;
        }

        public void TryToApply(Image image, TextMeshProUGUI textMesh = null) {
            if (sprite != null) {
                image.sprite = sprite;
            }
            image.color = color;

            if (text != null && textMesh != null) {
                textMesh.text = text;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public void TryToApply(SpriteRenderer renderer) {
            if (sprite != null) {
                renderer.sprite = sprite;
            }
            renderer.color = color;
        }
    }
}