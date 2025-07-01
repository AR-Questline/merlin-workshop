using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    [UsesPrefab("Items/TooltipSystem/" + nameof(VWyrdTalentTooltipSystemUI))]
    public class VWyrdTalentTooltipSystemUI : VFloatingTooltipSystemUI {
        string _tittle;
        string _currentDesc;
        string _nextDesc;
        
        public void Show(string tittle, string currentDesc, string nextDesc = null) {
            _isVisible.Set(true);
            _tittle = tittle;
            _currentDesc = currentDesc;
            _nextDesc = nextDesc;
        }
        
        public void Hide() {
            _isVisible.Set(false);
        }

        protected override bool TryAppear() {
            Target.View<VCWyrdTalentTooltip>().RefreshContent(_tittle, _currentDesc, _nextDesc);
            return base.TryAppear();
        }

        public void Refresh(string tittle, string currentDesc, string nextDesc = null) {
            Target.View<VCWyrdTalentTooltip>().RefreshContent(tittle, currentDesc, nextDesc);
        }
    }
}