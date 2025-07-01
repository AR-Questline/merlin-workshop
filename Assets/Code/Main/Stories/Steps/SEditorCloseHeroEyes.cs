using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Hero: Close Eyes")]
    public class SEditorCloseHeroEyes : EditorStep {
        public float duration = 3;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SCloseHeroEyes {
                duration = duration
            };
        }
    }

    public partial class SCloseHeroEyes : StoryStep {
        public float duration = 3;
        
        public static class Events {
            public static readonly Event<Hero, bool> EyesClosed = new(nameof(EyesClosed));
        }
        
        public override StepResult Execute(Story story) {
            var result = new StepResult();
            CloseHeroEyes(result).Forget();
            World.EventSystem.Trigger(Hero.Current, Events.EyesClosed, true);
            return result;
        }

        async UniTaskVoid CloseHeroEyes(StepResult result) {
            if (await World.Services.Get<TransitionService>().TransitionToBlack(duration)) {
                result.Complete();
            }
        }
    }
}