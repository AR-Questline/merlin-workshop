using System;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Shops {
    public class ShopTemplate : Template {
        [InfoBox("Sell = Shop is selling item to Hero\nBuy = Shop is buying item from Hero\nResult price = Base price * Modifier (Item base price is 10, modifier is 1.5 so shop price is 15)")]
        public float sellModifier = 1.1f;
        public float buyModifier = 0.9f;

        public bool IsFence;
        [ShowIf(nameof(IsFence))] public float fenceBuyModifierMultiplier = 0.5f;
        public StockData[] restockableItems = Array.Empty<StockData>();
        public ItemSpawningData[] uniqueItems = Array.Empty<ItemSpawningData>();
        public IntRange restockWealthGain;
        public int maxWealth;

        [Tags(TagsCategory.Item), PropertyOrder(0), Space]
        public string[] shopCantBuyItemsWithTheseTags = Array.Empty<string>();
    }

    [Serializable]
    public class StockData {
        [SerializeField, TemplateType(typeof(LootTableAsset))]
        TemplateReference table;

        [SerializeField] int capacity;
        [SerializeField] bool removeAllOnRestock;
        [SerializeField, HideIf(nameof(removeAllOnRestock))] IntRange removeOnRestock;
        
        public LootTableAsset Table => table.Get<LootTableAsset>();
        public int Capacity => capacity;
        public bool RemoveAllOnRestock => removeAllOnRestock;
        public IntRange RemoveOnRestock => removeOnRestock;
    }
}