using Awaken.TG.Main.General.Costs;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Skills.Passives {
    public partial class PassiveCostModifier : Element<Skill>, IPassiveEffect {
        public sealed override bool IsNotSaved => true;

        float _modificationValue;
        StatType _type;
        bool _isOverride;
        
        public bool IsOverride => _isOverride;
        [UnityEngine.Scripting.Preserve] public int Order => IsOverride ? 0 : 1;

        public PassiveCostModifier(StatType statType, float modificationValue, bool isOverride) {
            _modificationValue = modificationValue;
            _type = statType;
            _isOverride = isOverride;
        }

        public StatCost OverridenCost(StatCost baseCosts) {
            if (baseCosts.Stat.Type == _type) {
                float originalAmount = baseCosts.Amount;
                float handleAmount = _modificationValue;
                float modifiedAmount = _isOverride ? handleAmount : handleAmount + originalAmount;
                return new StatCost(baseCosts.Stat, modifiedAmount, skill: ParentModel);
            }

            return baseCosts;
        }
    }
}