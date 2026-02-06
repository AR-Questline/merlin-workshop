using System;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipBodyComponent : IItemTooltipComponent {
        [Title("Item Slot")]
        [SerializeField] GameObject itemSlotSection;
        [SerializeField] ItemSlotUI itemSlot;
        [Title("Item Type")]
        [SerializeField] TextMeshProUGUI itemTypeText;
        [Title("Item Stats")]
        [SerializeField] GameObject sharedStatsAndDescriptionSection;
        [SerializeField] GameObject statsAndDescriptionSeparator;
        [SerializeField] ItemTooltipStatsComponent stats;
        [SerializeField] ItemTooltipMagicStatsComponent magicStats;
        
        public View TargetView { get; set; }
        public ref PartialVisibility Visibility => ref _visibility;
        PartialVisibility _visibility;
        public bool UseReadMore { get; private set; }
        public bool UseReadMoreEnabled { get; set; }
        
        public void ToggleSectionActive(bool active) {
            sharedStatsAndDescriptionSection.TrySetActiveOptimized(active);
        }
        
        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            SetupIcon(descriptor, TargetView);
            SetupItemType(descriptor);
            
            if (stats != null && magicStats != null) {
                SetupStats(descriptor, descriptorToCompare);
            }
        }
        
        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare, View view) {
            SetupIcon(descriptor, TargetView ? TargetView : view);
            SetupItemType(descriptor);
            
            if (stats != null && magicStats != null) {
                SetupStats(descriptor, descriptorToCompare);
            }        
        }

        void SetupIcon(IItemDescriptor descriptor, View view) {
            if (itemSlot == null) return;
            
            itemSlot.SetVisibilityConfig(ItemSlotUI.VisibilityConfig.Tooltip);
            
#pragma warning disable CS0612 // Type or member is obsolete
            itemSlot.Setup(descriptor.ExistingItem, view);
#pragma warning restore CS0612 // Type or member is obsolete
        }
        
        void SetupItemType(IItemDescriptor descriptor) { 
            if (itemTypeText) {
                itemTypeText.text = descriptor.ItemType;
            }
        }
        
        void SetupStats(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            descriptor.TypeSpecificDescriptor?.SetupStatTexts(stats, magicStats, descriptorToCompare);
            
            bool hasStats = HasSpecificDescriptor(descriptor.TypeSpecificDescriptor);
            bool hasDescription = HasItemDescription(descriptor) || HasReadDescription(descriptor);
            bool hasSharedSection = sharedStatsAndDescriptionSection != null 
                                    && hasStats || hasDescription;
            
            if (statsAndDescriptionSeparator != null) {
                statsAndDescriptionSeparator.SetActiveOptimized((hasStats && hasDescription && !descriptor.ExistingItem.IsSoulCube));
            }
            
            Visibility.SetInternal(hasSharedSection);
        }
        
        bool HasSpecificDescriptor(IItemTypeSpecificDescriptor itemTypeSpecificDescriptor) => itemTypeSpecificDescriptor is not (null or IItemTypeSpecificDescriptor.GenericDescriptor);
        bool HasItemDescription(IItemDescriptor descriptor) => !string.IsNullOrWhiteSpace(descriptor.ItemDescription) || descriptor.Effects.Any();
        bool HasReadDescription(IItemDescriptor descriptor) => descriptor.Read != null && descriptor.Read.Recipes.Any();
    }
}