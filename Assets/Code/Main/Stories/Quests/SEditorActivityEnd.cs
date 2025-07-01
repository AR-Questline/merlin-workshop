using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Quests {
    [Element("Technical/PS5 Activity: End")]
    public class SEditorActivityEnd : EditorStep {
        public string activityId;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SActivityEnd {
                activityId = activityId
            };
        }
    }
    
    public partial class SActivityEnd : StoryStep {
        public string activityId;
        
        public override StepResult Execute(Story story) {
#if UNITY_PS5
            var social = World.Services.TryGet<SocialService>();
            if (social is SocialServices.PlayStationServices.PlayStationSocialService psSocial) {
                psSocial.EndActivity(activityId);
            }
#endif
            return StepResult.Immediate;
        }
    }
}