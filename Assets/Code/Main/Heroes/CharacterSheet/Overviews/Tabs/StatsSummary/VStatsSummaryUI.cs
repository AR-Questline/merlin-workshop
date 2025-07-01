using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterStats;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.StatsSummary {
    [UsesPrefab("CharacterSheet/Stats/" + nameof(VStatsSummaryUI))]
    public class VStatsSummaryUI : View<StatsSummaryUI> {
        [SerializeField] VCStatsSummaryEntryUI armor;
        [SerializeField] VCStatsSummaryEntryUI weakSpotDamage;
        [SerializeField] VCStatsSummaryEntryUI lifesteal;

        public override Transform DetermineHost() => Target.ParentModel.View<VCharacterStatsUI>().StatsSummaryParent;
        static Hero Hero => Hero.Current;
        
        Dictionary<StatType, VCRefreshableStatsSummaryEntryUI> _refreshableStats = new();
        
        protected override void OnInitialize() {
            Target.ParentModel.ListenTo(CharacterStatsUI.Events.NewCharacterStatsApplied, RefreshAll, this);
            Target.ParentModel.ListenTo(CharacterStatsUI.Events.PointsToApplyChange, (pointsChange) => PredictDependentStat(pointsChange.rpgHeroStat, pointsChange.count), this);

            RegisterStats();
        }
        
        void RegisterStats() {
            SetupRefreshableStats();
            SetupDisplayOnlyStats();
            
            foreach (var stat in GetComponentsInChildren<VCStatsSummaryEntryUI>()) {
                stat.Refresh();
            }
        }
        
        void SetupRefreshableStats() {
            _refreshableStats.Clear();
            
            foreach (var stat in GetComponentsInChildren<VCRefreshableStatsSummaryEntryUI>()) {
                if (stat.MainStatType != null) {
                    _refreshableStats.Add(stat.MainStatType, stat);
                    stat.PrepareDependentStats();
                }
            }
            
            _refreshableStats[HeroStatType.CriticalDamageMultiplier].Override(() => 1 + Hero.Stat(HeroStatType.CriticalDamageMultiplier).ModifiedValue);
            _refreshableStats[CharacterStatType.RangedDamageMultiplier].Override(() => Hero.Stat(CharacterStatType.RangedDamageMultiplier).ModifiedValue + Hero.Stat(CharacterStatType.Strength).ModifiedValue - 1);
            _refreshableStats[CharacterStatType.MeleeDamageMultiplier].Override(() => Hero.Stat(CharacterStatType.MeleeDamageMultiplier).ModifiedValue + Hero.Stat(CharacterStatType.Strength).ModifiedValue - 1);
            _refreshableStats[HeroStatType.MeleeCriticalChance].Override(() => Hero.Stat(HeroStatType.MeleeCriticalChance).ModifiedValue + Hero.Stat(HeroStatType.CriticalChance).ModifiedValue);
            _refreshableStats[HeroStatType.MagicCriticalChance].Override(() => Hero.Stat(HeroStatType.MagicCriticalChance).ModifiedValue + Hero.Stat(HeroStatType.CriticalChance).ModifiedValue);
            _refreshableStats[HeroStatType.RangedCriticalChance].Override(() => Hero.Stat(HeroStatType.RangedCriticalChance).ModifiedValue + Hero.Stat(HeroStatType.CriticalChance).ModifiedValue);
            _refreshableStats[HeroStatType.SneakDamageMultiplier].Override(() => 1 + Hero.Stat(HeroStatType.SneakDamageMultiplier).ModifiedValue);

            foreach (var effect in GameConstants.Get.rpgHeroStats.SelectMany(statParams => statParams.Effects)) {
                float effectValue = effect.EffectType == OperationType.Multi ? effect.BaseEffectStrength / 100 : effect.BaseEffectStrength;

                if (_refreshableStats.TryGetValue(effect.StatEffected, out var summaryEntryUI)) {
                    summaryEntryUI.Setup(effectValue);
                    continue;
                }

                var dependentStats = _refreshableStats.Values.Where(refreshable => refreshable.DependentStats.Any(depStat => depStat == effect.StatEffected));
                foreach (var refreshable in dependentStats) {
                    refreshable.Setup(effectValue);
                }
            }
        }

        void SetupDisplayOnlyStats() {
            armor.Override(() => Hero.ArmorValue());
            lifesteal.Override(() => Hero.Stat(CharacterStatType.LifeSteal));
            weakSpotDamage.Override(() => 1 + Hero.Stat(HeroStatType.WeakSpotDamageMultiplier).ModifiedValue);
        }
        
        void PredictDependentStat(Stat rpgStat, int count) {
            foreach (var effect in GameConstants.Get.rpgHeroStats.Where(statParams => statParams.RPGStat == rpgStat.Type).SelectMany(statParams => statParams.Effects)) {
                // handle main stats
                if (_refreshableStats.TryGetValue(effect.StatEffected, out var summaryEntryUI)) {
                    summaryEntryUI.PredictApplyValue(count);
                    continue;
                }

                // handle dependent stats
                var dependentStats = _refreshableStats.Values.Where(refreshable => refreshable.DependentStats.Any(depStat => depStat == effect.StatEffected));
                foreach (var refreshable in dependentStats) {
                    refreshable.PredictApplyValue(count);
                }
            }
        }
        
        void RefreshAll() {
            _refreshableStats.Values.ForEach(entryUI => entryUI.Refresh());
        }
    }
}