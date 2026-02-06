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
                icon.Get().RegisterAndSetup(this, iconImage, (_, _) => {
                    StartHighlightAnimation();
                });
            }
        }
        
        void StartHighlightAnimation() {
            if (Target.UseHighlightAnimation) {
                highlightFeedback.TrySetActiveOptimized(true);
                highlightFeedback.Play();
            }
        }
        
        protected override IBackgroundTask OnDiscard() {
            iconImage.sprite = null;
            return base.OnDiscard();
        }
    }
}
