using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Tooltips.Base;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Sources;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public partial class ItemTooltipUI : FloatingTooltipUI, IUIAware {
        public IItemDescriptor Descriptor { get; private set; }
        public IItemDescriptor DescriptorToCompare => ConstantDescriptorToCompare ?? GetOptimalComparisonDescriptor();
        public IItemDescriptor ConstantDescriptorToCompare { get; set; }
        static ICharacterInventory HeroInventory => Hero.Current.Inventory;
        
        readonly bool _comparerActive;

        public new static class Events {
            public static readonly Event<ItemTooltipUI, Change<IItemDescriptor>> ItemDescriptorChanged = new(nameof(ItemDescriptorChanged));
        }
        
        public ItemTooltipUI(Type viewType, Transform host, float appearDelay = -1f, float hideDelay = -1, float alphaTweenTime = 0.25f, bool isStatic = false, bool preventDisappearing = false, bool comparerActive = true) : base(viewType, host, appearDelay, hideDelay, alphaTweenTime, isStatic, preventDisappearing) {
            _comparerActive = comparerActive;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            SetComparerActive(_comparerActive);
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, this));
        }

        public void SetDescriptor(IItemDescriptor descriptor) {
            if (Descriptor == descriptor) return;
            var previousDescriptor = Descriptor;
            Descriptor = descriptor;
            this.Trigger(Events.ItemDescriptorChanged, new Change<IItemDescriptor>(previousDescriptor, Descriptor));
        }

        public void ResetDescriptor(IItemDescriptor descriptor) {
            if (Descriptor != descriptor) return;
            var previousDescriptor = Descriptor;
            Descriptor = null;
            this.Trigger(Events.ItemDescriptorChanged, new Change<IItemDescriptor>(previousDescriptor, Descriptor));
        }

        void SetComparerActive(bool active, bool instant = false) {
            if (active) {
                View<IViewCompareTooltipSystem>()?.ComparerAppear(instant);
            } else {
                View<IViewCompareTooltipSystem>()?.ComparerDisappear(instant);
            }
        }
        
        IItemDescriptor GetOptimalComparisonDescriptor() {
            var equipmentType = Descriptor.EquipmentType;
            if (equipmentType == null) return null;
            
            var currentItem = Descriptor.ExistingItem;
            var currentSlot = HeroInventory.SlotWith(currentItem);
            var eqChoosePanelSlot = World.Any<EquipmentChooseUI>()?.EquipmentSlotType;
            if (Descriptor.IsEquipped && currentSlot == eqChoosePanelSlot) return null;
            
            var equippedItems = Hero.Current.HeroItems.EquippedItems();
            var possibleItems = new StructList<Item>(equippedItems.Count());
            Item lowestLevelItem = null;
            int minLevel = int.MaxValue;
            
            foreach (var otherEquippedItem in Hero.Current.HeroItems.EquippedItems()) {
                // skip items the same as the one being compared
                // skip items that are hidden on UI (e.g., hero fists)
                if (otherEquippedItem == currentItem || otherEquippedItem.Template.hiddenOnUI) {
                    continue;
                }
                
                var otherEquippedItemElement = otherEquippedItem.Element<ItemEquip>();
                var otherEquippedItemType = otherEquippedItemElement.EquipmentType;
                // skip items that are not of the same equipment type category
                if (otherEquippedItemType.Category != equipmentType.Category) {
                    continue;
                }
                
                bool isNotWeaponOrAmmo = otherEquippedItemType.Category is not (EquipmentCategory.Weapon or EquipmentCategory.Ammo);
                // skip items that are not of the same category or type as the one being compared (since only weapons can have multiple slot types)
                if (isNotWeaponOrAmmo && otherEquippedItemType != equipmentType) {
                    continue;
                }
                
                bool inventoryOpen = World.Any<LoadoutsUI>() != null;
                // skip comparing consumables outside of inventory
                if (!inventoryOpen && equipmentType.Category == EquipmentCategory.QuickUse) {
                    continue;
                }
                
                // skip items that are equipped in different slots than the one being actively compared
                if (isNotWeaponOrAmmo && inventoryOpen && HeroInventory.SlotWith(otherEquippedItem) != eqChoosePanelSlot) {
                    continue;
                }
                
                int itemLevel = otherEquippedItem.Level.ModifiedInt;
                if (itemLevel < minLevel) {
                    minLevel = itemLevel;
                    lowestLevelItem = otherEquippedItem;
                } else {
                    possibleItems.Add(otherEquippedItem);
                }
            }

            if (lowestLevelItem != null) {
                possibleItems.Add(lowestLevelItem);
            }
            
            return possibleItems.Count switch {
                0 => null,
                1 => new ExistingItemDescriptor(possibleItems[0]),
                _ => new ExistingItemDescriptor(FindBestCandidate(possibleItems, currentSlot, eqChoosePanelSlot))
            };
        }
        
        Item FindBestCandidate(StructList<Item> candidates,EquipmentSlotType currentSlot, EquipmentSlotType eqChoosePanelSlot) {
            Item bestItem = null;
            int bestScore = int.MinValue;
            var currentType = Descriptor.EquipmentType;
    
            for (int i = 0; i < candidates.Count; i++) {
                var toCompare = candidates[i];
                int score = 0;
        
                // Slot priority in EquipmentChooseUI
                if (HeroInventory.SlotWith(toCompare) == eqChoosePanelSlot) {
                    score += 100;
                }
        
                // Type matching
                var toCompareType = toCompare.EquipmentType;
                if (toCompareType == currentType) {
                    score += 30;
                }
        
                // Loadout priority
                if ((toCompareType.Category == EquipmentCategory.Weapon) && toCompare.IsPrimaryInLoadout()) {
                    score += 20;
                }
        
                // Current slot type bonus
                if (HeroInventory.SlotWith(toCompare) == currentSlot) {
                    score += 10;
                }

                bool weaponSlotMatch = toCompareType.Category == EquipmentCategory.Weapon && toCompareType.MainSlotType == currentType.MainSlotType;
                bool generalSlotMatch = toCompareType.Category != EquipmentCategory.Weapon && toCompareType == currentType;
                // Generic slot type match
                if (weaponSlotMatch || generalSlotMatch) {
                    score += 1;
                }

                if (score > bestScore) {
                    bestScore = score;
                    bestItem = toCompare;
                }
            }
    
            return bestItem;
        }
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UIKeyDownAction action) {
                if (action.Name == KeyBindings.UI.Generic.ReadMore) {
                    foreach (var view in Views) {
                        if (view is VCItemBaseTooltipUI itemBaseTooltip) {
                            itemBaseTooltip.ToggleReadMore();
                        }
                    }
                }
            }
            
            return UIResult.Ignore;
        }
    }
}