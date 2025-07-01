using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel {
    public interface IItemsUIConfig {
        IEnumerable<Item> Items { get; }
        IEnumerable<ItemsTabType> Tabs { get; }
        ItemsTabType SortingTab => null;
        EquipmentSlotType EquipmentSlotType => null;
        bool TryToFocusFirstItemOnTheList => true;
        bool UseCategoryList => false;
        bool UseFilter => true;
        bool UseDefaultTab => true;
        
        ItemDescriptorType ItemDescriptorType => ItemDescriptorType.ExistingItem;
        string CustomMemoryContext => null;
        int MinRowsCount => 4;
        
        // UI Customization
        Type ItemsUIView => typeof(VItemsDefaultUI);
        Type ItemsListUIView => typeof(VItemsListDefaultUI);
        Type ItemsCategoryListUIView => typeof(VItemsListWithCategory);
        Type ItemsCategoryListHostView => typeof(VHostItemsListWithCategory);
        Type ItemsListElementView => typeof(VItemsListElement);
        LayoutPosition TabsPosition => LayoutPosition.Top; 
        string ContextTitle => string.Empty;
        bool AllowMultipleClickEventsOnTheSameItem => true;
        
        internal Transform ItemsHost { get; }
        internal UIResult HandleItemEvent(Item item, UIEvent evt) => UIResult.Ignore;
    }
    public enum LayoutPosition : byte {
        [UnityEngine.Scripting.Preserve] None,
        Top,
        Bottom,
        Left,
        Right
    }
}