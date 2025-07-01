using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Interactions {
    public interface IHeroInteractionUI : IElement<Hero> {
        bool Visible { get; }
        IInteractableWithHero Interactable { get; }
    }
}