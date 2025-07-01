using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Assets {
    public class LazyImage : MonoBehaviour {
        // === References
        public Image image;
        public bool preserveAspect;
        [ARAssetReferenceSettings(new []{typeof(Sprite), typeof(Texture2D)}, true)]
        public ARAssetReference arSpriteReference;
        
        SpriteReference _spriteReference;

        // === Unity lifetime
        void OnEnable() {
            _spriteReference ??= new SpriteReference {arSpriteReference = arSpriteReference};
            SetSprite();
        }

        void SetSprite() {
            _spriteReference.SetSprite(image, (_, _) => {
                if (!image.preserveAspect) {
                    image.preserveAspect = preserveAspect;
                }
            });
        }

        void OnDisable() {
            _spriteReference.Release();
        }
    }
}