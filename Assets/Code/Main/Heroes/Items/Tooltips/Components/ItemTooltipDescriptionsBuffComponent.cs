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
        bool _hasContent;
        
        public ItemTooltipDescriptionsBuffComponent(DescriptionComponentConfig config) : base(config) { }
        
        public override void ToggleSectionActive(bool active) {
            if (_hasContent) {
                SetParentSectionVisibility(active);
            }
        }

        protected override void Setup(IItemDescriptor descriptor, View view) {
            _hasContent = descriptor.Buffs.Any();
            PrepareDescription(descriptor.Buffs, view);
            
            SetParentSectionVisibility(_hasContent);
            UseReadMore = _hasContent;
        }
        
        protected override void PrepareItemDescription(AppliedItemBuff item, ItemDescriptionElement descriptionElement, View view) {
            Item buffItem = descriptionElement.AddItemIcon(item.Template, view);
            ExistingItemDescriptor descriptor = new (buffItem);
            
            string nameLabel = $"{item.DisplayName} ({item.SecondsLeft}{LocTerms.SecondsAbbreviation.Translate()})";
            descriptionElement.Setup(ParentSection, descriptor.ItemDescription, ItemSlotUI.VisibilityConfig.OnlyIcon, nameLabel);
        }
    }
}