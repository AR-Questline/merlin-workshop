using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public partial class ItemTracker : BaseSimpleTracker<ItemTrackerAttachment> {
        public override ushort TypeForSerialization => SavedModels.ItemTracker;

        IEnumerable<ItemTemplate> _itemTemplates;

        public override void InitFromAttachment(ItemTrackerAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            _itemTemplates = spec.ItemTemplates;
        }
        
        protected override void OnInitialize() { 
            InitListeners();
            ParentModel.ParentModel.AfterFullyInitialized(InitItems);
        }

        protected override void OnRestore() {
            InitListeners();
        }

        void InitListeners() {
            Hero.Current.ListenTo(IItemOwner.Relations.Owns.Events.AfterEstablished, OnRelationChange, this);
            Hero.Current.ListenTo(IItemOwner.Relations.Owns.Events.Changed, OnRelationChange, this);
            Hero.Current.ListenTo(IItemOwner.Relations.Owns.Events.AfterDisestablished, OnRelationChange, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, Item.Events.QuantityChanged, this, OnQuantityChange);
        }
        
        void InitItems() {
            Refresh();
        }

        protected override string ConstructDesc() {
            var desc = base.ConstructDesc();
            var itemKeyword = "{item}";
            var index = desc.IndexOf(itemKeyword, System.StringComparison.Ordinal);
            if (index == -1) {
                return desc.FontBold();
            }
            
            int endIndex = index + itemKeyword.Length;
            // highlight the counter after the item name
            desc = desc[..endIndex] + desc[(endIndex)..].FontBold();
            return desc.Replace(itemKeyword, string.Join(", ", _itemTemplates.Select(i => i.itemName.ToString())));
        }

        void OnRelationChange(RelationEventData data) {
            Item item = (Item)data.to;
            RefreshFor(item);
        }

        void OnQuantityChange(QuantityChangedData data) {
            if (data.target.Owner == Hero.Current) {
                RefreshFor(data.target);
            }
        }

        void RefreshFor(Item item) {
            if (_itemTemplates.Any(t => item.Template == t)) {
                Refresh();
            }
        }
        
        void Refresh() {
            SetTo(GetCurrentItemQuantity());
        }
        
        int GetCurrentItemQuantity() {
            return Hero.Current.HeroItems.Items
                .Where(i => _itemTemplates.Any(t => i.Template == t))
                .Sum(static i => i.Quantity);
        }
    }
}