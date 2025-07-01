using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Tags;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Utils {
    public class HealingUtils {
        public static void TakeHealing(ICharacter character, float healing, Item healingItem = null) {
            // apply incoming healing stat
            healing *= CalculateHealingModifier(character, healingItem);
            if (healing <= 0) {
                return;
            }
            character.Health.IncreaseBy(healing);
        }
        
        public static float CalculateHealingModifier(ICharacter character, Item healingItem) {
            float healingModifier = character.CharacterStats.IncomingHealing;
            if (healingItem != null) {
                var tags = healingItem.Tags;
                if (TagUtils.HasRequiredTag(tags, "type:consumable")) {
                    healingModifier += character.Stat(CharacterStatType.ConsumableHealingBonus).ModifiedValue - 1f;
                }
                if (healingItem.IsPotion) {
                    healingModifier += character.Stat(CharacterStatType.PotionHealingBonus).ModifiedValue - 1f;
                }
            }

            return healingModifier;
        }
    }
}