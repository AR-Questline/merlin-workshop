using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Fast Travel")]
    public class SEditorFastTravel : EditorStep {
        public bool withTransition = true;
        public string portalTag;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SFastTravel {
                withTransition = withTransition,
                portalTag = portalTag,
            };
        }
    }

    public partial class SFastTravel : StoryStep {
        public bool withTransition = true;
        public string portalTag;
        
        public override StepResult Execute(Story story) {
            if (story.Hero == null) {
                Log.Important?.Error("Can't fast travel without hero selected");
                return StepResult.Immediate;
            }

            var result = new StepResult();
            TravelAndWait(story, result).Forget();
            
            if (IsLastStep()) {
                story.FinishStory();
                return StepResult.Immediate;
            }

            return result;
        }
        
        async UniTaskVoid TravelAndWait(Story api, StepResult result) {
            SceneReference scene = World.Services.Get<SceneService>().ActiveSceneRef;
            Portal ft = Portal.FindWithTagOrDefault(scene, portalTag);
            await Portal.FastTravel.To(api.Hero, ft, withTransition);
            result.Complete();
        }
    }
}