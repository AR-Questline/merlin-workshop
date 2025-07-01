using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Hero Stat: Restore")]
    public class SEditorRestoreStat : EditorStep {
        [RichEnumExtends(typeof(StatType))]
        public RichEnumReference affectedStat = AliveStatType.Health;
        public bool isKnown = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SRestoreStat {
                affectedStat = affectedStat,
                isKnown = isKnown
            };
        }
    }
    
    public partial class SRestoreStat : StoryStep {
        public RichEnumReference affectedStat = AliveStatType.Health;
        public bool isKnown = true;
        
        public StatType AffectedStat => affectedStat.Enum as StatType;
        
        public override StepResult Execute(Story story) {
            var stat = ExtractStat(AffectedStat, story);
            if (stat is LimitedStat limited) {
                limited.SetToFull();
            } else {
                Log.Important?.Error("Can't restore stat that has no upper bounds.");
            }

            return StepResult.Immediate;
        }
        
        static Stat ExtractStat(StatType affectedStat, Story story) {
            IWithStats subject = StoryRole.DefaultForStat(affectedStat).RetrieveFrom<IWithStats>(story);
            return subject.Stat(affectedStat);
        }

        public override void AppendKnownEffects(Story story, ref StructList<string> effects) {
            if (!isKnown) {
                return;
            }
            var stat = ExtractStat(AffectedStat, story);
            if (stat is LimitedStat limited) {
                effects.Add($"{limited.UpperLimit} {AffectedStat.DisplayName}");
            }
        }
    }
}