using System;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipDescriptionsRequirementsComponent : ItemTooltipDescriptionsBaseComponent<string> {
        bool _hasContent;
        bool _meetsRequirements;

        public ItemTooltipDescriptionsRequirementsComponent(DescriptionComponentConfig config) : base(config) { }
        
        public override void ToggleSectionActive(bool active) {
            if (_hasContent) {
                if (!_meetsRequirements) {
                    SetParentSectionVisibility(!active);
                } else if (_meetsRequirements) {
                    SetParentSectionVisibility(active);
                }
            }
        }
        
        protected override void Setup(IItemDescriptor descriptor, View view) {
            string requirements = descriptor.ItemRequirements;
            _hasContent = false;
            _meetsRequirements = descriptor.RequirementsMet;
            
            if (!string.IsNullOrEmpty(requirements)) {
                if (!_meetsRequirements && descriptor.HasSkills && !descriptor.IsMagic) {
                    string info = LocTerms.CannotUseSkillsOfItem.Translate().ColoredText(ARColor.MainRed).Italic();
                    requirements += info;
                }
                
                PrepareDescription(requirements, view);
                _hasContent = true;
            }
            
            UseReadMore = _hasContent && _meetsRequirements;
            SetParentSectionVisibility(_hasContent && !_meetsRequirements);
        }

        protected override void PrepareItemDescription(string item, ItemDescriptionElement descriptionElement, View view) {
            descriptionElement.Setup(ParentSection, item);
        }
    }
}