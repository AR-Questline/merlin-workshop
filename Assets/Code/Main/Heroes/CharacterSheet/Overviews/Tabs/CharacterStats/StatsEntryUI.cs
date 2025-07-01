using Awaken.TG.Assets;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterStats {
    [SpawnsView(typeof(VStatsEntryUI))]
    public partial class StatsEntryUI : Element<CharacterStatsUI> {
        public sealed override bool IsNotSaved => true;

        public readonly Stat heroRPGStat;
        public readonly ShareableSpriteReference icon;

        public int StatChangeValue { get; private set; }
        public bool CanIncrease => ParentModel.AvailablePoints > 0 && TalentTree.IsUpgradeAvailable;
        public bool CanDecrease => StatChangeValue > 0;

        public StatsEntryUI(Stat rpgStat) {
            heroRPGStat = rpgStat;
            var statType = (HeroRPGStatType)heroRPGStat.Type;
            icon = statType.icon?.Invoke();
        }

        public void IncreaseStatValue() {
            if (CanIncrease == false) return;

            ParentModel.AvailablePoints.DecreaseBy(1);
            StatChangeValue += 1;
            ParentModel.Trigger(CharacterStatsUI.Events.PointsToApplyChange, new StatPointsChange(ParentModel.HasUnsavedChanges, heroRPGStat, 1));
            TriggerChange();
        }

        public void DecreaseStatValue() {
            if (CanDecrease == false) return;

            ParentModel.AvailablePoints.IncreaseBy(1);
            StatChangeValue -= 1;
            ParentModel.Trigger(CharacterStatsUI.Events.PointsToApplyChange, new StatPointsChange(ParentModel.HasUnsavedChanges, heroRPGStat, -1));
            TriggerChange();
        }
        
        public void ResetStatValue() {
            if (CanDecrease == false) return;
            
            ParentModel.AvailablePoints.IncreaseBy(StatChangeValue);
            ParentModel.Trigger(CharacterStatsUI.Events.PointsToApplyChange, new StatPointsChange(ParentModel.HasUnsavedChanges, heroRPGStat, StatChangeValue));
            StatChangeValue = 0;
            TriggerChange();
        }

        public void ApplyStatChange() {
            heroRPGStat.IncreaseBy(StatChangeValue);
            StatChangeValue = 0;
        }
    }
}