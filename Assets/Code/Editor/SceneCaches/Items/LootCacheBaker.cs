using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.NoticeBoards;
using Awaken.TG.Main.Locations.Pickables;
using Awaken.TG.Main.Locations.Regrowables;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Locations.Spawners.Critters;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine;
using SceneReference = Awaken.TG.Assets.SceneReference;

namespace Awaken.TG.Editor.SceneCaches.Items {
    public class LootCacheBaker : SceneBaker<LootCache> {
        protected override LootCache LoadCache => LootCache.Get;

        static readonly List<ItemTemplate> ItemCache = new(200);

        static FactionTree s_tree;
        static Faction s_heroFaction;

        public override void Bake(SceneReference sceneRef) {
            s_tree = new FactionTree(TemplatesSearcher.FindAllOfType<FactionTemplate>().ToArray());
            FactionTemplate heroFactionTemplate =
                AssetDatabase.LoadAssetAtPath<FactionTemplate>(AssetDatabase.GUIDToAssetPath("f541926e32941de40ad78cd27c3d7f99"));
            s_heroFaction = s_tree.FactionByTemplate(heroFactionTemplate);

            var itemSources = FindAllItemSources(sceneRef).ToList();
            var sceneItemSources = new SceneItemSources(sceneRef) {
                sources = itemSources
            };
            Cache.sceneSources.Add(sceneItemSources);
        }

        static IEnumerable<ItemSource> FindAllItemSources(SceneReference sceneRef) {
            var sharedData = new SharingCache();
            foreach (var go in CacheBakerUtils.ForEachSceneGO()) {
                foreach (var itemSource in GetItemSourcesFromGameObject(go, go, sharedData)) {
                    if (itemSource != null && itemSource.lootData.Any()) {
                        PostProcessItemSource(itemSource);
                        yield return itemSource;
                    }
                }
            }

            foreach (var location in LocationCache.Get.GetAllSpawnedLocations(sceneRef)) {
                foreach (var itemSource in GetItemSourcesFromGameObject(location.SpawnedLocationTemplate.gameObject, location.SceneGameObject, sharedData)) {
                    if (itemSource != null && itemSource.lootData.Any()) {
                        foreach (ItemLootData lootData in itemSource.lootData) {
                            lootData.Grindable |= location.Respawns;
                            lootData.OnlyNight |= location.OnlyNight;
                            lootData.minCount *= location.spawnAmount;
                            lootData.maxCount *= location.spawnAmount;
                        }

                        PostProcessItemSource(itemSource);
                        yield return itemSource;
                    }
                }
            }
        }

        static IEnumerable<ItemSource> GetItemSourcesFromGameObject(GameObject go, GameObject sceneGo, SharingCache sharingCache) {
            // TODO: Add Hero initial items, fishing, ?

            if (go.TryGetComponent(out NpcAttachment npc)) {
                if (npc.IsUnique && !sharingCache.uniqueNpcs.Add(go)) {
                    yield break;
                }

                yield return GetFromNpcAttachment(npc, sceneGo);
            }

            foreach (var (bookmark, component) in CacheBakerUtils.ForEachStoryIn(go)) {
                var itemSource = GetItemSourceFromStoryGraph(bookmark, sceneGo);
                if (component is NpcAttachment) {
                    itemSource.lootData.ForEach(l => l.OwnedByNpc = true);
                }

                yield return itemSource;
            }

            if (go.TryGetComponent(out PickableSpec pickable)) {
                yield return GetItemSourceFromPickable(pickable, sceneGo);
            }

            if (go.TryGetComponent(out IRegrowableSpec regrowable)) {
                foreach (var source in GetItemSourceFromRegrowable(regrowable, sceneGo)) {
                    yield return source;
                }
            }

            if (go.TryGetComponent(out PickItemAttachment pickAttachment)) {
                yield return GetItemSourceFromPickAttachment(pickAttachment, sceneGo);
            }

            if (go.TryGetComponent(out SearchAttachment search)) {
                yield return GetItemSourceFromSearch(search, sceneGo);
            }

            if (go.TryGetComponent(out LootInteractAttachment loot)) {
                yield return GetItemSourceFromLoot(loot, sceneGo);
            }

            if (go.TryGetComponent(out NpcDummyAttachment dummy)) {
                yield return GetItemSourceFromDummy(dummy, sceneGo);
            }

            if (go.TryGetComponent(out ShopAttachment shop)) {
                yield return GetItemSourceFromShop(shop, sceneGo);
            }

            if (go.TryGetComponent(out CritterSpawnerAttachment critters)) {
                foreach (var item in GetItemSourcesFromCritters(critters, sceneGo, sharingCache)) {
                    yield return item;
                }
            }

            if (go.TryGetComponent(out NoticeBoardAttachment noticeBoard)) {
                yield return GetItemSourceFromNoticeBoard(noticeBoard, sceneGo);
            }
        }

        static void PostProcessItemSource(ItemSource itemSource) {
            List<ItemLootData> toAdd = new();
            foreach (var loot in itemSource.lootData) {
                var readSpec = loot.Template?.GetComponent<ItemReadSpec>();
                if (readSpec != null && StoryBookmark.ToInitialChapter(readSpec.StoryRef, out var bookmark)) {
                    toAdd.AddRange(GetLootFromStoryGraph(bookmark));
                }

                var recipeSpec = loot.Template?.GetComponent<RecipeItemAttachment>();
                if (recipeSpec != null) {
                    var recipe = recipeSpec.Recipe;
                    var outcome = recipe?.Outcome;
                    if (outcome != null) {
                        var itemLootData = new ItemLootData(new TemplateReference(outcome.GUID), grindable: true);
                        itemLootData.Conditional = true;
                        toAdd.Add(itemLootData);
                    }
                }
                
                var unidentifiedItemAttachment = loot.Template?.GetComponent<UnidentifiedItemAttachment>();
                if (unidentifiedItemAttachment != null) {
                    var unidentifiedLootTable = unidentifiedItemAttachment.LootTableWrapper?.LootTable(itemSource.SceneGameObject);
                    toAdd.AddRange(GetLootFromLootTable(unidentifiedLootTable, itemSource.SceneGameObject));
                }
            }

            itemSource.lootData.AddRange(toAdd);
        }

        static ItemSource GetItemSourceFromPickable(PickableSpec pickableSpec, GameObject sceneGO) {
            if (pickableSpec.ItemData?.itemTemplateReference is not { IsSet: true }) {
                return null;
            }

            if (!pickableSpec.ItemData.itemTemplateReference.TryGet(out ItemTemplate itemTemplate)) {
                return null;
            }
            
            var itemSource = new ItemSource(sceneGO);
            itemSource.lootData.Add(new ItemLootData(pickableSpec.ItemData.itemTemplateReference, pickableSpec.ItemData.quantity) {
                IsStealable = IsStealable(sceneGO, itemTemplate.CrimeValue)
            });
            return itemSource;
        }

        static IEnumerable<ItemSource> GetItemSourceFromRegrowable(IRegrowableSpec regrowable, GameObject sceneGO) {
            for (var i = 0u; i < regrowable.Count; i++) {
                var itemReference = regrowable.ItemReference(i);

                if (itemReference?.itemTemplateReference is not { IsSet: true }) {
                    continue;
                }

                if (!itemReference.itemTemplateReference.TryGet(out ItemTemplate itemTemplate)) {
                    continue;
                }

                var itemSource = new ItemSource(sceneGO);
                itemSource.lootData.Add(new ItemLootData(itemReference.itemTemplateReference, itemReference.quantity, grindable: true) {
                    IsStealable = IsStealable(sceneGO, itemTemplate.CrimeValue)
                });
                yield return itemSource;
            }
        }

        static ItemSource GetItemSourceFromPickAttachment(PickItemAttachment pickAttachment, GameObject sceneGO) {
            if (pickAttachment.itemReference?.itemTemplateReference is not { IsSet: true }) {
                return null;
            }

            if (!pickAttachment.itemReference.itemTemplateReference.TryGet(out ItemTemplate itemTemplate)) {
                return null;
            }
            
            var itemSource = new ItemSource(sceneGO);
            itemSource.lootData.Add(new ItemLootData(pickAttachment.itemReference.itemTemplateReference, pickAttachment.itemReference.quantity) {
                IsStealable = IsStealable(sceneGO, itemTemplate.CrimeValue)
            });
            return itemSource;
        }

        static ItemSource GetItemSourceFromNoticeBoard(NoticeBoardAttachment noticeBoard, GameObject sceneGo) {
            var itemSource = new ItemSource(sceneGo);
            
            foreach (var notice in noticeBoard.notices) {
                foreach (var entry in notice.entries) {
                    if (entry.item is { IsSet: true }) {
                        var itemTemplate = entry.item.TryGet<ItemTemplate>();
                        if (itemTemplate != null) {
                            var lootData = new ItemLootData(entry.item);
                            lootData.Conditional = true;
                            itemSource.lootData.Add(lootData);
                        }
                    }
                }
            }

            return itemSource;
        }

        static ItemSource GetItemSourceFromSearch(SearchAttachment search, GameObject sceneGO) {
            var itemSource = new ItemSource(sceneGO);
            itemSource.lootData.AddRange(GetLootFromLootTableWrapper(search.lootTableWrapper, sceneGO));
            itemSource.lootData.AddRange(search.additionalItemsFromBerlin?
                .Where(i => i.itemTemplateReference is { IsSet: true } && i.itemTemplateReference.TryGet<ItemTemplate>())
                .Select(i => new ItemLootData(i.itemTemplateReference, i.quantity)
                    { IsStealable = IsStealable(sceneGO, i.itemTemplateReference.Get<ItemTemplate>().CrimeValue) }) ?? Array.Empty<ItemLootData>());
            return itemSource;
        }

        static ItemSource GetItemSourceFromLoot(LootInteractAttachment loot, GameObject sceneGO) {
            var itemSource = new ItemSource(sceneGO);
            itemSource.lootData.AddRange(GetLootFromLootTableWrapper(loot.lootTable, sceneGO));
            return itemSource;
        }

        static ItemSource GetItemSourceFromDummy(NpcDummyAttachment dummy, GameObject sceneGO) {
            var itemSource = new ItemSource(sceneGO);
            foreach (var item in dummy.initialItems) {
                if (item?.itemTemplateReference is { IsSet: true }) {
                    itemSource.lootData.Add(new ItemLootData(item.itemTemplateReference, item.quantity) {
                        IsStealable = IsStealable(dummy.npcTemplate?.Get<NpcTemplate>())
                    });
                }
            }

            return itemSource;
        }

        static ItemSource GetFromNpcAttachment(NpcAttachment npc, GameObject sceneGO) {
            var template = npc.NpcTemplate;
            var itemSource = new ItemSource(sceneGO);
            itemSource.lootData.AddRange(GetLootFromLootTableWrapper(template.inventoryItems, sceneGO));

            try {
                foreach (var table in template.Loot ?? Enumerable.Empty<ILootTable>()) {
                    itemSource.lootData.AddRange(GetLootFromLootTable(table, sceneGO));
                }

                foreach (var table in template.CorpseLoot ?? Enumerable.Empty<ILootTable>()) {
                    itemSource.lootData.AddRange(GetLootFromLootTable(table, sceneGO));
                }

                foreach (var table in template.WyrdConvertedLoot ?? Enumerable.Empty<ILootTable>()) {
                    itemSource.lootData.AddRange(GetLootFromLootTable(table, sceneGO).ForEachDeferred(i => i.OnlyNight = true));
                }
                
                var deathStory = npc.StoryOnDeath?.Get<StoryGraph>();
                if (deathStory != null) {
                    itemSource.lootData.AddRange(GetLootFromStoryGraph(StoryBookmark.EDITOR_ToInitialChapter(deathStory)));
                }
            } catch (Exception e) {
                Log.Minor?.Error($"Invalid loot in template {template.name}");
                Debug.LogException(e);
            }

            itemSource.lootData.ForEach(i => {
                i.OwnedByNpc = true;
                i.IsStealable = IsStealable(npc.NpcTemplate);
            });
            return itemSource;
        }

        static ItemSource GetItemSourceFromShop(ShopAttachment shop, GameObject sceneGo) {
            var shopTemp = shop.shopDefinition.Get<ShopTemplate>();
            var itemSource = new ItemSource(sceneGo);

            foreach (var item in shopTemp.uniqueItems) {
                if (item.itemTemplateReference is not { IsSet: true } 
                    || !item.itemTemplateReference.TryGet(out ItemTemplate itemTemplate)) {
                    continue;
                }
                
                itemSource.lootData.Add(new ItemLootData(item.itemTemplateReference, item.quantity) {
                    IsStealable = IsStealable(sceneGo, itemTemplate.CrimeValue)
                });
            }

            foreach (var stock in shopTemp.restockableItems) {
                foreach (var item in GetLootFromLootTable(stock?.Table?.table, sceneGo)) {
                    item.Grindable = true;
                    itemSource.lootData.Add(item);
                }
            }

            return itemSource;
        }

        static ItemSource GetItemSourceFromStoryGraph(StoryBookmark bookmark, GameObject sceneGo) {
            var itemSource = new ItemSource(sceneGo);
            itemSource.lootData.AddRange(GetLootFromStoryGraph(bookmark));
            return itemSource;
        }

        static IEnumerable<ItemSource> GetItemSourcesFromCritters(CritterSpawnerAttachment critters, GameObject sceneGo, SharingCache sharingCache) {
            var locationTemplate = critters.DropTemplateRef.Get<LocationTemplate>();
            foreach (var item in GetItemSourcesFromGameObject(locationTemplate.gameObject, sceneGo, sharingCache)) {
                item.lootData.ForEach(i => {
                    i.minCount *= critters.Count;
                    i.maxCount *= critters.Count;
                    i.Grindable = true;
                });
                yield return item;
            }
        }

        static IEnumerable<ItemLootData> GetLootFromStoryGraph(StoryBookmark bookmark) {
            if (bookmark == null || !bookmark.IsValid) {
                yield break;
            }

            foreach (var element in StoryExplorerUtil.ExtractElements(bookmark)) {
                if (element is SEditorChangeItemsQuantity itemStep) {
                    foreach (var pair in itemStep.itemTemplateReferenceQuantityPairs) {
                        if (pair.quantity > 0) {
                            var itemLootData = new ItemLootData(pair.itemTemplateReference, pair.quantity);
                            itemLootData.Conditional = true;
                            yield return itemLootData;
                        }
                    }

                    if (itemStep.lootTableReference is { IsSet: true }) {
                        foreach (var lootData in itemStep.lootTableReference.Get<LootTableAsset>().EDITOR_PopLootData()) {
                            lootData.Conditional = true;
                            yield return lootData;
                        }
                    }

                    if (itemStep.taggedQuantity > 0 && (itemStep.tags?.Any() ?? false)) {
                        ItemCache.Clear();
                        TemplatesSearcher.FindAllOfType(ItemCache, TemplateTypeFlag.Regular);
                        ItemCache.RemoveAll(item => !TagUtils.HasRequiredTags(item.EditorTagsNoRefresh, itemStep.tags));

                        foreach (var potentialItem in ItemCache) {
                            var templateRef = new TemplateReference(potentialItem.GUID);
                            float probability = 1f - Mathf.Pow((ItemCache.Count - 1f) / ItemCache.Count, itemStep.taggedQuantity);
                            var itemLootData = new ItemLootData(templateRef, 1, probability);
                            itemLootData.Conditional = true;
                            yield return itemLootData;
                        }

                        ItemCache.Clear();
                    }
                } else if (element is SEditorLearnRecipe learnRecipe) {
                    var recipe = learnRecipe.recipe.TryGet<IRecipe>(); 
                    var outcome = recipe?.Outcome;
                    if (outcome != null) {
                        var itemLootData = new ItemLootData(new TemplateReference(outcome.GUID), grindable: true);
                        itemLootData.Conditional = true;
                        yield return itemLootData;
                    }
                }
            }
        }

        static IEnumerable<ItemLootData> GetLootFromLootTableWrapper(LootTableWrapper wrapper, GameObject sceneGO) {
            ILootTable lootTable = null;
            try {
                lootTable = wrapper?.LootTable(sceneGO);
            } catch (Exception e) {
                Log.Minor?.Error($"Invalid loot table in: {sceneGO.scene.name}: {sceneGO.PathInSceneHierarchy()}");
                Debug.LogException(e);
            }

            foreach (var item in GetLootFromLootTable(lootTable, sceneGO)) {
                yield return item;
            }
        }

        static IEnumerable<ItemLootData> GetLootFromLootTable(ILootTable table, GameObject sceneGO) {
            if (table == null) {
                yield break;
            }

            List<ItemLootData> list = new();
            try {
                foreach (var item in table.EDITOR_PopLootData()) {
                    if (item.Template != null && !item.Template.HiddenOnUI) {
                        item.IsStealable = IsStealable(sceneGO, item.Template.CrimeValue);
                        list.Add(item);
                    }
                }
            } catch (Exception e) {
                Log.Minor?.Error($"Invalid loot table in: {sceneGO.scene.name}: {sceneGO.PathInSceneHierarchy()}");
                Debug.LogException(e);
            }

            // Merge item loot data with the same template, sum their quantity
            var mergedList = list
                .GroupBy(i => (i.template, probability: (float)Math.Round(i.probability, 3), i.tags))
                .Select(g => {
                    var itemLootData = new ItemLootData(g.Key.template, g.Sum(i => i.minCount), g.Sum(i => i.maxCount), g.Key.probability) {
                        IsStealable = IsStealable(sceneGO, g.Key.template.Get<ItemTemplate>().CrimeValue)
                    };
                    itemLootData.tags = g.Key.tags;
                    return itemLootData;
                });

            foreach (var item in mergedList) {
                yield return item;
            }
        }

        class SharingCache {
            public readonly HashSet<GameObject> uniqueNpcs = new();
        }

        /// Helpers
        static bool IsStealable(GameObject go, CrimeItemValue crimeItemValue) {
            CrimeOwnerUtils.GetCrimeOwnersOfRegion(CrimeType.Theft, go.transform.position, out var factions);
            return factions.AllOwners.Any(f => !f.IsAcceptable(CrimeArchetype.Theft(crimeItemValue)));
        }

        static bool IsStealable(NpcTemplate npcTemplate) {
            if (!npcTemplate) {
                return false;
            }
            
            var factionTemplate = npcTemplate.FactionEditorContext;
            if (factionTemplate != null) {
                var npcFaction = s_tree.FactionByTemplate(factionTemplate);
                return !npcFaction.IsHostileTo(s_heroFaction);
            }

            return false;
        }
    }
}