using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems {
    [UsesPrefab("Gems/VGemBaseUI")]
    public class VGemBaseUI : View<IGemBase> {
        [field: SerializeField] public VGenericPromptUI GemPrompt { get; private set; }
        [field: SerializeField] public Transform ItemsHost { get; private set; }
        [field: SerializeField] public float FadeDuration { get; private set; } = 1;
        
        [Space]
        [SerializeField] CanvasGroup rightSide;
        
        Tween _fadeTween;

        protected override void OnFullyInitialized() {
            Target.ListenTo(IGemBase.Events.AfterRefreshed, OnActionPerformed, this);
            Target.ListenTo(IGemBase.Events.ClickedItemChanged, OnClickedItemChanged, this);
        }

        protected virtual void OnClickedItemChanged(Item item) {
            if (item) {
                FadeInRightSide();
            } else {
                FadeOutRightSide();
            }
        }

        protected virtual void OnActionPerformed() { }

        protected void FadeInRightSide() {
            Fade(1f);
        }

        protected virtual void FadeOutRightSide() {
            Fade(0f);
            Target.RefreshPrompt(false);
        }

        public void HideRightSide() {
            _fadeTween.Kill();
            rightSide.alpha = 0;
        }
        
        void Fade(float targetAlpha) {
            _fadeTween.Kill();
            _fadeTween = rightSide.DOFade(targetAlpha, FadeDuration).SetUpdate(true);
        }
    }
}