using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    [UsesPrefab("Items/TooltipSystem/" + nameof(VBagItemTooltipSystemUI))]
    public class VBagItemTooltipSystemUI : VCompareTooltipSystemUI {
        [SerializeField] protected VCItemTooltipUI tooltipToCompare;
        
        protected override void RefreshContent(IItemDescriptor descriptor) {
            SetComparerState(ItemTooltip.DescriptorToCompare != null);
            tooltipMain.RefreshContent(descriptor, ItemTooltip.DescriptorToCompare);
            tooltipToCompare.RefreshContent(ItemTooltip.DescriptorToCompare, null);
        }
    }
}