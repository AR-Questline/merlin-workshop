using System;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipDescriptionsGemComponent : ItemTooltipDescriptionsBaseComponent<GemAttached> {
        public override void ToggleSectionActive(bool active) {
            SetParentSectionVisibility(active);
        }

        protected override void Setup(IItemDescriptor descriptor, View view) {
            bool hasContent = descriptor.Gems.Any() || descriptor.GemsSlot.Any();
            PrepareDescription(descriptor.Gems, view);
            
            foreach (var slot in descriptor.GemsSlot) {
                var element = _elementPool.Get();
                PrepareDescription(slot, element);
            }

            Visibility.SetInternal(hasContent);
        }

        protected override void PrepareItemDescription(GemAttached item, ItemDescriptionElement descriptionElement, View view) {
            var gemItem = descriptionElement.AddItemIcon(item.Template, view, item.GemLevel, item.GemNgPlusLevel);
            descriptionElement.Setup(ParentSection, item.Description(gemItem), ItemSlotUI.VisibilityConfig.OnlyIcon, item.DisplayName);        
        }
        
        void PrepareDescription(string item, ItemDescriptionElement descriptionElement) {
            descriptionElement.Setup(ParentSection, item, ItemSlotUI.VisibilityConfig.OnlyEmpty );
        }
    }
}