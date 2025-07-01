using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    [UsesPrefab("Items/TooltipSystem/" + nameof(VItemTooltipSystemUI))]
    public class VItemTooltipSystemUI : VCompareTooltipSystemUI {
        [SerializeField] VCItemBaseTooltipUI tooltipToCompare;
        
        protected override void RefreshContent(IItemDescriptor descriptor) {
            tooltipMain.RefreshContent(descriptor, ItemTooltip.DescriptorToCompare);
            tooltipToCompare.RefreshContent(ItemTooltip.DescriptorToCompare, null);
        }
    }
}