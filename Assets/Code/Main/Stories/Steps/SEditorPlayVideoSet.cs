using System;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Cutscenes/Cutscene: Play Video Set"), NodeSupportsOdin]
    public class SEditorPlayVideoSet : EditorStep {
        [TemplateType(typeof(VideoSetData))] public TemplateReference data;

        [LabelWidth(120)] public bool muteOtherAudio;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPlayVideoSet {
                videoSet = data,
                muteOtherAudio = muteOtherAudio
            };
        }
    }

    public partial class SPlayVideoSet : StoryStep {
        public TemplateReference videoSet;
        public bool muteOtherAudio;

        public override StepResult Execute(Story story) {
            if (DebugReferences.FastStory || DebugReferences.ImmediateStory) {
                return StepResult.Immediate;
            }

            var loadingHandles = videoSet.Get<VideoSetData>().GetLoadingHandles();
            if (loadingHandles.Length == 0) {
                return StepResult.Immediate;
            }

            var result = new StepResult();
            var uiState = UIState.TransparentState.WithPauseTime();

            UIStateStack.Instance.PushState(uiState, story);
            
            PlayAllVideos(loadingHandles, story, muteOtherAudio, () => { 
                UIStateStack.Instance.RemoveState(uiState);
                result.Complete();
            });
            return result;
        }

        static void PlayAllVideos(LoadingHandle[] videos, IModel owner, bool muteAudio, Action onComplete) {
            var videoModel = World.Add(Video.FullScreen(videos, owner, muteAudio));
            videoModel.ListenTo(Model.Events.BeforeDiscarded, onComplete.Invoke, owner);
        }
    }
}