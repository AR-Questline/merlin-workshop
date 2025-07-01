using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.MVC.Attributes;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    [UsesPrefab("Items/TooltipSystem/" + nameof(VItemDiscoveredTooltipSystemUI))]
    public class VItemDiscoveredTooltipSystemUI : VCraftingTooltipSystemUI, IPromptHost {
        [field: SerializeField] public Transform PromptsHost { get; private set; }
        
        public Tween ShowTooltip(float duration) {
            return MainCanvasGroup.DOFade(1, duration).SetUpdate(true);
        }

        public void RefreshTooltipContent(IItemDescriptor descriptor) {
            RefreshContent(descriptor);
        }

        protected override bool TryAppear() {
            return false;
        }
    }
}