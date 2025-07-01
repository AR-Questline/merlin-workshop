using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Housing {
    [SpawnsView(typeof(VDecorMode))]
    public partial class DecorMode : Model {
        public override ushort TypeForSerialization => SavedModels.DecorMode;

        public override Domain DefaultDomain => Domain.Gameplay;

        protected override void OnFullyInitialized() {
            Hero.Current.ListenTo(Hero.Events.WalkedThroughPortal, Discard, this);
        }
    }
}