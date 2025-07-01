using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Storage {
    public partial class HeroStorageAction : AbstractLocationAction {
        public override ushort TypeForSerialization => SavedModels.HeroStorageAction;

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            Hero.Current.Storage.Open();
        }
    }
}