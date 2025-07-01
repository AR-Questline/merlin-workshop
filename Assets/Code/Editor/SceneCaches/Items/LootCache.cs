using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Editor.SceneCaches.Items {
    public class LootCache : BaseCache, ISceneCache<SceneItemSources, ItemSource> {
        static LootCache s_cache;
        public static LootCache Get => s_cache ??= BaseCache.LoadFromAssets<LootCache>("caddfb4805a25e147844ae7db802c127");
        
        [Searchable, ListDrawerSettings(NumberOfItemsPerPage = 10, IsReadOnly = true, ListElementLabelName = nameof(SceneItemSources.SceneName))]
        public List<SceneItemSources> sceneSources = new();

        Dictionary<ItemTemplate, LootSearchWindow.ItemSetupOutput> _lootCacheByItem;
        
        public IEnumerable<SceneItemSources> FindOccurrencesOf(ItemTemplate template) {
            string templateGuid = template.GUID;
            
            foreach (var sceneSource in sceneSources) {
                SceneItemSources newSceneSource = new(sceneSource.sceneRef);
                foreach (var source in sceneSource.sources) {
                    ItemSource newSource = source.GetCopyExceptLootData();
                    newSource.lootData.AddRange(source.GetItems().Where(i => i.template?.GUID == templateGuid));
                    if (newSource.lootData.Count > 0) {
                        newSceneSource.sources.Add(newSource);
                    }
                }
                
                if (newSceneSource.sources.Any()) {
                    yield return newSceneSource;
                }
            }
        }

        public void GenerateRegionFilters(OnDemandCache<string, ExportRegionFilter> cache) {
            OnDemandCache<string, HashSet<string>> scenesUsedByItem = new(_ => new HashSet<string>(50));
            
            foreach (var sceneSource in sceneSources) {
                foreach (var source in sceneSource.sources) {
                    foreach (var itemLootData in source.lootData) {
                        string guid = itemLootData.template?.GUID;
                        if (guid != null) {
                            if (scenesUsedByItem[guid].Add(source.SceneName)) {
                                string region = source.OpenWorldRegion;
                                cache[guid] |= RegionFilterUtil.GetRegionFrom(region);
                            }
                        }
                    }
                }
            }
        }

        public LootSearchWindow.ItemSetupOutput GetLootCache(ItemTemplate item) {
            if (_lootCacheByItem == null) {
                PrepareLootCacheByItem();
            }
            return _lootCacheByItem.GetValueOrDefault(item);
        }

        public void PrepareLootCacheByItem() {
            _lootCacheByItem = new(1000);
            
            foreach (var sceneSource in sceneSources) {
                string sceneName = sceneSource.SceneName;
                foreach (var source in sceneSource.sources) {
                    foreach (var itemLootData in source.lootData) {
                        if (itemLootData.Template != null) {
                            AddToLootCacheByItem(itemLootData, sceneName);
                        }
                    }
                }
            }
            
            foreach (var pair in _lootCacheByItem) {
                foreach (var lootInfo in pair.Value.generatedInfoPerScene.Values) {
                    lootInfo.Setup();
                }
            }
        }

        void AddToLootCacheByItem(ItemLootData lootData, string sceneName) {
            if (!_lootCacheByItem.TryGetValue(lootData.Template, out var output)) {
                output = new();
                _lootCacheByItem[lootData.Template] = output;
            }
            var sceneLootInfo = output.generatedInfoPerScene[sceneName];
            LootSearchWindow.AppendItemLootDataToSceneLootInfo(lootData, sceneLootInfo, 1f);
        }

        public override void Clear() {
            sceneSources.Clear();
        }

        [Button]
        void BakeCache() {
            SceneCacheBaker.Bake();
        }
        
        [Button]
        void ShowInvalidTemplates() {
            foreach (var sceneSource in sceneSources) {
                foreach (var source in sceneSource.sources) {
                    foreach (var data in source.lootData) {
                        if (data.Template == null) {
                            Log.Important?.Error($"Invalid template in {source.sceneRef.Name}: {source.scenePath}");
                        }
                    }
                }
            }
        }

        List<SceneItemSources> ISceneCache<SceneItemSources, ItemSource>.Data => sceneSources;
    }

    [Serializable]
    public class SceneItemSources : SceneDataSources, ISceneCacheData<ItemSource> {
        [PropertyOrder(1), Searchable, ListDrawerSettings(NumberOfItemsPerPage = 8, IsReadOnly = true, ShowFoldout = false, 
             ListElementLabelName = nameof(ItemSource.scenePath))]
        public List<ItemSource> sources = new(50);
        
        public SceneItemSources(SceneReference sceneRef) : base(sceneRef) { }

        SceneReference ISceneCacheData<ItemSource>.SceneRef => sceneRef;
        List<ItemSource> ISceneCacheData<ItemSource>.Sources => sources;
    }
    
    [Serializable]
    public class ItemSource : SceneSource, ISceneCacheSource, IEquatable<ItemSource> {
        [TableList(NumberOfItemsPerPage = 12, ShowPaging = true, AlwaysExpanded = true, IsReadOnly = true)]
        public List<ItemLootData> lootData = new();
        
        public ItemSource(GameObject go) : base(go) { }
        public ItemSource(SceneReference sceneRef, string path) : base(sceneRef, path) { }

        public IEnumerable<ItemLootData> GetItems() => lootData;
        public ItemSource GetCopyExceptLootData() => new(sceneRef, scenePath);

        public bool Equals(ItemSource other) {
            return ReferenceEquals(this, other);
        }
    }
}