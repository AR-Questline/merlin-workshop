using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Heroes.Combat {
    public static class MagicUtils {
        public static float GetModifiedManaCost(ICharacter character, Item item, out float baseManaCost, out bool costPerSecond) {
            costPerSecond = false;
            
            baseManaCost = item?.Stat(ItemStatType.LightCastManaCost) ?? 0f;
            if (baseManaCost == 0f) {
                // TODO this probably needs to be reworked with new magic UI?
                baseManaCost = item?.Stat(ItemStatType.HeavyCastManaCostPerSecond) ?? 0f;
                if (baseManaCost != 0f) {
                    costPerSecond = true;
                }
            }
            return baseManaCost * GetManaCostMultiplier(character, item);
        }

        public static float GetManaCostMultiplier(ICharacter character, Item item) {
            float multiplier = character?.Stat(CharacterStatType.ManaUsageMultiplier)?.ModifiedValue ?? 1f;
            multiplier *= ItemRequirementsUtils.GetManaCostMultiplier(character, item);
            return multiplier;
        }
        
        // === VS
        public static float GetLightManaCost(this ISkillUnit unit, Flow flow) {
            return unit.GetManaCost(flow, ItemStatType.LightCastManaCost);
        }
        
        public static float GetHeavyManaCost(this ISkillUnit unit, Flow flow) {
            return unit.GetManaCost(flow, ItemStatType.HeavyCastManaCost);
        }
        
        public static float GetHeavyManaCostPerSecond(this ISkillUnit unit, Flow flow) {
            return unit.GetManaCost(flow, ItemStatType.HeavyCastManaCostPerSecond);
        }
        
        static float GetManaCost(this ISkillUnit unit, Flow flow, ItemStatType itemStatType) {
            Skill skill = unit.Skill(flow);
            Item item = skill.SourceItem;
            float manaCost = item?.Stat(itemStatType) ?? 0f;
            return manaCost * GetManaCostMultiplier(skill.Owner, item);
        }
        
        public static bool GetAnySummonsOwned(this ISkillUnit unit, Flow flow) {
            return GetOwnedSummonsForSkillItemCount(unit, flow) > 0;
        }

        public static int GetOwnedSummonsForSkillItemCount(this ISkillUnit unit, Flow flow) {
            var item = unit.Skill(flow).SourceItem;
            var count = 0;
            foreach (var summon in World.All<NpcHeroSummon>()) {
                if (summon.Item == item) {
                    count++;
                }
            }
            return count;
        }
        
        public static int GetOwnedSummonsCount(this ISkillUnit unit, Flow flow) => (int) World.All<NpcHeroSummon>().Count();
    }
}