using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    public class VCQuickLoadout : VCQuickUseOption {
        [SerializeField] Transform middlePoint;
        [SerializeField] ItemSlotUI primarySlot;
        [SerializeField] ItemSlotUI secondarySlot;
        [SerializeField] RectTransform primarySlotRectTransform;
        [SerializeField] RectTransform secondarySlotRectTransform;
        [Space(10f)] 
        [SerializeField] int loadoutIndex;
        [SerializeField] GameObject primarySelectedIndicator;
        [SerializeField] GameObject secondarySelectedIndicator;

        public int LoadoutIndex => loadoutIndex;

        Item PrimaryItem => Target.HeroItems.LoadoutAt(loadoutIndex).PrimaryItem?.IsFists ?? true
            ? null
            : Target.HeroItems.LoadoutAt(loadoutIndex).PrimaryItem;

        Item SecondaryItem => Target.HeroItems.LoadoutAt(loadoutIndex).SecondaryItem?.IsFists ?? true
            ? null
            : Target.HeroItems.LoadoutAt(loadoutIndex).SecondaryItem;
        bool IsSelected => Target.HeroItems.CurrentLoadout == Target.HeroItems.LoadoutAt(loadoutIndex);

        protected override void OnAttach() {
            base.OnAttach();
            var visibility = ItemSlotUI.VisibilityConfig.QuickWheel;

            primarySlot.SetVisibilityConfig(visibility);
            secondarySlot.SetVisibilityConfig(visibility);
            Refresh();
        }

        public void Refresh() {
            int marker = 0;
            try {
                if (!IsEmptySlot(PrimaryItem)) {
                    marker = 1;
                    primarySlot.gameObject.SetActive(true);
                    marker = 2;
                    primarySlot.Setup(PrimaryItem, ParentView);
                    marker = 3;
                    primarySelectedIndicator.SetActive(IsSelected);
                    
                    marker = 4;
                    if (IsEmptySlot(SecondaryItem) || SecondaryItem.IsTwoHanded) {
                        marker = 5;
                        primarySlotRectTransform.transform.position = middlePoint.position;
                    }
                } else {
                    marker = 6;
                    primarySlotRectTransform.transform.position = middlePoint.position;
                    marker = 7;
                    primarySelectedIndicator.SetActive(false);
                }
                
                marker = 8;
                bool shouldShowSecondSlot = !IsEmptySlot(SecondaryItem) && (IsEmptySlot(PrimaryItem) || !PrimaryItem.IsTwoHanded || PrimaryItem.EquipmentType == EquipmentType.Bow);
                marker = 9;
                secondarySlot.gameObject.SetActive(shouldShowSecondSlot);
                marker = 10;
                secondarySelectedIndicator.SetActive(IsSelected && shouldShowSecondSlot);
                
                marker = 11;
                if (shouldShowSecondSlot) {
                    marker = 12;
                    secondarySlot.Setup(SecondaryItem, ParentView);
                    
                    marker = 13;
                    if (IsEmptySlot(PrimaryItem)) {
                        marker = 14;
                        primarySlotRectTransform.transform.position = middlePoint.position;
                    }
                }
            } catch {
                Log.Critical?.Error($"Error in VCQuickLoadout.Refresh at marker {marker}");
                throw;
            }
        }

        protected override void NotifyHover() {
            secondarySlot.NotifyHover();
            primarySlot.NotifyHover();
        }

        protected override void OnShow() {
            if (VQuickUseWheel == null) {
                Log.Critical?.Error("VQuickUseWheel is null in VCQuickLoadout.OnShow");
            }
            SearchForException();
            VQuickUseWheel.QuickItemTooltipUIPrimary.ShowItem(PrimaryItem);
            VQuickUseWheel.QuickItemTooltipUISecondary.ShowItem(SecondaryItem == PrimaryItem ? null : SecondaryItem);
        }

        protected override void OnHide() {
            if (VQuickUseWheel == null) {
                Log.Critical?.Error("VQuickUseWheel is null in VCQuickLoadout.OnShow");
            }
            SearchForException();
            VQuickUseWheel.HideItemTooltips();
        }

        public override void OnSelect(bool _) {
            if (VQuickUseWheel == null) {
                Log.Critical?.Error("VQuickUseWheel is null in VCQuickLoadout.OnShow");
            }
            SearchForException();
            Target.HeroItems.ActivateLoadout(loadoutIndex);
            RadialMenu.Close();
        }

        public override OptionDescription Description => new(true, LocTerms.UIItemsEquip.Translate());

        bool IsEmptySlot(Item itemSlot) {
            return string.IsNullOrEmpty(itemSlot?.DisplayName);
        }

        void SearchForException() {
            try {
                var target = Target;
            } catch {
                Log.Critical?.Error("Exception on Target");
                throw;
            }

            try {
                var heroItems = Target.HeroItems;
            } catch {
                Log.Critical?.Error("Exception on Target.HeroItems");
                throw;
            }

            try {
                var heroItems = Target.HeroItems.Loadouts;
            } catch {
                Log.Critical?.Error("Exception on Target.HeroItems.Loadouts");
                throw;
            }

            try {
                var heroItems = Target.HeroItems.LoadoutAt(loadoutIndex);
            } catch {
                Log.Critical?.Error("Exception on Target.HeroItems.Loadouts[loadoutsIndex]");
                throw;
            }
            
            try {
                var item = PrimaryItem;
            } catch {
                Log.Critical?.Error("Exception on PrimaryItem");
                throw;
            }

            try {
                var item = SecondaryItem;
            } catch {
                Log.Critical?.Error("Exception on SecondaryItem");
                throw;
            }
        }
    }
}