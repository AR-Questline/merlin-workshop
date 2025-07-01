using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Bounty: Reset"), NodeSupportsOdin]
    public class SEditorBountyReset : EditorStep {
        [DisableIf(nameof(IsCrimeOwnerSet))]
        public LocationReference guard;
        [TemplateType(typeof(CrimeOwnerTemplate))]
        public TemplateReference crimeOwner;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SBountyReset {
                guard = guard,
                crimeOwnerTemplate = crimeOwner ?? new TemplateReference()
            };
        }
        
        bool IsCrimeOwnerSet => crimeOwner?.IsSet ?? false;
    }

    public partial class SBountyReset : StoryStep {
        public LocationReference guard;
        public TemplateReference crimeOwnerTemplate;
        
        public override StepResult Execute(Story story) {
            CrimeOwnerTemplate crimeOwner = crimeOwnerTemplate.TryGet<CrimeOwnerTemplate>();
            if (crimeOwner != null || StoryUtils.TryGetCrimeOwnerTemplate(story, guard, out crimeOwner)) {
                CrimeUtils.ClearBounty(crimeOwner);
            } else {
                Log.Important?.Error($"Story {story.Graph.guid}: Crime owner not found for bounty reset");
            }
            return StepResult.Immediate;
        }
    }
}