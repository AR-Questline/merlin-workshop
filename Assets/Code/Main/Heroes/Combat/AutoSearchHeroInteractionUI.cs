using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.Combat {
    [SpawnsView(typeof(VHeroInteractionUI))]
    public partial class AutoSearchHeroInteractionUI : AutoStartHeroInteractionUI<SearchAction>, IUniqueKeyProvider {
        public override bool Visible => action.IsEmpty();
        public KeyIcon.Data UniqueKey => new(KeyBindings.Gameplay.Interact, false);

        public AutoSearchHeroInteractionUI(IInteractableWithHero interactable, SearchAction action) : base(interactable, action) { }
    }
}