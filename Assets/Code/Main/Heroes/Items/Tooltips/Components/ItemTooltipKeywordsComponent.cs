using System;
using System.Linq;
using System.Text;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipKeywordsComponent : IItemTooltipComponent {
        [SerializeField, CanBeNull] GameObject keywordsSection;
        [SerializeField] TextMeshProUGUI keywordsText;

        public View TargetView { get; set; }
        public ref PartialVisibility Visibility => ref _visibility;
        PartialVisibility _visibility;
        public bool UseReadMore { get; private set; }

        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            SetupKeywords(descriptor);
        }

        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare, View view) {
            Refresh(descriptor, descriptorToCompare);
        }

        public void ToggleSectionActive(bool active) {
            keywordsSection.TrySetActiveOptimized(active);
        }

        void SetupKeywords(IItemDescriptor descriptor) {
            if(keywordsSection == null) return;
            
            var tooltipConstructor = descriptor?.KeywordsTooltip;
            bool hasKeywords = tooltipConstructor != null;
            UseReadMore = hasKeywords;
            _visibility.SetInternal(hasKeywords);
            
            if (hasKeywords) {
                var contentToDisplay = tooltipConstructor.ElementsToSpawn;
                var keywords = new StringBuilder();
                
                foreach (string entry in contentToDisplay.Select(tooltipValue => tooltipValue.Value as string)) {
                    keywords.AppendLine(entry);
                }
                
                keywordsText.SetText(keywords.ToString());
            } 
        }
    }
}