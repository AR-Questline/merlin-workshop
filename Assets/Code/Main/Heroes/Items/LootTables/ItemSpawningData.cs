using Awaken.Utility;
using System;
using System.Collections.Generic;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [System.Serializable]
    public partial class ItemSpawningData : ILootTable, IEquatable<ItemSpawningData> {
        public ushort TypeForSerialization => SavedTypes.ItemSpawningData;

        [TemplateType(typeof(ItemTemplate))]
        [Saved] public TemplateReference itemTemplateReference;
        [Saved(1)] public int quantity = 1;
        [Saved] public IntRange itemLvl;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public ItemSpawningData() { }

        public ItemSpawningData(TemplateReference itemTemplateReference, int quantity = 1) {
            this.itemTemplateReference = itemTemplateReference;
            this.quantity = quantity;
        }

        public int ItemLvl => itemLvl.RandomPick();

        public ItemTemplate ItemTemplate(object debugTarget) => itemTemplateReference.Get<ItemTemplate>(debugTarget);
        public LootTableResult PopLoot(object debugTarget) {
            return new LootTableResult(new[] { ToRuntimeData(debugTarget) });
        }

        public ItemSpawningDataRuntime ToRuntimeData(object debugTarget) {
            ItemTemplate itemTemplate = ItemTemplate(debugTarget);
            int templateLvl = itemTemplate != null ? itemTemplate.LevelBonus : 0;
            return new ItemSpawningDataRuntime(itemTemplate, quantity, itemLvl is {high: 0, low: 0} ? templateLvl : ItemLvl);
        }

        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            yield return new ItemLootData(itemTemplateReference, quantity);
        }

        public bool Equals(ItemSpawningData other) {
            if (other is null) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return Equals(itemTemplateReference, other.itemTemplateReference) && quantity == other.quantity && itemLvl.Equals(other.itemLvl);
        }
        public override bool Equals(object obj) {
            if (obj is null) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return Equals((ItemSpawningData)obj);
        }
        public override int GetHashCode() {
            unchecked {
                int hashCode = (itemTemplateReference != null ? itemTemplateReference.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ quantity;
                hashCode = (hashCode * 397) ^ itemLvl.GetHashCode();
                return hashCode;
            }
        }
        public static bool operator ==(ItemSpawningData left, ItemSpawningData right) {
            return Equals(left, right);
        }
        public static bool operator !=(ItemSpawningData left, ItemSpawningData right) {
            return !Equals(left, right);
        }
    }
}