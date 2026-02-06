using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Assets {
    public class LazyImage : MonoBehaviour {
        // === References
        public Image image;
        public bool preserveAspect;
        [ARAssetReferenceSettings(new []{typeof(Sprite), typeof(Texture2D)}, true)]
        public ARAssetReference arSpriteReference;
        [SerializeField] bool preserveOnDisable;
        
        SpriteReference _spriteReference;

        // === Unity lifetime
        void OnEnable() {
            if (_spriteReference == null) {
                _spriteReference = new SpriteReference {arSpriteReference = arSpriteReference};
                SetSprite();
            } else if (!preserveOnDisable) {
                SetSprite();
            }
        }

        void SetSprite() {
            _spriteReference.SetSprite(image, (_, _) => {
                if (!image.preserveAspect) {
                    image.preserveAspect = preserveAspect;
                }
            });
        }
        
        void ReleaseSprite() {
            image.sprite = null;
            _spriteReference?.Release();
            _spriteReference = null;
        }

        void OnDisable() {
            if (!preserveOnDisable) {
                ReleaseSprite();
            }
        }

        void OnDestroy() {
            ReleaseSprite();
        }
    }
}