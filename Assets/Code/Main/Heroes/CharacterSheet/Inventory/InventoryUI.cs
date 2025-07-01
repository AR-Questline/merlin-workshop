using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.MVC;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Inventory {
    public partial class InventoryUI : CharacterSheetTab<VInventoryUI>, InventorySubTabs.ITabParent<VInventoryUI>, ICharacterSheetTabWithSubTabs {
        static InventorySubTabType s_lastTab;
        public InventorySubTabType CurrentType { get; set; } = InventorySubTabType.Equipment;
        public Tabs<InventoryUI, VInventoryTabs, InventorySubTabType, IInventorySubTab> TabsController { get; set; }
        public HeroItems HeroItems => ParentModel.Hero.HeroItems;
        public IEnumerable<Item> Items => HeroItems.Items.Where(item => !item.HiddenOnUI);
        public IEnumerable<Item> ItemsForLoadout => HeroItems.Items.Where(i => i.VisibleOnUIForLoadout);

        protected override void AfterViewSpawned(VInventoryUI view) {
            AddElement(new InventorySubTabs());
        }
        
        public void DropCurrentItem(Item item, ItemsUI itemsUI = null, bool forcePopupWhenNotSingle = false) {
            if (item is { Locked: false }) {
                if (item.Quantity > GameConstants.Get.DropPopupThreshold || (item.Quantity > 1 && forcePopupWhenNotSingle)) {
                    ShowDropPopup(item, itemsUI);
                    return;
                }
                
                HeroItems.Drop(item, 1);
                itemsUI?.Trigger(ItemsUI.Events.ItemsCollectionChanged, Items);
            }
        }
        
        void ShowDropPopup(Item item, ItemsUI itemsUI = null) {
            var inputItemQuantityUI = new InputItemQuantityUI(item);
            AddElement(inputItemQuantityUI);
            var popupContent = new DynamicContent(inputItemQuantityUI, typeof(VInputItemQuantityUI));
            
            PopupUI popup = null;
            popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                string.Empty,
                PopupUI.AcceptTapPrompt(() => {
                    HeroItems.Drop(item, inputItemQuantityUI.Value);
                    ClosePopup();
                }),
                PopupUI.CancelTapPrompt(ClosePopup),
                LocTerms.Quantity.Translate(),
                popupContent
            );

            void ClosePopup() {
                itemsUI?.Trigger(ItemsUI.Events.ItemsCollectionChanged, Items);
                inputItemQuantityUI.Discard();
                popup?.Discard();
            }
        }
        
        public bool TryToggleSubTab(CharacterSheetUI ui) {
            ui.Element<InventoryUI>().TabsController.SelectTab(s_lastTab ?? InventorySubTabType.Equipment);
            return true;
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            s_lastTab = CurrentType;
        }
    }
}
