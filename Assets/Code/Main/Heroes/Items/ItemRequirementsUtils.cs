using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items.Weapons;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items {
    public static class ItemRequirementsUtils {
        const float MinMultiplierValue = 0.05f;
        
        public static float GetDamageMultiplier(ICharacter owner, Item item) {
            ItemStatsRequirements statsRequirements = item?.StatsRequirements;
            
            if (statsRequirements == null || owner is not Hero) {
                return 1f;
            }
            
            int missingPoints = statsRequirements.MissingRequirementPoints;
            float multiplier = missingPoints > 0 ? 1 - (0.1f + missingPoints * GameConstants.Get.DamageDecreasePerMissingPoint) : 1;
            return Mathf.Max(MinMultiplierValue, multiplier);
        }
        
        public static float GetManaCostMultiplier(ICharacter owner, Item item) {
            ItemStatsRequirements statsRequirements = item?.StatsRequirements;

            if (statsRequirements == null || owner is not Hero) {
                return 1f;
            }

            int missingPoints = statsRequirements.MissingRequirementPoints;
            return missingPoints > 0 ?  1 + (0.1f + missingPoints * GameConstants.Get.ManaCostIncreasePerMissingPoint) : 1;
        }

        public static float GetBlockDamageReductionMultiplier(ICharacter owner, Item item) {
            ItemStatsRequirements statsRequirements = item?.StatsRequirements;

            if (statsRequirements == null || owner is not Hero) {
                return 1f;
            }

            float multiplier = 1 - (statsRequirements.MissingRequirementPoints * GameConstants.Get.BlockDamageReductionPerMissingPoint);
            return Mathf.Max(MinMultiplierValue, multiplier);
        }

        public static float GetArmorAfterReduction(ICharacter owner, Item item) {
            float armor = item?.ItemStats?.Armor.ModifiedValue ?? 0;
            return armor * GetArmorReductionMultiplier(owner, item);
        }

        public static float GetArmorReductionMultiplier(ICharacter owner, Item item) {
            ItemStatsRequirements statsRequirements = item?.StatsRequirements;

            if (statsRequirements == null || owner is not Hero) {
                return 1f;
            }
            
            int missingPoints = statsRequirements.MissingRequirementPoints;
            float multiplier = missingPoints > 0 ? 1 - (0.1f + missingPoints * GameConstants.Get.ArmorReductionPerMissingPoint) : 1f;
            return Mathf.Max(MinMultiplierValue, multiplier);
        }
    }
}