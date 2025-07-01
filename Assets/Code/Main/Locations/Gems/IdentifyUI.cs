using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.Result;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Locations.Gems {
    public partial class IdentifyUI : GemsBaseUI<VIdentifyUI> {
        protected override int ServiceBaseCost => 0;
        protected override string GemActionName => LocTerms.RelicsPromptIdentify.Translate();
        protected override Func<Item, bool> ItemFilter => item => item.HasElement<UnidentifiedItem>();
        public override string ContextTitle => LocTerms.IdentifyTab.Translate();
        public override Type ItemsListElementView => typeof(VItemGemChooseElement);
        public override IEnumerable<ItemsTabType> Tabs => ItemsTabType.Identify;
        public override bool UseCategoryList => false;
        
        Item _previousClickedItem;
        VIdentifyUI CurrentView => View<VIdentifyUI>();

        protected override bool CanRunAction(Item _) {
            return ClickedItem is { HasBeenDiscarded: false } && CheckIngredients();
        }

        protected override void OnGamepadSlotSelect(Item item) { }
        protected override void SpawnItemTooltip() { }
        protected override void SpawnIngredientTooltip() { }
        protected override void RefreshItemTooltip() { }

        protected override void GemAction() {
            if (!ClickedItem) {
                return;
            }
            
            ClickedItem.TryGetElement(out UnidentifiedItem unidentifiedItem);
            if (unidentifiedItem == null) {
                Log.Important?.Error("Item is not unidentified! This should not happen!");
                return;
            }
            
            PayForService();
            
            var identifiedItems = unidentifiedItem.Identify().ToArray();
            foreach (var item in identifiedItems) {
                World.Add(item);
            }
            
            AddElement(new ItemDiscoveredInfo(identifiedItems)).ShowItemIdentifiedInfo(LocTerms.NewItems.Translate());

            Refresh();
        }

        protected override void OnItemClicked(Item item) {
            if (HasBeenDiscarded) return;
            
            if (item) {
                if (item != _previousClickedItem) {
                    CurrentView.ResetOutcomeSection(item);
                }
                
                CobwebServiceBaseCost = item.Element<UnidentifiedItem>().CostOfIdentification;
                _previousClickedItem = item;
            }
            
            base.OnItemClicked(item);
        }
    }
}