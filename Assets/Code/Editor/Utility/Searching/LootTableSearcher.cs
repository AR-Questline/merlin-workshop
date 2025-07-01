using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Shops;
using UnityEditor;

namespace Awaken.TG.Editor.Utility.Searching {
    public class LootTableAssetSearcher : ProgressiveSearcher {
        protected override ITemplateQuery[] TemplateQueries => new ITemplateQuery[] {
            new TemplateQuery<LootTableAsset>(SearchResult),
            new TemplateQuery<ShopTemplate>(SearchResult),
            new TemplateQuery<NpcTemplate>(SearchResult), 
            new AttachmentQuery<LootInteractAttachment>(SearchResult),
            new AttachmentQuery<SearchAttachment>(SearchResult),
        };
        protected override ISceneQuery[] SceneQueries => new ISceneQuery[] {
            new SceneQuery<LootInteractAttachment>(SearchResult),
            new SceneQuery<SearchAttachment>(SearchResult),
        };
        
        [MenuItem("TG/Assets/Search/Invalid LootTableAssets", priority = 2500)]
        static void OpenWindow() {
            var window = GetWindow<LootTableAssetSearcher>();
            window.Show();
        }

        static bool SearchResult(LootTableAsset loot, out Result result) {
            result = new Result(loot, "table");
            return TestLootTable(loot.table);
        }

        static bool SearchResult(ShopTemplate shop, out Result result) {
            bool needChange = false;
            string note = "";

            if (shop.restockableItems != null) {
                for (int i = 0; i < shop.restockableItems.Length; i++) {
                    if (shop.restockableItems[i].Table == null) {
                        note += $"restockableItems[{i}]";
                        needChange = true;
                    }
                }
            }
            
            result = new Result(shop, note);
            return needChange;
        }
        
        static bool SearchResult(NpcTemplate npc, out Result result) {
            bool needChange = false;
            string note = "";

            TestWrapperExplicit(npc.inventoryItems, ref note, ref needChange, "InventoryItems\n");
            foreach (var wrapper in npc.lootTables) {
                TestWrapperExplicit(wrapper, ref note, ref needChange, "LootItems\n");
            }

            result = new Result(npc, note);
            return needChange;
        }

        static bool SearchResult(LootInteractAttachment loot, out Result result) {
            bool needChange = false;
            string note = "";

            TestWrapperExplicit(loot.lootTable, ref note, ref needChange, "LootTable\n");

            result = new Result(loot, note);
            return needChange;
        }

        static bool SearchResult(SearchAttachment search, out Result result) {
            bool needChange = false;
            string note = "";

            TestWrapperExplicit(search.lootTableWrapper, ref note, ref needChange, "LootTableWrapper\n");

            result = new Result(search, note);
            return needChange;
        }

        static void TestWrapperExplicit(LootTableWrapper wrapper, ref string note, ref bool needChange, string noteAppend) {
            if (wrapper.Type == LootTableWrapper.LootType.Explicit && wrapper.LootTableAsset(null) == null) {
                note += $"[LootTableWrapper.Explicit] {noteAppend}";
                needChange = true;
            }

            if (wrapper.Type == LootTableWrapper.LootType.Embed && TestLootTable(wrapper.EmbedTable)) {
                note += $"[LootTableWrapper.Embed] {noteAppend}";
                needChange = true;
            }
        }

        static bool TestLootTable(ILootTable table) {
            return table switch {
                LootTableAssetRef assetRef => !assetRef.Table.IsSet,
                LootArray array => array.array.Any(TestLootTable),
                LootLevelOverride levelOverride => TestLootTable(levelOverride.loot),
                LootTableFlagConditional flagConditional => TestLootTable(flagConditional.ifFalse) || TestLootTable(flagConditional.ifTrue),
                LootTableMultiplier multiplier => TestLootTable(multiplier.loot),
                LootWeightArray weightArray => weightArray.loots.Select(loot => loot.loot).Any(TestLootTable),
                LootWithDropChance dropChance => TestLootTable(dropChance.loot),
                _ => false
            };
        }
    }
}