using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Choose {
    public interface IItemChooseUI : IItemsUIConfig, IModel { }

    public abstract partial class ItemChooseUI<TParent> : Element<TParent>, IItemChooseUI where TParent : IItemChooseParent {
        public sealed override bool IsNotSaved => true;

        protected ItemsUI _itemsUI;
        protected Prompt _promptSelect;

        public virtual Type ItemsListElementView => typeof(VItemsListElement);
        public virtual Type ItemsCategoryListHostView => typeof(VHostItemsListWithCategory);
        public Type ItemsUIView => typeof(VItemsEquipmentChooseUI);
        public Type ItemsListUIView => typeof(VItemsListEquipmentChooseUI);
        public virtual int MinRowsCount => 5;
        public virtual bool UseFilter => false;
        
        public IEnumerable<Item> Items => ParentModel.PossibleItems.Where(ItemFilter);
        public IEnumerable<ItemsTabType> Tabs { get; }

        protected Prompts Prompts => ParentModel.Prompts;
        Transform IItemsUIConfig.ItemsHost => View<VItemChooseUI>().ItemsHost;
        
        public virtual ItemsTabType SortingTab => null;
        public virtual bool UseCategoryList => false;
        public bool IsEmpty => !Items.Any();
        
        protected ItemChooseUI(IEnumerable<ItemsTabType> tabs) {
            Tabs = tabs;
        }

        protected override void OnInitialize() {
            World.SpawnView<VItemChooseUI>(this, true, true, ParentModel.ChooseHost);
            _itemsUI = AddElement(new ItemsUI(this));
            AddPrompts();
        }

        protected override void OnFullyInitialized() {
            var items = Element<ItemsUI>();
            items.ListenTo(ItemsUI.Events.HoveredItemsChanged, HoveredItemsChanged, this);
            items.ListenTo(ItemsUI.Events.ClickedItemsChanged, SelectCurrent, this);
        }
        
        protected abstract void HoveredItemsChanged(Item item);

        protected virtual void AddPrompts() {
            _promptSelect = Prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.UIItemsSelect.Translate()), this, false);
        }
        
        protected virtual void AfterChoose() { }

        protected virtual void SelectCurrent() {
            if (_itemsUI.HoveredItem.Locked) {
                return;
            }
            
            Choose(_itemsUI.HoveredItem);
            AfterChoose();
        }

        protected abstract void Choose(Item item);
        protected abstract bool ItemFilter(Item item);
    }
}