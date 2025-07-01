using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    public abstract class VBaseTooltipSystemUI : VFloatingTooltipSystemUI {
        [SerializeField] protected VCItemBaseTooltipUI tooltipMain;
        
        protected ItemTooltipUI ItemTooltip => (ItemTooltipUI)Target;
        
        bool _comparerVisible;
        
        protected override void OnInitialize() {
            base.OnInitialize();
            ItemTooltip.ListenTo(ItemTooltipUI.Events.ItemDescriptorChanged, CheckVisibility, this);
        }

        void CheckVisibility(Change<IItemDescriptor> change) {
            _isVisible.Set(change.to != null);
        }

        protected virtual void RefreshContent(IItemDescriptor descriptor) {
            tooltipMain.RefreshContent(descriptor, null);
        }

        protected override bool TryAppear() {
            if (ItemTooltip.Descriptor == null) {
                return false;
            }

            RefreshContent(ItemTooltip.Descriptor);
            return true;
        }
    }
}