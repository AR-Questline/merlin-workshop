using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    public partial class ItemsListElementUI : Element<ItemsListUI>, IWithRecyclableView {
        public sealed override bool IsNotSaved => true;

        public Item Item { get; }
        public int Index { get; private set; }

        public ItemsListUI ItemsListUI => ParentModel;
        public ItemsUI ItemsUI => ItemsListUI.ParentModel;
        public ItemDescriptorType ItemDescriptorType => ItemsUI.ItemDescriptorType;
        public Transform ViewHost { get; private set; }
        public IItemsUIConfig Config => ParentModel.ParentModel.Config;
        
        public Func<Component> NextFocusTarget { get; set; }

        public ItemsListElementUI(Item item, int index, Transform viewHost) {
            Item = item;
            Index = index;
            ViewHost = viewHost;
        }

        protected override void OnFullyInitialized() {
            Item.ListenTo(Events.AfterDiscarded, Discard, this);
            World.SpawnView(this, Config.ItemsListElementView, true, true, ViewHost);
        }

        public void RefreshIndex(int index) {
            Index = index;
            TriggerChange();
        }

        public UIResult HandleEvent(UIEvent evt) => ParentModel.HandleEventFor(Item, evt);

        public void OnSelected() => ParentModel.OnSelected(Item);
        public void OnDeselected() => ParentModel.OnDeselected(Item);
        public void OnHoverStarted() => ParentModel.OnHoverStarted(Item);
        public void OnHoverEnded() => ParentModel.OnHoverEnded(Item);
    }
}