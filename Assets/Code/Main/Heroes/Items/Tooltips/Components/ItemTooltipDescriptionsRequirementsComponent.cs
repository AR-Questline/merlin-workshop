using System;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipDescriptionsRequirementsComponent : ItemTooltipDescriptionsBaseComponent<string> {
        [SerializeField] GameObject requirementsInfoSection;
        [SerializeField] TMP_Text requirementsInfo;
        
        bool _hasInfo;
        bool _meetsRequirements;
        
        public override void ToggleSectionActive(bool active) {
            var effectiveActive = UseReadMoreEnabled
                ? _meetsRequirements ? active : !active 
                : !_meetsRequirements;
            SetParentSectionVisibility(effectiveActive);
            requirementsInfoSection.SetActiveOptimized(_hasInfo && effectiveActive);
        }
        
        protected override void Setup(IItemDescriptor descriptor, View view) {
            string requirements = descriptor.ItemRequirements;
            bool hasContent = false;
            _hasInfo = false;
            _meetsRequirements = descriptor.RequirementsMet;
            
            if (!string.IsNullOrEmpty(requirements)) {
                Visibility.SetInternal(false);
                if (!_meetsRequirements && descriptor.HasSkills && !descriptor.IsMagic) {
                    string info = LocTerms.CannotUseSkillsOfItem.Translate().ColoredText(ARColor.MainRed).Italic();
                    requirementsInfo.SetText(info);
                    _hasInfo = true;
                }
                
                PrepareDescription(requirements, view);
                hasContent = true;
            }
            
            UseReadMore = hasContent && _meetsRequirements;
            Visibility.SetInternal(hasContent);
        }

        protected override void PrepareItemDescription(string item, ItemDescriptionElement descriptionElement, View view) {
            descriptionElement.Setup(ParentSection, item);
        }
    }
}