using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.Main.Stories.Conditions {
    /// <summary>
    /// Check if hero stats meet requirements
    /// </summary>
    [Element("Hero: Check stat")]
    public class CEditorHasStats : EditorCondition {

        [RichEnumExtends(typeof(StatType))]
        [RichEnumSearchBox]
        public RichEnumReference statType;

        [NodeEnum]
        public Comparison comparison;
        public StatValue statValue;

        public StatType StatType => statType.EnumAs<StatType>();
        
        public string Summary() {
            return $"{StatType?.EnumName} {comparison.ToString()} {statValue.Label()}";
        }

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CHasStats {
                statType = statType,
                comparison = comparison,
                statValue = statValue
            };
        }
    }
    
    public partial class CHasStats : StoryCondition {
        public RichEnumReference statType;
        public Comparison comparison;
        public StatValue statValue;
        
        public StatType StatType => statType.EnumAs<StatType>();
        
        public override bool Fulfilled(Story story, StoryStep step) {
            StoryRole role = StoryRole.DefaultForStat(StatType);
            IWithStats statOwner = role.RetrieveFrom<IWithStats>(story); 
            if (statOwner == null) {
                return false;
            }

            Stat stat = statOwner.Stat(StatType);
            return stat.ModifiedValue.CompareTo(statValue.GetValue(stat)) == (int) comparison;
        }
    }
}