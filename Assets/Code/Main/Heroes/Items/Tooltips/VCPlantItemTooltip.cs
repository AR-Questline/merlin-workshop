using System;
using Awaken.TG.Main.Heroes.Items.Tooltips.Components;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public class VCPlantItemTooltip : VCItemBaseTooltipUI {
        [Title("Tooltip Sections")]
        [SerializeField] ItemTooltipHeaderComponent header;
        [SerializeField] ItemTooltipBodyComponent body;
        [SerializeField] PlantItemInfoComponent plantInfo;
        
        protected override IItemTooltipComponent[] AllSections => new IItemTooltipComponent[] { header, body, plantInfo };
        protected override IItemTooltipComponent[] ReadMoreSectionsToShow => Array.Empty<IItemTooltipComponent>();
        protected override IItemTooltipComponent[] ReadMoreSectionsToHide => Array.Empty<IItemTooltipComponent>();

        public override void RefreshContent(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            base.RefreshContent(descriptor, descriptorToCompare);

            if (descriptor == null) {
                return;
            }
            
            header.Refresh(descriptor, descriptorToCompare);
            body.Refresh(descriptor, descriptorToCompare);
            plantInfo.Refresh(descriptor, descriptorToCompare);
            this.Trigger(Events.ContentRefreshed, this);
        }
    }
}