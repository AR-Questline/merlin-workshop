using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Locations.Shops.Stocks {
    /// <summary>
    /// Shop stock which on every restock removes some of its items and add some of item from loot table
    /// </summary>
    public partial class RestockableStock : Stock {
        public override ushort TypeForSerialization => SavedModels.RestockableStock;

        [Saved] LootTableAsset LootTable { get; set; }
        [Saved] int Capacity { get; set; }
        [Saved] bool RemoveAllOnRestock { get; set; }
        [Saved] IntRange RemoveOnRestock { get; set; }
        [Saved] List<ItemSpawningDataRuntime> _compressedItems = new List<ItemSpawningDataRuntime>();
        
        public int Count => _isCompressed ? _compressedItems.Sum(data => data.quantity) : Items.Sum(item => item.Quantity);
        public override bool RestockOnce => RemoveAllOnRestock;
        protected override List<ItemSpawningDataRuntime> CompressedItems => _compressedItems;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        RestockableStock() {}
        
        public RestockableStock(StockData data) {
            LootTable = data.Table;
            Capacity = data.Capacity;
            RemoveAllOnRestock = data.RemoveAllOnRestock;
            RemoveOnRestock = data.RemoveOnRestock;
        }

        public override void Restock() {
            TryRemoveOnRestock();

            if (Count < Capacity) {
                try {
                    var items = LootTable?.PopLoot().items ?? Enumerable.Empty<ItemSpawningDataRuntime>();
                    foreach (var itemData in items) {
                        if (itemData.ItemTemplate == null) continue;
                        AddItemData(itemData);
                    }
                } catch (Exception e) {
                    Log.Important?.Error($"Exception below happened on popping loot from RestockableStock of ShopTemplate ({ParentModel.Template.GUID})", ParentModel.Template);
                    Debug.LogException(e, ParentModel.Template);
                }
            }
        }

        void TryRemoveOnRestock() {
            if (RemoveAllOnRestock) {
                CompressedItems.Clear();
            } else {
                foreach (var itemData in RandomUtil.UniformSelectMultiple(CompressedItems.ToList(), RemoveOnRestock.RandomPick())) {
                    CompressedItems.Remove(itemData);
                }
            }
        }
    }
}