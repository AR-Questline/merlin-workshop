using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    [UsesPrefab("Items/TooltipSystem/" + nameof(VSimpleTooltipSystemUI))]
    public class VSimpleTooltipSystemUI : VFloatingTooltipSystemUI {
        public void Show(string tittle, string desc) {
            _isVisible.Set(true);
            Target.View<VCSimpleTooltip>().RefreshContent(tittle, desc);
        }
        
        public void Hide() {
            _isVisible.Set(false);
        }
    }
}