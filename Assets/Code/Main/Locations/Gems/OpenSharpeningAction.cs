using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Gems {
    public partial class OpenSharpeningAction : AbstractLocationAction, IRefreshedByAttachment<OpenSharpeningAttachment> {
        public override ushort TypeForSerialization => SavedModels.OpenSharpeningAction;

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            GemsUI.OpenSharpeningUI();
        }

        public void InitFromAttachment(OpenSharpeningAttachment spec, bool isRestored) { }
    }
}