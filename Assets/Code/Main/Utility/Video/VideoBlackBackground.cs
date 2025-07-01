using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.Main.Utility.Video {
    [SpawnsView(typeof(VVideoBlackBackground))]
    public class VideoBlackBackground : Model, IUIStateSource {
        public override Domain DefaultDomain => Domain.Gameplay;
        public override bool IsNotSaved => true;

        public UIState UIState => UIState.TransparentState.WithPauseTime();

        Model _owner;

        public VideoBlackBackground(Model owner) {
            _owner = owner;
        }

        protected override void OnInitialize() {
            _owner.ListenTo(Events.BeforeDiscarded, Discard, this);
        }
    }
}