using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Actions {
    /// <summary>
    /// Implemented by classes that represent actions available for given Hero in given Location.
    /// These actions show on GUI when user targets a location, choosing one of them adds it to Hero's order queue.
    /// </summary>
    public interface IHeroAction {
        /// <summary>
        /// Is triggered when hero starts performing Action
        /// In most cased hero clicks an Interact button
        /// </summary>
        bool StartInteraction(Hero hero, IInteractableWithHero interactable);
        /// <summary>
        /// Is triggered when Action has ended
        /// automatically right after Start Interaction if it was instant
        /// manually after Start Interaction if Action was has duration (animations, ui etc.)
        /// </summary>
        void FinishInteraction(Hero hero, IInteractableWithHero interactable);
        /// <summary>
        /// Is triggered when hero stops looking at the Action prompt
        /// </summary>
        void EndInteraction(Hero hero, IInteractableWithHero interactable);
        ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable);
        bool IsValidAction { get; }
        bool IsIllegal { get; }
        IHeroInteractionUI InteractionUIToShow(IInteractableWithHero interactable) => IsIllegal
                                                      ? new HeroIllegalInteractionUI(interactable)
                                                      : new HeroInteractionUI(interactable);
        InfoFrame ActionFrame { get; }
        InfoFrame InfoFrame1 { get; }
        InfoFrame InfoFrame2 { get; }
        string DefaultActionName { get; }
    }
    public interface IHeroActionModel : IHeroAction, IModel {
        bool IHeroAction.IsValidAction => !HasBeenDiscarded;
    }
}