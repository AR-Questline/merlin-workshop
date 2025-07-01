using System;
using System.Linq;
using System.Reflection;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Locations.Shops.Stocks;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.Patchers {
    [UnityEngine.Scripting.Preserve]
    public class Patcher026_027 : Patcher {
        protected override Version MaxInputVersion => new Version(0, 26);
        protected override Version FinalVersion => new Version(0, 27);

        public override void AfterRestorePatch() {
            PropertyInfo lootTableProperty = typeof(RestockableStock)
                .GetProperty("LootTable", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo removeAllProperty = typeof(RestockableStock)
                .GetProperty("RemoveAllOnRestock", BindingFlags.Instance | BindingFlags.NonPublic);
            
            foreach (var shop in World.All<Shop>()) {
                foreach (var restockable in shop.Elements<RestockableStock>()) {
                    
                    LootTableAsset restockableLootTable = lootTableProperty!.GetValue(restockable) as LootTableAsset;
                    if (restockableLootTable == null) {
                        Log.Important?.Error($"Something went wrong for {restockable.ID}");
                        continue;
                    }

                    StockData stockData = shop.Template.restockableItems.FirstOrDefault(s => s.Table == restockableLootTable);
                    if (stockData == null) {
                        Log.Important?.Error($"Haven't found StockData for {restockable.ID}");
                        continue;
                    }

                    bool desiredValue = stockData.RemoveAllOnRestock;
                    removeAllProperty!.SetValue(restockable, desiredValue);
                    Log.Important?.Error($"Changed value for {restockable.ID} to {desiredValue}");
                }
            }
        }
    }
}