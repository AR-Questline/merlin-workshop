using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.SceneCaches.Items;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.Utility.UI;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static Awaken.TG.Editor.SceneCaches.Visualization.SceneCacheDrawer;

namespace Awaken.TG.Editor.SceneCaches.Visualization.Drawers {
    [Serializable]
    public struct LootCacheDrawer : ISceneCacheDrawer<ItemSource, LootCacheDrawer.Metadata> {
        static readonly HashSet<AssetData<ItemTemplate>> ReusableItemSet = new();
        
        [SerializeField, BoxGroup("Settings")] float width;
        [SerializeField, BoxGroup("Settings")] int maxDisplayCount;
        [SerializeField, BoxGroup("Filter")] bool weapons;
        [SerializeField, BoxGroup("Filter")] bool armors;
        [SerializeField, BoxGroup("Filter")] bool consumables;
        [SerializeField, BoxGroup("Filter")] bool rest;
        
        public void Init() {
            width = 200;
            maxDisplayCount = 10;
            weapons = true;
        }
        
        public int FilterHash() {
            return (weapons ? 1 : 0) + (armors ? 2 : 0) + (consumables ? 4 : 0) + (rest ? 8 : 0);
        }

        public int PartsHash() {
            return width.GetHashCode() ^ maxDisplayCount.GetHashCode();
        }

        public bool Filter(ref Metadata metadata) {
            metadata.filteredItems = Array.Empty<AssetData<ItemTemplate>>();
            if (metadata.gameObject == null) {
                return false;
            }
            var set = ReusableItemSet;
            set.Clear();
            foreach (var loot in metadata.lootData) {
                var template = loot.template;
                if (template.asset == null) {
                    continue;
                }
                if (template.asset.IsWeapon) {
                    if (weapons) {
                        set.Add(template);
                    }
                } else if (template.asset.IsArmor) {
                    if (armors) {
                        set.Add(template);
                    }
                } else if (template.asset.IsConsumable) {
                    if (consumables) {
                        set.Add(template);
                    }
                } else {
                    if (rest) {
                        set.Add(template);
                    }
                }
            }
            metadata.filteredItems = set.ToArray();
            set.Clear();
            
            metadata.width = 0;
            foreach (var item in metadata.filteredItems) {
                metadata.width = math.max(metadata.width, item.width);
            }
            metadata.width = math.min(metadata.width, width);
            
            return metadata.filteredItems.Length > 0;
        }

        public void GetSize(in Metadata metadata, out float width, out float height) {
            width = 0;
            var itemsLines = math.min(metadata.filteredItems.Length, maxDisplayCount);
            for (int i = 0; i < itemsLines; i++) {
                width = math.max(width, metadata.filteredItems[i].width);
            }
            width = math.min(width, this.width);
            
            var lines = math.min(metadata.filteredItems.Length, maxDisplayCount + 1);
            height = lines * EditorGUIUtility.singleLineHeight;
        }

        public void Draw(in Metadata metadata, Rect rect) {
            var rects = new PropertyDrawerRects(rect);
            
            var itemsLines = math.min(metadata.filteredItems.Length, maxDisplayCount);
            for (int i = 0; i < itemsLines; i++) {
                GUIDraw(rects.AllocateLine(), metadata.filteredItems[i]);
            }

            if (metadata.filteredItems.Length > maxDisplayCount) {
                GUIDraw(rects.AllocateLine(), $"{metadata.filteredItems.Length - maxDisplayCount} more");
            }
        }

        public string LOD1Name(in Metadata metadata) {
            return $"{metadata.filteredItems.Length} items";
        }

        public Vector3 GetPosition(in Metadata metadata) {
            return metadata.gameObject?.transform.position ?? Vector3.zero;
        }

        public Metadata CreateMetadata(ItemSource source) {
            var go = source.SceneGameObject;
            var lootData = new ItemLootMetadata[source.lootData.Count];
            for (int i = 0; i < source.lootData.Count; i++) {
                lootData[i] = new ItemLootMetadata(source.lootData[i]);
            }
            return new Metadata {
                gameObject = go,
                lootData = lootData,
                filteredItems = Array.Empty<AssetData<ItemTemplate>>(),
            };
        }
        
        public struct Metadata {
            public GameObject gameObject;
            public ItemLootMetadata[] lootData;

            public AssetData<ItemTemplate>[] filteredItems;
            public float width;
        }
        
        public readonly struct ItemLootMetadata {
            public readonly AssetData<ItemTemplate> template;

            public ItemLootMetadata(ItemLootData data) {
                template = GetAssetData(data.Template);
            }
        }
    }
}