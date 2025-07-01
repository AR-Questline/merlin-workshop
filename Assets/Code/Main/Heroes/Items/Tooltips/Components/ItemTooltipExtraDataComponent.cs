using System;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipExtraDataComponent : IItemTooltipComponent {
        [SerializeField] GameObject sectionParent;
        [SerializeField] TextMeshProUGUI stolenText;
        [SerializeField] RectTransform stolenBlend;

        public View TargetView { get; set; }
        public ref PartialVisibility Visibility => ref _visibility;
        PartialVisibility _visibility;
        public bool UseReadMore { get; private set; }

        public void ToggleSectionActive(bool active) {
            sectionParent.TrySetActiveOptimized(active);
        }

        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare, View view) {
            Refresh(descriptor, descriptorToCompare);
        }

        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            bool isStolen = descriptor.IsStolen;
            Visibility.SetInternal(isStolen);
            
            if (isStolen) {
                stolenText.text = descriptor.StolenText;
            }

            if (stolenBlend) {
                stolenBlend.gameObject.SetActive(isStolen);
            }

            if (sectionParent) {
                sectionParent.SetActive(isStolen);
            }
            
            UseReadMore = isStolen;
        }
    }
}