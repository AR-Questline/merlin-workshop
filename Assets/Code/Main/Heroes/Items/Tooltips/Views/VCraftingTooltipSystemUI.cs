using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    [UsesPrefab("Items/TooltipSystem/" + nameof(VCraftingTooltipSystemUI))]
    public class VCraftingTooltipSystemUI : VBaseTooltipSystemUI {
        public void DisappearTooltip() {
            MainCanvasGroup.alpha = 0f;
        }
    }
}