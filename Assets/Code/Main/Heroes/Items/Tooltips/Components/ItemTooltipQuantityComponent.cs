using System;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipQuantityComponent : IItemTooltipComponent {
        [SerializeField] GameObject panel;
        [SerializeField] TextMeshProUGUI text;
        
        public View TargetView { get; set; }
        public ref PartialVisibility Visibility => ref _visibility;
        PartialVisibility _visibility;
        public bool UseReadMore { get; private set; }

        public void ToggleSectionActive(bool active) {
            panel.TrySetActiveOptimized(active);
        }

        public void SetColor(Color color) {
            text.color = color;
        }

        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare, View view) {
            Refresh(descriptor, descriptorToCompare);
        }

        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            text.text = $"({descriptor.Quantity})";
            panel.SetActive(descriptor.Quantity > 1);
        }
    }
}