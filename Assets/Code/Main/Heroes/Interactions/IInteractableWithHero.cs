using System.Collections.Generic;
using Awaken.TG.Main.Locations.Actions;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Interactions {
    public interface IInteractableWithHero {
        bool Interactable { get; }
        
        string DisplayName { get; }
        
        GameObject InteractionVSGameObject { get; }
        Vector3 InteractionPosition { get; }

        IEnumerable<IHeroAction> AvailableActions(Hero hero);
        IHeroAction DefaultAction(Hero hero);
        void DestroyInteraction();
    }

    /// <summary>
    /// Marker interface used when searching raycasted collider for IInteractableWithHero
    /// </summary>
    public interface IInteractableWithHeroProviderComplex {
        IInteractableWithHero InteractableWithHero(Collider collider);
    }

    public interface IInteractableWithHeroProvider : IInteractableWithHeroProviderComplex {
        new IInteractableWithHero InteractableWithHero { get; }

        IInteractableWithHero IInteractableWithHeroProviderComplex.InteractableWithHero(Collider collider) => InteractableWithHero;
    }
}