using Awaken.TG.Main.Locations.Gems;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    [UsesPrefab("Items/TooltipSystem/" + nameof(VSharpenTooltipSystemUI))]
    public class VSharpenTooltipSystemUI : VItemTooltipSystemUI {
        protected override void OnInitialize() {
            base.OnInitialize();
            World.EventSystem.ListenTo(EventSelector.AnySource, IGemBase.Events.AfterUpgraded, this, HighlightTooltip);
            World.EventSystem.ListenTo(EventSelector.AnySource, IGemBase.Events.ClickedItemChanged, this, HideHighlight);
        }
    }
}