using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Result {
    /// <summary>
    /// Model used to display information about an items that was crafted or a new recipe that was discovered (only for experimental cooking). Also, useful for displaying information about multiple items that was identified.
    /// Lifecycle: Created by AddElement for CraftingTabsUI and discarded when closed by the player
    /// </summary>
    [SpawnsView(typeof(VModalBlocker), isMainView = false)]
    [SpawnsView(typeof(VItemDiscoveredInfo))]
    public partial class ItemDiscoveredInfo : Element<IModel>, IPromptHost, IClosable {
        public sealed override bool IsNotSaved => true;

        public Transform PromptsHost { get; private set; }

        TempItemDescriptor _itemDescriptor;
        Prompt _confirmPrompt;
        readonly Item[] _items;
        ItemDiscoveredTooltipSystemUI[] _tooltips;

        public ItemDiscoveredInfo(Item[] items) {
            _items = items;
        }

        protected override void OnFullyInitialized() {
            _tooltips = new ItemDiscoveredTooltipSystemUI[_items.Length];
            for (int i = 0; i < _items.Length; i++) {
                _tooltips[i] = AddElement(new ItemDiscoveredTooltipSystemUI(typeof(VItemDiscoveredTooltipSystemUI), View<VItemDiscoveredInfo>().ItemInfoTooltipParent, 0, 0, 0.3f, true, false));
            }

            PromptsHost = _tooltips[^1].View<VItemDiscoveredTooltipSystemUI>().PromptsHost;
            _confirmPrompt = Prompt.Tap(KeyBindings.UI.Generic.Accept, LocTerms.Accept.Translate(), Close)
                .AddAudio()
                .SetupState(false, false);
            
            var prompts = AddElement(new Prompts(this));
            prompts.AddPrompt(_confirmPrompt, this);
            
            this.ListenTo(VModalBlocker.Events.ModalDismissed, Close, this);
        }

        public void ShowItemIdentifiedInfo(string info) {
            PrepareTooltips();
            ShowItemInfo(info);
        }
        
        public void ShowItemCreatedInfo(string itemName) {
            PrepareTooltips();
            ShowItemInfo($"{LocTerms.ItemCrafted.Translate(itemName)}!");
        }
        
        public void ShowNewRecipeDiscoveredInfo(string recipeName) {
            PrepareTooltips();
            ShowItemInfo($"{LocTerms.NewRecipeDiscovered.Translate(recipeName)}!");
        }

        void PrepareTooltips() {
            for (int index = 0; index < _items.Length; index++) {
                Item item = _items[index];
                ItemDiscoveredTooltipSystemUI tooltip = _tooltips[index];
                _itemDescriptor = new TempItemDescriptor(item.Template, this, item.Quantity, item.Level.ModifiedInt, item.WeightLevel.ModifiedInt);
                tooltip.SetDescriptor(_itemDescriptor);
                tooltip.View<VItemDiscoveredTooltipSystemUI>().RefreshTooltipContent(_itemDescriptor);
            }
        }

        void ShowItemInfo(string info) {
            _confirmPrompt.SetupState(true, true);
            View<VItemDiscoveredInfo>().Show(info, _tooltips);
        }

        public void Close() {
            View<VItemDiscoveredInfo>().Hide();
            _itemDescriptor.Dispose();
            Discard();
        }
    }
}