using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public class VCMysteriousConsumableTooltip : ViewComponent<ExperimentalCooking> {
        [SerializeField] float fadeDuration = 0.5f;
        [SerializeField] CanvasGroup canvasGroup;
        
        Tween _fadeTween;
        
        protected override void OnAttach() {
            World.EventSystem.ListenTo(EventSelector.AnySource, CraftingItemTooltipUI.Events.ResultTooltipDisplayed, this, HandleVisibility);
        }

        void HandleVisibility(bool resultVisible) {
            _fadeTween.Kill();
            _fadeTween = canvasGroup.DOFade(resultVisible ? 0f : 1f, fadeDuration).SetUpdate(true);
        }
    }
}