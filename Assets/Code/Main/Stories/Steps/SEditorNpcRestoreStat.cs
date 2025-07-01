using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Debugging;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC Stat: Restore"), NodeSupportsOdin]
    public class SEditorNpcRestoreStat : EditorStep {
        public LocationReference locationReference;
        [RichEnumExtends(typeof(StatType))]
        public RichEnumReference affectedStat = AliveStatType.Health;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcRestoreStat {
                locationReference = locationReference,
                affectedStat = affectedStat
            };
        }
    }
    
    public partial class SNpcRestoreStat : StoryStep {
        public LocationReference locationReference;
        public RichEnumReference affectedStat;
        
        public StatType AffectedStat => affectedStat.Enum as StatType;
        
        public override StepResult Execute(Story story) {
            var stats = ExtractStat(AffectedStat, locationReference, story);
            foreach (var stat in stats) {
                if (stat is LimitedStat limited) {
                    limited.SetToFull();
                } else {
                    Log.Important?.Error("Can't restore stat that has no upper bounds.");
                }
            }
            return StepResult.Immediate;
        }
        
        static IEnumerable<Stat> ExtractStat(StatType affectedStat, LocationReference locationReference, Story api) {
            return locationReference.MatchingLocations(api).Select(location => location.Stat(affectedStat)).Where(stat => stat != null);
        }
    }
}