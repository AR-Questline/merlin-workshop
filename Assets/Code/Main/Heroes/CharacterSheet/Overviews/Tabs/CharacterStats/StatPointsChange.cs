using Awaken.TG.Main.Heroes.Stats;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterStats {
    public struct StatPointsChange {
        public bool hasPointsToApply;
        public Stat rpgHeroStat;
        public int count;
        
        public StatPointsChange(bool hasPointsToApply, Stat rpgHeroStat, int count) {
            this.hasPointsToApply = hasPointsToApply;
            this.rpgHeroStat = rpgHeroStat;
            this.count = count;
        }
    }
}
