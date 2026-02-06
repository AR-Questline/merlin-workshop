using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Credits: Show"), NodeSupportsOdin]
    public class SEditorShowCredits : EditorStep {
        public bool displaySarrasCredits;
        [LabelWidth(100)]
        public Video.FadeInOptions fadeInOption;
        [Indent, LabelWidth(100)]
        [HideIf(nameof(fadeInOption), Video.FadeInOptions.None)]
        public Video.TransitionType fadeType = Video.TransitionType.FadeIn;
        [Indent, LabelWidth(100)]
        [ShowIf(nameof(fadeInOption), Video.FadeInOptions.ToCamera)]
        public float transitionTime = TransitionService.DefaultFadeIn;
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SShowCredits {
                displaySarrasCredits = displaySarrasCredits,
                fadeIn = fadeInOption,
                fadeType = fadeType,
                transitionTime = transitionTime,
            };
        }
    }
    
    public partial class SShowCredits : StoryStep {
        public bool displaySarrasCredits;
        public Video.FadeInOptions fadeIn;
        public Video.TransitionType fadeType;
        public float transitionTime;
        
        public override StepResult Execute(Story story) {
            var result = new StepResult();
            Run(story, result).Forget();
            return result;
        }
        
        async UniTask Run(Story story, StepResult result) {
            await InitialFade();
            await Credits.Show(displaySarrasCredits ? typeof(VCreditsSarras) : typeof(VCredits));
            result.Complete();
        }
        
        async UniTask InitialFade() {
            if (fadeIn == Video.FadeInOptions.None) {
                return;
            }
            bool instant = fadeIn.HasFlagFast(Video.FadeInOptions.ToCameraInstant);

            transitionTime = instant ? 0 : transitionTime;
            switch (fadeType) {
                case Video.TransitionType.Transition:
                    await World.Services.Get<TransitionService>().ToCamera(transitionTime);
                    break;
                case Video.TransitionType.FadeIn:
                    await World.Services.Get<TransitionService>().TransitionFromBlack(transitionTime);
                    break;
            }
        }
    }
}
