using System;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Popup: Open")]
    public class SEditorOpenPopup : EditorStep {
        [NodeEnum] public SOpenPopup.PopupSize size;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SOpenPopup {
                size = size
            };
        }
    }

    public partial class SOpenPopup : StoryStep {
        public PopupSize size;
        
        Type ViewType => size switch {
            PopupSize.Small => typeof(VSmallStoryPopupUI),
            PopupSize.ReadableWithImage => typeof(VReadablePopupUI),
            PopupSize.ThanksForPlaying => typeof(VThanksForPlayingPopupUI),
            PopupSize.ConquestPanel => typeof(VStoryPanel),
            _ => typeof(VSmallStoryPopupUI)
        };

        public override StepResult Execute(Story story) {
            story.MainView?.Discard();
            World.SpawnView(story, ViewType, true);

            var stack = UIStateStack.Instance;
            stack.ReleaseAllOwnedBy(story);
            stack.PushState(UIState.ModalState(HUDState.MiddlePanelShown), story);
            
            return StepResult.Immediate;
        }
        
        public enum PopupSize : byte {
            Small = 0,
            Medium = 1,
            Big = 2,
            ReadableWithImage = 3,
            ThanksForPlaying = 4,
            ConquestPanel = 5,
        }
    }
}
