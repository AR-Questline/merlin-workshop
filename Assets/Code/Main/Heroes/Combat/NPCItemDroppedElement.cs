using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Combat {
    /// <summary>
    /// There are two ways to pick up an enemies weapon: Through the location this is attached to as well as the original item in the inventory.
    /// This component makes sure that you can only pick up the item once.
    /// </summary>
    public partial class NPCItemDroppedElement : Element<Location> {
        public override ushort TypeForSerialization => SavedModels.NPCItemDroppedElement;

        [Saved] Item _droppedItem;
        
        IEventListener _locListener;
        IEventListener _itemListener;

        public Item DroppedItem => _droppedItem;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public NPCItemDroppedElement() { }
        public NPCItemDroppedElement(Item droppedItem) {
            _droppedItem = droppedItem;
        }

        protected override void OnFullyInitialized() {
            if (DroppedItem == null || DroppedItem.HasBeenDiscarded) {
                ParentModel.Discard();
                return;
            }
            
            // Listen to pickable item location being picked up. If its picked up then remove item from inventory
            _locListener = ParentModel.ListenTo(Events.BeforeDiscarded, AfterItemPickedThroughLocation, this);
            
            // Listen to item being picked up from inventory. If its picked up then remove the pickable item location
            _itemListener = DroppedItem.ListenTo(IItemOwner.Relations.OwnedBy.Events.BeforeDetached, BeforeItemPickedThroughInventory, ParentModel);
        }

        void AfterItemPickedThroughLocation() {
            if (DroppedItem == null 
                || ParentModel.WasDiscardedFromDomainDrop) {
                return;
            }

            World.EventSystem.TryDisposeListener(ref _itemListener);
            DroppedItem.Discard();
        }

        void BeforeItemPickedThroughInventory() {
            if (DroppedItem == null 
                || DroppedItem.WasDiscardedFromDomainDrop) {
                return;
            }

            World.EventSystem.TryDisposeListener(ref _locListener);
            ParentModel?.Discard();
        }
    }
}