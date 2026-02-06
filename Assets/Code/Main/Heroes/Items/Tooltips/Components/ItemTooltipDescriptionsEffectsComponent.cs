using System;
using System.Linq;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipDescriptionsEffectsComponent : ItemTooltipDescriptionsBaseComponent<string> {
        public override void ToggleSectionActive(bool active) { 
            SetParentSectionVisibility(active);
        }

        protected override void Setup(IItemDescriptor descriptor, View view) {
            string description = descriptor.ItemDescription;
            bool hasContent = false;
            
            if (!string.IsNullOrWhiteSpace(description)) {
                PrepareDescription(description, view);
                hasContent = true;
            } else if (descriptor.Effects.Any()) {
                PrepareDescription(descriptor.Effects, view);
                hasContent = true;
            }
            
            Visibility.SetInternal(hasContent);
        }

        protected override void PrepareItemDescription(string item, ItemDescriptionElement descriptionElement, View view) {
            descriptionElement.Setup(ParentSection, item);
        }
    }
}