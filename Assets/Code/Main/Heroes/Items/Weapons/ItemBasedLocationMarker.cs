using Awaken.Utility;
using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items.Weapons {
    public partial class ItemBasedLocationMarker : Element<Location> {
        public override ushort TypeForSerialization => SavedModels.ItemBasedLocationMarker;

        [Saved] protected ItemBasedLocationData _data;

        [CanBeNull] public ICharacter Owner => _data.Owner;
        [CanBeNull] public Item SourceItem => _data.SourceItem;
        public ItemTemplate ItemTemplate => _data.ItemTemplate;
        public ItemBasedLocationData Data => _data;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public ItemBasedLocationMarker() { }

        public ItemBasedLocationMarker(ICharacter owner, Item sourceItem) {
            _data = new ItemBasedLocationData(owner, sourceItem);
        }
    }

    [Serializable]
    public partial struct ItemBasedLocationData {
        public ushort TypeForSerialization => SavedTypes.ItemBasedLocationData;

        [Saved] WeakModelRef<ICharacter> _owner;
        [Saved] WeakModelRef<Item> _sourceItem;
        [Saved] ItemTemplate _itemTemplate;

        public ItemBasedLocationData(ICharacter owner, Item sourceItem) 
            : this(new WeakModelRef<ICharacter>(owner), new WeakModelRef<Item>(sourceItem), sourceItem?.Template) { }

        public ItemBasedLocationData(WeakModelRef<ICharacter> owner, WeakModelRef<Item> sourceItem, ItemTemplate itemTemplate) {
            _owner = owner;
            _sourceItem = sourceItem;
            _itemTemplate = itemTemplate;
        }
        
        [CanBeNull] public ICharacter Owner => _owner.Get();
        [CanBeNull] public Item SourceItem => _sourceItem.Get();
        public ItemTemplate ItemTemplate => _itemTemplate;
    }

    public static class ItemBasedLocationMarkerUtils {
        [UnityEngine.Scripting.Preserve] 
        public static ItemBasedLocationMarker AddItemBasedLocationMarker(this Location location, ICharacter owner, Item sourceItem) {
            return location.AddElement(new ItemBasedLocationMarker(owner, sourceItem));
        }
        
        [UnityEngine.Scripting.Preserve]
        public static ICharacter GetLocationOwner(this Location location) {
            if (location.TryGetElement<ItemBasedLocationMarker>(out var marker)) {
                return marker.Owner;
            }
            return null;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static Item GetLocationSourceItem(this Location location) {
            if (location.TryGetElement<ItemBasedLocationMarker>(out var marker)) {
                return marker.SourceItem;
            }
            return null;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static ItemTemplate GetLocationSourceItemTemplate(this Location location) {
            if (location.TryGetElement<ItemBasedLocationMarker>(out var marker)) {
                return marker.ItemTemplate;
            }
            return null;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static ItemBasedLocationData GetLocationData(this Location location) {
            if (location.TryGetElement<ItemBasedLocationMarker>(out var marker)) {
                return marker.Data;
            }
            return new ItemBasedLocationData();
        }
    }
}