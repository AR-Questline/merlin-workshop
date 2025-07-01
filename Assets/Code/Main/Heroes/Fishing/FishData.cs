using Awaken.TG.Code.Utility;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using System;
using Awaken.TG.Main.General.StatTypes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Fishing {
    [CreateAssetMenu(fileName = "FishData", menuName = "Scriptable Objects/FishData")]
    public class FishData : ScriptableObject {
        [SerializeField, ShowIf(nameof(IsFish))] FishQuality quality;
        [SerializeField] FishingPrizeType fishingPrizeType;
        [SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference fish;
        [SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference item;
        [SerializeField, ShowIf(nameof(IsFish))] FloatRange weight;
        [SerializeField, ShowIf(nameof(IsFish))] FloatRange length;
        [SerializeField, ShowIf(nameof(IsFish))] FloatRange health;
        [SerializeField, ShowIf(nameof(IsFish))] FloatRange damage;
        [SerializeField] IntRange amountOfItems;
        [SerializeField, ShowIf(nameof(IsFish)), Range(0.1f, 5f)] float changeDestinationInterval = 1f;
        [SerializeField, ShowIf(nameof(IsFish))] float speed;
        [SerializeField, ShowIf(nameof(IsFish))] float optimalRange;
        [SerializeField] float probability;
        public float Probability => probability;
        public bool IsFish => fishingPrizeType == FishingPrizeType.Fish;
        
        public FightingFish ToFightingFish() => fishingPrizeType switch {
            FishingPrizeType.Fish => GetFightingFish(),
            FishingPrizeType.Item => FightingFish.Item(item),
            _ => default
        };

        FightingFish GetFightingFish() {
            float mean = (weight.min + weight.max) / 2;
            float finalMean = mean * Hero.Current.Stat(HeroStatType.FishingMeanMultiplier);
            float sigma = (weight.max - mean) / 3;
            float invCdf = RandomUtil.NormalDistribution(finalMean, sigma, false);
            float fishWeight = Mathf.Clamp(invCdf, weight.min, weight.max);
            float lerpWeight = fishWeight / weight.max;
            float fishLength = Mathf.Lerp(length.min, length.max, lerpWeight);
            float fishHealth = Mathf.Lerp(health.min, health.max, lerpWeight);
            float fishDamage = Mathf.Lerp(damage.min, damage.max, lerpWeight);
            int fishAmountOfItems = (int)Mathf.Lerp(amountOfItems.low, amountOfItems.high, lerpWeight);

            return FightingFish.Fish(fish, item, fishWeight, fishAmountOfItems, quality, fishLength, fishHealth, fishDamage, changeDestinationInterval, speed, optimalRange);
        }
        
        enum FishingPrizeType : byte {
            Fish,
            Item,
        }

        [Serializable]
        public struct FightingFish {
            readonly TemplateReference _fishTemplate;
            readonly TemplateReference _itemTemplate;
            public readonly float weight;
            public readonly int itemsCount;
            public readonly bool isFish;
            public readonly FishQuality quality;
            public readonly float length;
            public float health;
            public readonly float damage;
            public readonly float changeDestinationInterval;
            public readonly float speed;
            public readonly float optimalRange;

            public ItemTemplate FishTemplate => _fishTemplate.Get<ItemTemplate>();
            public ItemTemplate ItemTemplate => _itemTemplate.Get<ItemTemplate>();

            FightingFish(TemplateReference fishTemplate, TemplateReference itemTemplate, float weight, int itemsCount, FishQuality quality = FishQuality.Garbage, bool isFish = false, 
                float length = 0, float health = 0, float damage = 0, float changeDestinationInterval = 0, float speed = 0, float optimalRange = 0) {
                this._fishTemplate = fishTemplate;
                this._itemTemplate = itemTemplate;
                this.weight = weight;
                this.itemsCount = itemsCount;
                this.isFish = isFish;
                this.quality = quality;
                this.length = length;
                this.health = health;
                this.damage = damage;
                this.changeDestinationInterval = changeDestinationInterval;
                this.speed = speed;
                this.optimalRange = optimalRange;
            }

            public static FightingFish Fish(TemplateReference fishTemplate, TemplateReference itemTemplate, float weight, int itemsCount, FishQuality quality, float length, float health, float damage, float changeDestinationInterval, float speed, float optimalRange) {
                return new FightingFish(fishTemplate, itemTemplate, weight, itemsCount, quality, true, length, health, damage, changeDestinationInterval, speed, optimalRange);
            }

            public static FightingFish Item(TemplateReference itemTemplate) {
                return new FightingFish(itemTemplate, itemTemplate, itemTemplate.Get<ItemTemplate>().Weight, 1);
            }

            public Item CreateItem() {
                return new Item(ItemTemplate, itemsCount);
            }
        }
    }
}
