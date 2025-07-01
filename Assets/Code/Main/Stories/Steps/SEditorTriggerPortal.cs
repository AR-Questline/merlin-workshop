using System.Linq;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Cysharp.Threading.Tasks;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Portal: Trigger"), NodeSupportsOdin]
    public class SEditorTriggerPortal : EditorStep {
        public LocationReference locRef;
        public bool waitForTeleport = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STriggerPortal {
                locRef = locRef,
                waitForTeleport = waitForTeleport
            };
        }
    }

    public partial class STriggerPortal : StoryStep {
        public LocationReference locRef;
        public bool waitForTeleport;
        
        public override StepResult Execute(Story story) {
            var result = new StepResult();
            ExecuteInternal(story, result).Forget();
            return result;
        }

        async UniTaskVoid ExecuteInternal(Story story, StepResult result) {
            if (waitForTeleport) {
                if (!await story.Hero.EnsureHeroCanTeleport()) {
                    result.Complete();
                    return;
                }
            }
            
            Location location = locRef.MatchingLocations(story).FirstOrDefault(l => l.TryGetElement<Portal>() != null);
            location?.Element<Portal>().Execute(story.Hero);
            result.Complete();
        }
    }
}
