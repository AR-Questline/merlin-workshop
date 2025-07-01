using System;
using Awaken.TG.Main.Heroes.Items.Tooltips.Components;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public class VCItemShortTooltipUI : VCItemBaseTooltipUI {
        [Title("Tooltip Sections")]
        [SerializeField] ItemTooltipHeaderComponent header;
        [SerializeField] ItemTooltipBodyComponent body;
        [SerializeField] ItemTooltipDescriptionsEffectsComponent effects;
        [SerializeField] ItemTooltipDescriptionsRequirementsComponent requirements;
        [SerializeField] ItemTooltipFooterComponent footer;
        [SerializeField] ItemTooltipExtraDataComponent extraData;
        
        protected override IItemTooltipComponent[] AllSections => new IItemTooltipComponent[] { header, body, effects, requirements, footer, extraData };
        protected override IItemTooltipComponent[] ReadMoreSectionsToShow => Array.Empty<IItemTooltipComponent>();
        protected override IItemTooltipComponent[] ReadMoreSectionsToHide => Array.Empty<IItemTooltipComponent>();

        public override void RefreshContent(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            base.RefreshContent(descriptor, descriptorToCompare);

            if (descriptor == null) {
                return;
            }
            
            header.Refresh(descriptor, descriptorToCompare);
            body.Refresh(descriptor, descriptorToCompare);
            effects.Refresh(descriptor, descriptorToCompare);
            requirements.Refresh(descriptor, descriptorToCompare);
            footer.Refresh(descriptor, descriptorToCompare);
            extraData.Refresh(descriptor, descriptorToCompare);
            this.Trigger(Events.ContentRefreshed, this);
        }
    }
}