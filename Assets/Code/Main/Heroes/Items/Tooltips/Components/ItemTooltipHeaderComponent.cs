using System;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipHeaderComponent : IItemTooltipComponent {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text newGamePlusLabel;
        [SerializeField] GameObject flavorSection; 
        [SerializeField] TextMeshProUGUI flavorText;
        
        public View TargetView { get; set; }
        public ref PartialVisibility Visibility => ref _visibility;
        PartialVisibility _visibility;
        public bool UseReadMore { get; private set; }
        public bool UseReadMoreEnabled { get; set; }

        public void ToggleSectionActive(bool active) {
            flavorSection.TrySetActiveOptimized(active);
        }

        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare, View view) {
            Refresh(descriptor, descriptorToCompare);
        }

        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            SetupName(descriptor);
            SetupNewGamePlusLabel(descriptor.ExistingItem);
            SetupFlavor(descriptor);
        }

        void SetupName(IItemDescriptor descriptor) {
            nameText.text = descriptor.Name;
            nameText.color = descriptor.Quality != null ? descriptor.Quality.NameColor : ARColor.MainWhite;
        }
        
        void SetupNewGamePlusLabel(Item item) {
            if (newGamePlusLabel == null || item == null) return;
            
            var isNewGamePlus = item.Template.CanHaveItemLevel && item.NewGamePlusLevel > 0;
            var newGamePlusText = NewGamePlusUtils.NewGamePlusLevel(item.NewGamePlusLevel, true);
            newGamePlusLabel.SetActiveAndText(isNewGamePlus, newGamePlusText);
        }
        
        void SetupFlavor(IItemDescriptor descriptor) {
            if (flavorSection == null) return;
            
            string flavor = descriptor.ItemFlavor;
            bool hasFlavor = !flavor.IsNullOrWhitespace();
            UseReadMore = hasFlavor;
            Visibility.SetInternal(hasFlavor);
            flavorText.text = hasFlavor ? flavor : string.Empty;
        }
    }
}