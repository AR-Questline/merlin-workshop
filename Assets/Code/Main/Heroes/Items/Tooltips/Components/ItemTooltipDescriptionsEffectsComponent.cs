using System;
using System.Linq;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipDescriptionsEffectsComponent : ItemTooltipDescriptionsBaseComponent<string> {
        bool _hasContent;
        
        public ItemTooltipDescriptionsEffectsComponent(DescriptionComponentConfig config) : base(config) { }
        
        public override void ToggleSectionActive(bool active) {
            if (_hasContent) {
                SetParentSectionVisibility(active);
            }
        }

        protected override void Setup(IItemDescriptor descriptor, View view) {
            string description = descriptor.ItemDescription;
            _hasContent = false;
            
            if (!string.IsNullOrWhiteSpace(description)) {
                PrepareDescription(description, view);
                _hasContent = true;
            } else if (descriptor.Effects.Any()) {
                PrepareDescription(descriptor.Effects, view);
                _hasContent = true;
            }
            
            SetParentSectionVisibility(_hasContent);
        }

        protected override void PrepareItemDescription(string item, ItemDescriptionElement descriptionElement, View view) {
            descriptionElement.Setup(ParentSection, item);
        }
    }
}