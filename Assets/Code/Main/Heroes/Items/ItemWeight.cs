using System;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.Items {
    public class ItemWeight : RichEnum {
        public LocString DisplayName { get; }
        readonly Func<Hero, float> _dashDurationMultiplier;
        readonly ProfStatType _linkedProficiency;

        public static readonly ItemWeight
            None = new(nameof(None), LocTerms.None, h => h.Data.dashDurationLightArmor),
            VeryLight = new(nameof(VeryLight), LocTerms.WeightVeryLight, h => h.Data.dashDurationLightArmor),
            Light = new(nameof(Light), LocTerms.WeightLight, h => h.Data.dashDurationLightArmor, ProfStatType.LightArmor),
            Medium = new(nameof(Medium), LocTerms.WeightMedium, h => h.Data.dashDurationMediumArmor, ProfStatType.MediumArmor),
            Heavy = new(nameof(Heavy), LocTerms.WeightHeavy, h => h.Data.dashDurationHeavyArmor, ProfStatType.HeavyArmor),
            VeryHeavy = new(nameof(VeryHeavy), LocTerms.WeightVeryHeavy, h => h.Data.dashDurationHeavyArmor),
            Overload = new(nameof(Overload), LocTerms.WeightOverload, h => h.Data.dashDurationOverload);

        ItemWeight(string enumName, string displayName, Func<Hero, float> dashDurationMultiplier, ProfStatType linkedProficiency = null) : base(enumName) {
            DisplayName = new LocString { ID = displayName };
            _dashDurationMultiplier = dashDurationMultiplier;
            _linkedProficiency = linkedProficiency;
        }

        public static ItemWeight FromArmorScore(float score) {
            if (score >= GameConstants.Get.HeavyArmorThreshold) return ItemWeight.Overload;
            if (score >= GameConstants.Get.MediumArmorThreshold) return ItemWeight.Heavy;
            if (score >= GameConstants.Get.LightArmorThreshold) return ItemWeight.Medium;
            return ItemWeight.Light;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static readonly ItemWeight[] All = {
            None, VeryLight, Light, Medium, Heavy, VeryHeavy, Overload
        };
        
        public static readonly ItemWeight[] ThreshHoldWeightsInOrder = {
            VeryLight, Light, Medium, Heavy, Overload
        };
        
        public static ItemWeight LighterArmorType(ItemWeight current) {
            for (int i = 0; i < ThreshHoldWeightsInOrder.Length; i++) {
                if (ThreshHoldWeightsInOrder[i] == current) {
                    return i > 0 ? ThreshHoldWeightsInOrder[i - 1] : ThreshHoldWeightsInOrder[i];
                }
            }
            throw new ArgumentOutOfRangeException(nameof(current), current, "Current weight not found in threshold weights");
        }
        
        [UnityEngine.Scripting.Preserve] public float DashDurationMultiplier(Hero hero) => _dashDurationMultiplier(hero);
        public Stat LinkedProficiency(Hero hero) => _linkedProficiency == null ? null : hero.Stat(_linkedProficiency);
    }
}