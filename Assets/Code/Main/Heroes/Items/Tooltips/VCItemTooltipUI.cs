using Awaken.TG.Main.Heroes.Items.Tooltips.Components;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public class VCItemTooltipUI : VCItemBaseTooltipUI {
        [Title("Tooltip Sections")]
        [SerializeField] ItemTooltipHeaderComponent header;
        [SerializeField] ItemTooltipBodyComponent body;

        [SerializeField] ItemTooltipDescriptionsEffectsComponent effects;
        [SerializeField] ItemTooltipDescriptionsRequirementsComponent requirements;
        [SerializeField] ItemTooltipDescriptionsRecipesComponent recipes;
        [SerializeField] ItemTooltipDescriptionsGemComponent gem;
        [SerializeField] ItemTooltipDescriptionsBuffComponent buff;
        
        [SerializeField] ItemTooltipFooterComponent footer;
        [SerializeField] ItemTooltipKeywordsComponent keywords;
        [SerializeField] ItemTooltipExtraDataComponent extraData;
        
        [Title("Others")]
        [SerializeField] TMP_Text equippedLabel;

        protected override IItemTooltipComponent[] AllSections => new IItemTooltipComponent[] { header, body, effects, requirements, recipes, gem, buff, keywords, footer, extraData };
        protected override IItemTooltipComponent[] ReadMoreSectionsToShow => new IItemTooltipComponent[] { header, keywords, requirements, buff, extraData };
        protected override IItemTooltipComponent[] ReadMoreSectionsToHide => new IItemTooltipComponent[] { body, effects, recipes, gem };
        
        protected override void OnMount() {
            if (equippedLabel) {
                equippedLabel.text = LocTerms.Equipped.Translate();
            }
        }

        public override void RefreshContent(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            base.RefreshContent(descriptor, descriptorToCompare);
            
            if (descriptor == null) {
                return;
            }
            
            header.Refresh(descriptor, descriptorToCompare);
            body.Refresh(descriptor, descriptorToCompare);
                
            effects.Refresh(descriptor, descriptorToCompare);
            requirements.Refresh( descriptor, descriptorToCompare);
            recipes.Refresh(descriptor, descriptorToCompare);
            gem.Refresh(descriptor, descriptorToCompare);
            buff.Refresh(descriptor, descriptorToCompare);
                
            keywords.Refresh(descriptor, descriptorToCompare);
            footer.Refresh(descriptor, descriptorToCompare);
            extraData.Refresh(descriptor, descriptorToCompare);
            this.Trigger(Events.ContentRefreshed, this);
        }
    }
}