using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Game: Add Reputation")]
    public class SEditorReputationProgress : EditorStep {
        [TemplateType(typeof(CrimeOwnerTemplate))]
        public TemplateReference crimeOwner;
        public int value;
        public ReputationType reputationType;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SReputationProgress {
                crimeOwner = crimeOwner,
                value = value,
                reputationType = reputationType
            };
        }
    }

    public partial class SReputationProgress : StoryStep {
        public TemplateReference crimeOwner;
        public int value;
        public ReputationType reputationType;

        CrimeOwnerTemplate CrimeOwner => crimeOwner.Get<CrimeOwnerTemplate>();
        
        public override StepResult Execute(Story story) {
            if (CrimeOwner == null) {
                Log.Important?.Error($"Invalid faction setup in Add Reputation in Story {story.ID}");
                return StepResult.Immediate;
            }
            
            OwnerReputationUtil.ChangeReputation(CrimeOwner, value, reputationType);
            return StepResult.Immediate;
        }
    }
}