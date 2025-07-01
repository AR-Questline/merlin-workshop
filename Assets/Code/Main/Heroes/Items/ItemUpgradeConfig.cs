using System;
using System.Collections.Generic;
using Awaken.TG.Main.Locations.Gems;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items {
    [CreateAssetMenu(fileName = "ItemUpgradeConfig", menuName = "ItemUpgradeConfig")]
    public class ItemUpgradeConfig : ScriptableObject {
        [field: SerializeField] public MoneyCostType CostType { get; private set; }
        [field: SerializeField] public MoneyCostType CobwebCostType { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: SerializeField, ShowIf("@CostType == MoneyCostType.Own")] public AnimationCurve MoneyPerLevel { get; private set; }
        [field: SerializeField, ShowIf("@CobwebCostType == MoneyCostType.Own")] public AnimationCurve CobwebPerLevel { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: SerializeField] public List<IngredientPerLevel> IngredientsPerLevel { get; private set; }
    }

    public class ItemUpgradeConfigData {
        public AnimationCurve MoneyPerLevel { get; }
        public AnimationCurve CobwebPerLevel { get; }
        public List<IngredientPerLevel> IngredientsPerLevel { get; }
        
        public ItemUpgradeConfigData(List<IngredientPerLevel> ingredientsPerLevel, AnimationCurve moneyPerLevel = null, AnimationCurve cobwebPerLevel = null) {
            this.IngredientsPerLevel = ingredientsPerLevel;
            MoneyPerLevel = moneyPerLevel;
            CobwebPerLevel = cobwebPerLevel;
        }

        public int GetPrice(CurrencyType currencyType, int level) {
            if (currencyType == CurrencyType.Cobweb) {
                return level >= 0 && CobwebPerLevel != null ? (int)CobwebPerLevel.Evaluate(level) : 0;
            } else {
                return level >= 0 && MoneyPerLevel != null ? (int)MoneyPerLevel.Evaluate(level) : 0;
            }
        }
        
        public IEnumerable<CountedItem> GetIngredients(int level) => GetCountedItems(level);

        IEnumerable<CountedItem> GetCountedItems(int level) {
            foreach (var ingredient in IngredientsPerLevel) {
                yield return new CountedItem(ingredient.Item.Get<ItemTemplate>(), (int)ingredient.QuantityPerLevel.Evaluate(level));
            }
        }
    }

    [Serializable]
    public struct IngredientPerLevel {
        [field: SerializeField] public IngredientType IngredientType { get; private set; }
        [field: SerializeField, TemplateType(typeof(ItemTemplate))] public TemplateReference Item { get; private set; }
        [field: SerializeField] public AnimationCurve QuantityPerLevel { get; private set; }
    }

    [Serializable]
    public enum IngredientType : byte {
        [UnityEngine.Scripting.Preserve] Upgrade = 0,
        [UnityEngine.Scripting.Preserve] Tier = 1,
        [UnityEngine.Scripting.Preserve] Flavour = 2
    }

    [Serializable]
    public enum MoneyCostType : byte {
        None = 0,
        Own = 1,
        Inherited = 2
    }

    /// <summary>
    /// Has properties
    /// <see cref="itemTemplate"/>
    /// <see cref="quantity"/>
    /// </summary>
    public readonly struct CountedItem {
        public readonly ItemTemplate itemTemplate;
        public readonly int quantity;

        public CountedItem(ItemTemplate itemTemplate, int quantity) {
            this.itemTemplate = itemTemplate;
            this.quantity = quantity;
        }
        
        public void Deconstruct(out ItemTemplate itemTemplate, out int quantity) {
            itemTemplate = this.itemTemplate;
            quantity = this.quantity;
        }
    }
}