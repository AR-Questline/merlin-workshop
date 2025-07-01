using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterStats;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Observers;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.EntryInfo {
    [SpawnsView(typeof(VCharacterStatsEntryInfoUI))]
    public partial class CharacterStatsEntryInfoUI : Element<StatsEntryUI> {
        public sealed override bool IsNotSaved => true;

        public List<StatEntryInfoData> StatEntries { get; } = new();

        readonly List<StatType> _prohibitedStatTypes = new() {
            ItemStatType.HoldItemCostPerTick,
            ItemStatType.DrawBowCostPerTick
        };
        
        public CharacterStatsEntryInfoUI(HeroRPGStatType statType) {
            foreach (var effect in GameConstants.Get.rpgHeroStats.Where(x => x.RPGStat == statType).SelectMany(x => x.Effects)) {
                if (_prohibitedStatTypes.Contains(effect.StatEffected)) continue;
                string name, value;

                if (effect.StatEffected == HeroStatType.AlchemyLevelBonus || effect.StatEffected == HeroStatType.CookingLevelBonus || effect.StatEffected == HeroStatType.EquipmentLevelBonus) {
                    name = LocTerms.CraftingLevelBonus.Translate();
                } else if (effect.StatEffected == HeroStatType.NoiseMultiplier || effect.StatEffected == HeroStatType.VisibilityMultiplier) {
                    name = LocTerms.NoiseAndVisibilityMultiplier.Translate();
                }else if (effect.StatEffected == CharacterStatType.StaminaUsageMultiplier || effect.StatEffected == CharacterStatType.ManaUsageMultiplier) {
                    name = LocTerms.StaminaAndManaUsage.Translate();
                } else {
                    name = effect.StatEffected.DisplayName;
                }
                
                value = GetStatValue(effect).ToString(CultureInfo.InvariantCulture);
                
                if (StatEntries.All(x => x.Name != name)){
                    StatEntries.Add(new StatEntryInfoData(name, value));
                }
            }
        }
        
        static string GetStatValue(StatEffect effect) {
            string valueDescription;

            if (effect.StatEffected == CharacterStatType.ManaRegen || effect.StatEffected == AliveStatType.HealthRegen || effect.StatEffected == CharacterStatType.StaminaRegen) {
                valueDescription = $"+{effect.BaseEffectStrength:F} / {LocTerms.SecondsAbbreviation.Translate()}"; 
            } else if (effect.StatEffected == HeroStatType.AlchemyLevelBonus || effect.StatEffected == HeroStatType.CookingLevelBonus || effect.StatEffected == HeroStatType.EquipmentLevelBonus) {
                valueDescription = CreateDefaultDescription(effect, "F");
            } else if (effect.StatEffected == CharacterStatType.MaxStamina || effect.StatEffected == AliveStatType.MaxHealth || effect.StatEffected == HeroStatType.EncumbranceLimit || effect.StatEffected == CharacterStatType.MaxMana) {
                valueDescription = CreateDefaultDescription(effect, "F0");
            } else {
                valueDescription = CreateDefaultDescription(effect, "P0");
            }
            
            return valueDescription;
        }
        
        static string CreateDefaultDescription(StatEffect effect, string format) {
            string valuePrefix = effect.BaseEffectStrength > 0 ? "+" : string.Empty;
            return $"{valuePrefix}{effect.BaseEffectStrength.ToString(format)}";
        }
    }

    public class StatEntryInfoData {
        public string Name { get; }
        public string Value { get; }

        public StatEntryInfoData(string name, string value) {
            Name = name;
            Value = value;
        }
    }
}
