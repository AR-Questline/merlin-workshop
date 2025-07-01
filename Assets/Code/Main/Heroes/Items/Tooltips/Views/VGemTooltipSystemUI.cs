using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    [UsesPrefab("Items/TooltipSystem/" + nameof(VGemTooltipSystemUI))]
    public class VGemTooltipSystemUI : VCompareTooltipSystemUI {
        [SerializeField] VCItemTooltipUI tooltipToCompare;
        
        protected override void RefreshContent(IItemDescriptor descriptor) {
            tooltipMain.RefreshContent(descriptor, ItemTooltip.DescriptorToCompare);
            tooltipToCompare.RefreshContent(ItemTooltip.DescriptorToCompare, null);
        }
    }
}