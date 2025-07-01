using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using FMODUnity;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.Items {
    public class DroppedItemSpawner : IService {
        static readonly TimeSpan DroppedItemLifetime = TimeSpan.FromDays(1);
        
        static Hero Hero => Hero.Current;

        LocationTemplate ItemDropParent {
            get {
                if (_itemDropParent == null) {
                    _itemDropParent = World.Services.Get<GameConstants>().DefaultItemDropPrefab;
                }
                return _itemDropParent;
            }
        }
        LocationTemplate _itemDropParent;
        public Transform DroppedItemsParent { get; private set; }
        
        public void Init() {
            DroppedItemsParent = new GameObject("---Dropped Items Parent---").GetComponent<Transform>();
            World.EventSystem.ListenTo(EventSelector.AnySource, ICharacterInventory.Events.ItemDropped, this, OnItemDropped);
        }

        void OnItemDropped(DroppedItemData data) {
            var item = data.item;
            if (item.Template.DropPrefab?.IsSet ?? false) {
                SpawnDroppedItemPrefab(Hero.Coords + Hero.Forward() + new Vector3(0, 1.2f, 0), data);
                EventReference eventReference = ItemAudioType.DropItem.RetrieveFrom(item);
                if (!eventReference.IsNull) {
                    //RuntimeManager.PlayOneShot(eventReference);
                }
                RewiredHelper.VibrateLowFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
            } else {
                Log.Important?.Info($"Item: {item.Template.ItemName}, doesn't have assigned drop prefab");
            }
        }

        public static Location SpawnDroppedItemPrefab(Vector3 spawnPosition, DroppedItemData itemData, Quaternion? rotation = null, Vector3? force = null) {
            Item item = itemData.item;
            return SpawnDroppedItemPrefab(spawnPosition, item.Template, itemData.quantity, item.Level.ModifiedInt, item.WeightLevel.ModifiedInt, rotation, force, itemData.elementsData, item.DisplayName);
        }
        
        public static Location SpawnDroppedItemPrefab(Vector3 spawnPosition, ItemTemplate template, int quantity, int itemLevel = 0, int weightLevel = 0,
            Quaternion? rotation = null, Vector3? force = null, ItemElementsDataRuntime elementsData = null, string itemName = null) {
            
            DroppedItemSpawner spawner = World.Services.Get<DroppedItemSpawner>();
            LocationTemplate itemDropParent = spawner.ItemDropParent;

            Location location = itemDropParent.SpawnLocation(spawnPosition, rotation ?? Quaternion.identity, Vector3.one, template.DropPrefab.Get(), itemName ?? template.ItemName);
            location.AddElement(new LifetimeElement(DroppedItemLifetime));
            var spawningData = new ItemSpawningDataRuntime(template) { quantity = quantity, itemLvl = itemLevel, weightLvl = weightLevel, elementsData = elementsData };
            location.AddElement(new PickItemAction(spawningData, true));
            location.AddElement(new ItemRigidbody(force));
            location.AddElement(new NoLocationCrimeOverride());
            return location;
        }
    }
}