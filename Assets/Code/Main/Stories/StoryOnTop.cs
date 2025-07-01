using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.States;
using World = Awaken.TG.MVC.World;

namespace Awaken.TG.Main.Stories {
    // This model exists when story view is shown
    public partial class StoryOnTop : Element<Story>, IUIStateSource {
        public sealed override bool IsNotSaved => true;

        public bool OverlayStory { get; private set; }
        public UIState UIState => OverlayStory 
            ? UIState.ModalState(HUDState.StoryPanel | HUDState.QuestTrackerHidden) 
            : _heroInvolved 
                ? UIState.TransparentState.WithPauseWeatherTime() 
                : UIState.TransparentState;

        bool _heroInvolved;
        
        Story Story => ParentModel;

        public StoryOnTop(bool overlayStory = true) {
            OverlayStory = overlayStory;
        }
        
        protected override void OnInitialize() {
            if (ParentModel.HasElement<HeroDialogueInvolvement>()) {
                _heroInvolved = true;
                RefreshUIState();
            }
            ParentModel.ListenTo(Events.AfterElementsCollectionModified, AfterElementsCollectionModified, this);
        }
        
        void AfterElementsCollectionModified(Element e) {
            if (e is HeroDialogueInvolvement heroDialogueInvolvement) {
                if (_heroInvolved != !heroDialogueInvolvement.HasBeenDiscarded) {
                    _heroInvolved = !heroDialogueInvolvement.HasBeenDiscarded;
                    RefreshUIState();
                };
                
            }
        }
        
        void RefreshUIState() {
            var stack = UIStateStack.Instance;
            stack.ReleaseAllOwnedBy(this);
            stack.PushState(UIState, this);
        }

        public void ProcessEndFrame() {
            if (Story.MainView == null) {
                Discard();
            }
        }
    }
}