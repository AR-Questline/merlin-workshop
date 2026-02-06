using Awaken.TG.Main.Character;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class HeroStunnedInvolvement : HeroInvolvement<StunnedCharacterElement> {
        public sealed override bool IsNotSaved => true;
        
        public override Location FocusedLocation => null;
        public override UIState UIState => UIState.BaseState;
        public override bool HideHands => false;
        
        public HeroStunnedInvolvement() : base(false) { }
    }
}