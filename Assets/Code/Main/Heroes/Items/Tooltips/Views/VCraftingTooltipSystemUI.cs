using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using DG.Tweening;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    [UsesPrefab("Items/TooltipSystem/" + nameof(VCraftingTooltipSystemUI))]
    public class VCraftingTooltipSystemUI : VBaseTooltipSystemUI {
        Tween _fadeTween;
        
        protected override void OnMount() {
            World.EventSystem.ListenTo(EventSelector.AnySource, CraftingItemTooltipUI.Events.ResultTooltipDisplayed, this, DisappearTooltip);
        }

        void DisappearTooltip(bool resultDisplayed) {
            if (resultDisplayed) {
                HighlightTooltip();
            } else {
                _fadeTween.Kill();
                _fadeTween = MainCanvasGroup.DOFade(0f, 0.25f).SetUpdate(true);
            }
        }
    }
}