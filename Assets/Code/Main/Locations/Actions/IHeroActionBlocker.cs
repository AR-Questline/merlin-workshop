using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Actions {

    public interface IHeroActionBlocker : IModel {
        bool IsBlocked(Hero hero, IInteractableWithHero interactable);
    }
}