using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.BalanceTool.Data;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Observers;
using Awaken.TG.Main.Heroes.Stats.Utils;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Editor.BalanceTool {
    public static class BalanceToolCalculator {
        static CommonReferences CommonReferences => CommonReferences.Get;
        
        public static (float value, string formula) ComputeAvgDmg(ItemStatsAttachment itemStats, BalanceToolData data, CharacterStats.ITemplate characterStats) {
            if (itemStats == null) {
                Log.Minor?.Error("ItemStatsAttachment is null");
                return (-1, string.Empty);
            }
            
            if (characterStats == null) {
                Log.Minor?.Error("CharacterStats.ITemplate is null");
                return (-1, string.Empty);
            }
            
            if (TryGetProficiency(itemStats, out ProfStatType proficiency) == false) {
                return (-1, string.Empty);
            }

            float proficiencyModifier = ProficiencyModifier(proficiency, proficiency.MultiStat, data);
            float strengthModifier = 0;
            
            if (itemStats.IsMelee) {
                float meleeModifier = GameConstants.Get.RPGStatParamsByType[HeroRPGStatType.Strength].Effects
                    .First(e => e.StatEffected == CharacterStatType.MeleeDamageMultiplier).BaseEffectStrength;
                strengthModifier = data[HeroRPGStatType.Strength].effective * meleeModifier;
            } else if (itemStats.IsRanged) {
                float rangedModifier = GameConstants.Get.RPGStatParamsByType[HeroRPGStatType.Dexterity].Effects
                    .First(e => e.StatEffected == CharacterStatType.RangedDamageMultiplier).BaseEffectStrength;
                strengthModifier = data[HeroRPGStatType.Dexterity].effective * rangedModifier;
            }
            
            float linearStatModifier = proficiency.UseStrength ? characterStats.StrengthLinear : 0f;

            float minDamage = Multiplier(itemStats.minDamage, proficiencyModifier + strengthModifier) + linearStatModifier;
            float maxDamage = Multiplier(itemStats.maxDamage, proficiencyModifier + strengthModifier) + linearStatModifier;
            float avgDamage = (minDamage + maxDamage) * 0.5f;
            
            string formula = $"(Weapon Avg Dmg {ComputeAvgWeaponDmg(itemStats).value} + Linear Stat Modifier {linearStatModifier}) * (Proficiency Modifier {proficiencyModifier} + Strength Modifier {strengthModifier})";
            return (avgDamage + data[ModifiersStatEntryEnum.AdditionalDamage].effective, formula);
        }

        public static (float value, string formula) ComputeAvgWeaponDmg(ItemStatsAttachment itemStats) {
            float avgDmg = itemStats ? (itemStats.minDamage + itemStats.maxDamage) * 0.5f : 0;
            string formula = $"(Min Dmg {itemStats.minDamage} + Max Dmg {itemStats.maxDamage}) * 0.5";
            return (avgDmg, formula);
        }
        
        public static (float value, string formula) ComputeAvgWeaponStaminaCost(ItemStatsAttachment itemStats) {
            float avgStamina = itemStats ? (itemStats.lightAttackStaminaCost + itemStats.heavyAttackStaminaCost) * 0.5f : 0;
            string formula = $"(Light Attack SP {itemStats.lightAttackStaminaCost} + Heavy Attack SP {itemStats.heavyAttackStaminaCost}) * 0.5";
            return (avgStamina, formula);
        }
        
        public static float ComputeLightStaminaCost(ItemStatsAttachment itemStats, BalanceToolData data) {
            var item = itemStats.GetComponent<ItemTemplate>();
            float avgStaminaCost = 0;

            if (item.IsRanged) {
                avgStaminaCost = itemStats.drawBowStaminaCostPerTick;
            } else if (item.IsMelee) {
                avgStaminaCost = itemStats.lightAttackStaminaCost;
            }
            
            return ComputeStaminaCost(data, avgStaminaCost);
        }
        
        public static float ComputeHeavyStaminaCost(ItemStatsAttachment itemStats, BalanceToolData data) {
            var item = itemStats.GetComponent<ItemTemplate>();
            float avgStaminaCost = 0;

            if (item.IsRanged) {
                avgStaminaCost = itemStats.drawBowStaminaCostPerTick;
            } else if (item.IsMelee) {
                avgStaminaCost = itemStats.heavyAttackStaminaCost;
            }
            
            return ComputeStaminaCost(data, avgStaminaCost);
        }
        
        static float ComputeStaminaCost(BalanceToolData data, float staminaCost) {
            GameConstants.Get.rpgHeroStats.ForEach(stat => stat.Effects.ForEach(effect => {
                if (effect.StatEffected == CharacterStatType.StaminaUsageMultiplier) {
                    staminaCost = Multiplier(staminaCost, data[stat.RPGStat].effective * effect.BaseEffectStrength);
                }
            }));

            return staminaCost;
        }
        
        public static float ComputeEffectiveStamina(float stamina) {
            return stamina;
        }
        
        public static (float value, string formula) ComputeEffectiveHP(float hp, float dmgReduction) {
            float effectiveHp = hp / (1 - dmgReduction);
            string formula = $"HP {hp} / (1 - Dmg Reduction {dmgReduction})";
            return (effectiveHp, formula);
        }
        
        public static float ComputeEqArmor(BalanceToolData data) {
            var stats = data.playerEquipment.Values.Select(eq => eq.stats).WhereNotNull().ToList();
            
            return stats.Sum(armor => {
                float proficiencyModifier = ProficiencyModifier(armor, ItemStatType.ItemArmor, data);
                return Multiplier(armor.armor, proficiencyModifier);
            }) + data[ModifiersStatEntryEnum.AdditionalArmor].effective;
        }

        /// <summary>
        /// e.g.
        /// we have 5 base stats at start value = 5 and one stat for example strength = 10 <br/>
        /// level = sum(stat value - stat start value)  + 1 <br/> 
        /// level = ((10 + 5 * 5) - (6 * 5)) + 1 = (35) - (30) + 1 = 6
        /// </summary>
        public static int ComputePlayerLevel(IEnumerable<StatEntry> baseStats) {
            return baseStats.Sum(x => ComputeStatLevel(x) - (int)x.BaseValue) + 1;
        }
        
        public static int ComputeStatLevel(StatEntry stat) {
            return (int)stat.effective;
        }

        static float Multiplier(float value, params float[] multipliers) => multipliers.Aggregate(value, (current, multi) => current * (1 + multi));

        static float ProficiencyModifier(ProfStatType proficiency, StatType statType, BalanceToolData data) {
            ProficiencyParams proficiencyParams = ProfUtils.GetProfParams(proficiency);
            return data[proficiency].effective * proficiencyParams.GetEffectStrOfType(statType);
        }
       
        static float ProficiencyModifier(ItemStatsAttachment itemStats, StatType statType, BalanceToolData data) {
            if (TryGetProficiency(itemStats, out ProfStatType proficiency) == false) {
                return 0;
            }
           
            ProficiencyParams proficiencyParams = ProfUtils.GetProfParams(proficiency);
            return data[proficiency].effective * proficiencyParams.GetEffectStrOfType(statType);
        }
        
        static bool TryGetProficiency(ItemStatsAttachment itemStats, out ProfStatType proficiency) {
            ItemTemplate item = itemStats.GetComponent<ItemTemplate>();
            proficiency = ProfUtils.ProfFromAbstracts(item, CommonReferences.ProficiencyAbstractRefs);
            
            if (proficiency == null) {
                Log.Minor?.Error($"No proficiency for item {item.itemName}");
                return false;
            }

            return true;
        }
    }
}
