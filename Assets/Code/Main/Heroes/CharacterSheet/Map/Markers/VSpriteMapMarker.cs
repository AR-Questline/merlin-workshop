using Awaken.TG.Assets;
using Awaken.TG.Main.Utility.UI.Feedbacks;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    public abstract class VSpriteMapMarker<T> : VMapMarker<T> where T : SpriteMapMarker {
        [Title("Highlight")]
        [SerializeField] VCScale highlightFeedback;
        [SerializeField] SpriteRenderer highlightSpriteRenderer;
        [Title("Icon")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] BoxCollider interactArea;

        protected SpriteRenderer SpriteRenderer => spriteRenderer;
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
                _spriteReference.SetSprite(spriteRenderer, (spriteRenderer, _) => {
                    spriteRenderer.sortingOrder = Target.Order;
                    highlightSpriteRenderer.sortingOrder = Target.Order;
                    interactArea.size = spriteRenderer.size;
                    StartHighlightAnimation();
                });
            }
        }
        
        void ReleaseSprite() {
            if ((_spriteReference?.IsSet ?? false) == false) {
                return;
            }
            
            spriteRenderer.sprite = null;
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
