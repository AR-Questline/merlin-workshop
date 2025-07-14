using Awaken.TG.Assets;
using Awaken.TG.Main.Utility.UI.Feedbacks;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    public abstract class VSpriteMapMarker<T> : VMapMarker<T> where T : SpriteMapMarker {
        [Title("Highlight")]
        [SerializeField] VCScale highlightFeedback;
        [SerializeField] Image highlightImage;
        [Title("Icon")] 
        [SerializeField] Image iconImage;

        protected Image IconImage => iconImage;
        SpriteReference _spriteReference;
        
        protected override void Awake() {
            base.Awake();
            highlightFeedback.TrySetActiveOptimized(false);
        }
        
        protected override void OnInitialize() {
            base.OnInitialize();
            InitSprite();
        }
        
        void InitSprite() {
            if (Target.Icon is { IsSet: true } icon) {
                _spriteReference = icon.Get();
                _spriteReference.RegisterAndSetup(this, IconImage, (_, _) => {
                    StartHighlightAnimation();
                });
            }
        }
        
        void ReleaseSprite() {
            if ((_spriteReference?.IsSet ?? false) == false) {
                return;
            }
            
            iconImage.sprite = null;
            _spriteReference.Release();
            _spriteReference = null;
        }
        
        void StartHighlightAnimation() {
            if (Target.UseHighlightAnimation) {
                highlightFeedback.TrySetActiveOptimized(true);
                highlightFeedback.Play();
            }
        }
        
        protected override IBackgroundTask OnDiscard() {
            ReleaseSprite();
            return base.OnDiscard();
        }
    }
}
