using System;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipDescriptionsBuffComponent : ItemTooltipDescriptionsBaseComponent<AppliedItemBuff> {
        public override void ToggleSectionActive(bool active) {
            SetParentSectionVisibility(active);
        }

        protected override void Setup(IItemDescriptor descriptor, View view) {
            bool hasContent = descriptor.Buffs.Any();
            PrepareDescription(descriptor.Buffs, view);
            
            UseReadMore = hasContent;
            Visibility.SetInternal(hasContent);
        }
        
        protected override void PrepareItemDescription(AppliedItemBuff item, ItemDescriptionElement descriptionElement, View view) {
            Item buffItem = descriptionElement.AddItemIcon(item.Template, view, item.BuffItemLevel, item.BuffNgPlusLevel);
            ExistingItemDescriptor descriptor = new (buffItem);
            
            string nameLabel = $"{item.DisplayName} ({item.SecondsLeft}{LocTerms.SecondsAbbreviation.Translate()})";
            descriptionElement.Setup(ParentSection, descriptor.ItemDescription, ItemSlotUI.VisibilityConfig.OnlyIcon, nameLabel);
        }
    }
}