using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Faction: Change Hostility")]
    public class SEditorFactionChangeHostility : EditorStep {
        [TemplateType(typeof(FactionTemplate))] public TemplateReference from;
        [TemplateType(typeof(FactionTemplate))] public TemplateReference to;
        public Antagonism antagonism;
        public bool twoWay = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SFactionChangeHostility {
                from = from,
                to = to,
                antagonism = antagonism,
                twoWay = twoWay
            };
        }
    }

    public partial class SFactionChangeHostility : StoryStep {
        public TemplateReference from;
        public TemplateReference to;
        public Antagonism antagonism;
        public bool twoWay = true;
        
        public override StepResult Execute(Story story) {
            if (from is not { IsSet: true } || to is not { IsSet: true }) {
                return StepResult.Immediate;
            }

            var fromFaction = from.Get<FactionTemplate>();
            var toFaction = to.Get<FactionTemplate>();
            FactionToFactionAntagonismOverride.UpdateAntagonism(fromFaction, toFaction, antagonism);
            if (twoWay) {
                FactionToFactionAntagonismOverride.UpdateAntagonism(toFaction, fromFaction, antagonism);
            }
            return StepResult.Immediate;
        }
    }
}