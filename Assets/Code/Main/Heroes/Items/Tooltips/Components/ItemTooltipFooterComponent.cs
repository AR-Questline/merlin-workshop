using System;
using System.Globalization;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipFooterComponent : IItemTooltipComponent {
        [SerializeField] GameObject sectionParent;
        [SerializeField] TextMeshProUGUI priceText;
        [SerializeField] TextMeshProUGUI weightText;

        public View TargetView { get; set; }
        public ref PartialVisibility Visibility => ref _visibility;
        PartialVisibility _visibility;
        public bool UseReadMore { get; private set; }

        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare, View view) {
            Refresh(descriptor, descriptorToCompare);
        }

        public void ToggleSectionActive(bool active) {
            sectionParent.TrySetActiveOptimized(active);
        }
        
        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            SetupCounters(descriptor);
        }
        
        void SetupCounters(IItemDescriptor descriptor) {
            priceText.text = descriptor.Price.ToString();

            var weight = descriptor.Weight;
            string weightFormat = weight switch {
                < 0.01f => "F0",
                < 0.1f => "F2",
                _ => "F1"
            };
            
            weightText.text = descriptor.Weight.ToString(weightFormat, CultureInfo.CurrentCulture);
        }
    }
}