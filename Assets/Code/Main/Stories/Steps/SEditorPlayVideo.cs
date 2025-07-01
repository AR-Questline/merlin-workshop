using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Cutscenes/Cutscene: Play Video")]
    public class SEditorPlayVideo : EditorStep {
        public LoadingHandle video;
        [Space] 
        public Video.TransitionType transitionType = Video.TransitionType.Transition;
        public Video.FadeInOptions fadeInOptions = Video.FadeInOptions.ToCamera;
        public Video.FadeOutOptions fadeOutOptions;
        public bool pauseARTime;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPlayVideo {
                video = video,
                transitionType = transitionType,
                fadeInOptions = fadeInOptions,
                fadeOutOptions = fadeOutOptions,
                pauseARTime = pauseARTime,
            };
        }
    }

    public partial class SPlayVideo : StoryStep {
        public LoadingHandle video;
        public Video.TransitionType transitionType;
        public Video.FadeInOptions fadeInOptions;
        public Video.FadeOutOptions fadeOutOptions;
        public bool pauseARTime;

        public override StepResult Execute(Story story) {
            if (DebugReferences.FastStory) {
                return StepResult.Immediate;
            }

            if (DebugReferences.ImmediateStory) {
                return StepResult.Immediate;
            }
            
            var result = new StepResult();
            var uiState = UIState.TransparentState.WithPauseTime();
            if (pauseARTime) {
                UIStateStack.Instance.PushState(uiState, story);
            }

            story.ClearText();
            var videoModel = World.Add(Video.FullScreen(video, fadeType: transitionType, fadeIn: fadeInOptions, fadeOut: fadeOutOptions));
            videoModel.ListenTo(Model.Events.BeforeDiscarded, () => {
                result.Complete();
                if (pauseARTime) {
                    UIStateStack.Instance.RemoveState(uiState);
                }
            }, story);
            return result;
        }
    }
}