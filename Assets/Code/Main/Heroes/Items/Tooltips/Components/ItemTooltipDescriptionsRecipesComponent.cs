using System;
using System.Linq;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipDescriptionsRecipesComponent : ItemTooltipDescriptionsBaseComponent<IRecipe> {
        bool _hasContent;
        
        public ItemTooltipDescriptionsRecipesComponent(DescriptionComponentConfig config) : base(config) { }
        
        public override void ToggleSectionActive(bool active) {
            if (_hasContent) {
                SetParentSectionVisibility(active);
            }
        }

        protected override void Setup(IItemDescriptor descriptor, View view) {
            _hasContent = descriptor.Read != null && descriptor.Read.Recipes.Any();
            if (descriptor.Read != null) {
                PrepareDescription(descriptor.Read.Recipes, view);
            }
            
            SetParentSectionVisibility(_hasContent);
        }
        
        protected override void PrepareItemDescription(IRecipe item, ItemDescriptionElement descriptionElement, View view) {
            Item outcomeItem = descriptionElement.AddItemIcon(item.Outcome, view);
            ExistingItemDescriptor descriptor = new (outcomeItem);

            descriptionElement.Setup(ParentSection, descriptor.ItemDescription, ItemSlotUI.VisibilityConfig.OnlyIcon, outcomeItem.DisplayName);        
        }
    }
}