using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {

    [UnitCategory("AR/General/Events/Items")]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnPossessed : GraphEvent<IModel, RelationEventData, Item> {
        protected override Item Payload(RelationEventData eventValue) => eventValue.to as Item;
        protected override Event<IModel, RelationEventData> Event => IItemOwner.Relations.Owns.Events.AfterAttached;
        protected override IModel Source(IListenerContext context) => context.Character;
    }

    [UnitCategory("AR/General/Events/Items")]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnStopPossessed : GraphEvent<IModel, RelationEventData, Item> {
        protected override Item Payload(RelationEventData eventValue) => eventValue.to as Item;
        protected override Event<IModel, RelationEventData> Event => IItemOwner.Relations.Owns.Events.AfterDetached;
        protected override IModel Source(IListenerContext context) => context.Character;
    }
    
    [UnitCategory("AR/General/Events/Items")]
    [UnityEngine.Scripting.Preserve]
    public class EvtBeforePickedItem : GraphEvent<ICharacterInventory, Item> {
        protected override Event<ICharacterInventory, Item> Event => ICharacterInventory.Events.BeforePickedUpItem;
        protected override ICharacterInventory Source(IListenerContext context) => context.Character.Inventory;
    }
    
    [UnitCategory("AR/General/Events/Items")]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnPickedItem : GraphEvent<ICharacterInventory, Item> {
        protected override Event<ICharacterInventory, Item> Event => ICharacterInventory.Events.PickedUpItem;
        protected override ICharacterInventory Source(IListenerContext context) => context.Character.Inventory;
    }
    
    [UnitCategory("AR/General/Events/Items")]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnItemDropped : GraphEvent<ICharacterInventory, DroppedItemData> {
        protected override Event<ICharacterInventory, DroppedItemData> Event => ICharacterInventory.Events.ItemDropped;
        protected override ICharacterInventory Source(IListenerContext context) => context.Character.Inventory;
    }
    
    [UnitCategory("AR/General/Events/Items")]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnItemSold : GraphEvent<IMerchant, Item> {
        protected override Event<IMerchant, Item> Event => IMerchant.Events.ItemSold;
        protected override IMerchant Source(IListenerContext context) =>  context.Character as Hero;
    }
    
    [UnitCategory("AR/General/Events/Items")]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnItemBought : GraphEvent<IMerchant, Item> {
        protected override Event<IMerchant, Item> Event => IMerchant.Events.ItemBought;
        protected override IMerchant Source(IListenerContext context) => context.Character as Hero;
    }
    
    [UnitCategory("AR/General/Events/Items")]
    [UnityEngine.Scripting.Preserve]
    public class EvtAfterEquipmentChanged : GraphEvent<ICharacterInventory, ICharacterInventory> {
        protected override Event<ICharacterInventory, ICharacterInventory> Event => ICharacterInventory.Events.AfterEquipmentChanged;
        protected override ICharacterInventory Source(IListenerContext context) => context.Character.Inventory;
    }
    
    [UnitCategory("AR/General/Events/Items")]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnEquip : GraphEvent<ICharacterInventory, Item> {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [RichEnumExtends(typeof(EquipmentSlotType))]
        public RichEnumReference slot;

        protected override Event<ICharacterInventory, Item> Event => ICharacterInventory.Events.SlotEquipped(slot.EnumAs<EquipmentSlotType>());
        protected override ICharacterInventory Source(IListenerContext context) => context.Character.Inventory;
    }
    
    [UnitCategory("AR/General/Events/Items")]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnUnequip : GraphEvent<ICharacterInventory, Item> {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [RichEnumExtends(typeof(EquipmentSlotType))]
        public RichEnumReference slot;

        protected override Event<ICharacterInventory, Item> Event => ICharacterInventory.Events.SlotUnequipped(slot.EnumAs<EquipmentSlotType>());
        protected override ICharacterInventory Source(IListenerContext context) => context.Character.Inventory;
    }
    
    [UnitCategory("AR/General/Events/Items")]
    [UnityEngine.Scripting.Preserve]
    public class EvtAllChargesSpent : GraphEvent<Item, int> {
        protected override Event<Item, int> Event => IItemWithCharges.Events.AllChargesSpent;
        protected override Item Source(IListenerContext context) => context.Item;
    }
}
