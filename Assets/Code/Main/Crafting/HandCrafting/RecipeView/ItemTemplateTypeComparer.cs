using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items;

namespace Awaken.TG.Main.Crafting.HandCrafting.RecipeView {
    public class ItemTemplateTypeComparer : IComparer<ItemTemplate> {
        public static readonly ItemTemplateTypeComparer Comparer = new();
        public int Compare(ItemTemplate x, ItemTemplate y) {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int xScore = AbstractTypeScore(x);
            int yScore = AbstractTypeScore(y);

            return xScore - yScore;
        }

        static int AbstractTypeScore(ItemTemplate item) {
            return item switch {
                {IsRanged: true} => 3,
                {IsOneHanded: true} => 1,
                {IsTwoHanded: true} => 2,
                {IsArrow: true} => 4,
                {IsArmor: true} => Armor(item, 10),
                {IsConsumable: true} => Consumable(item, 100),
                {IsComponent: true} => 200,
                {IsCrafting: true} => 201,
                {IsJewelry: true} => 202,
                _ => 0
            };
        }

        static int Armor(ItemTemplate item, int offset) {
            return item switch {
                {IsLightArmor: true} => offset + 0,
                {IsMediumArmor: true} => offset + 1,
                {IsHeavyArmor: true} => offset + 2,
                _ => offset + 9,
            };
        }

        static int Consumable(ItemTemplate item, int offset) {
            return item switch {
                {ConsumableModifiesHealth: true} => offset + 0,
                {ConsumableModifiesMana: true} => offset + 1,
                {ConsumableStamina: true} => offset + 2,
                _ => offset + 9,
            };
        }
    }
}