using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Setup {
    /// <summary>
    /// Dummy script used to determine the Location, which collider was hit by VHeroInteraction.cs
    /// </summary>
    public class LocationParent : MonoBehaviour, IInteractableWithHeroProvider, IModelProvider {
        public IInteractableWithHero InteractableWithHero {
            get {
                var components = GetComponentsInChildren<IInteractableWithHeroProvider>();
                if (components.Length == 0) {
                    return null;
                }
                foreach (var interactableWithHeroProvider in components) {
                    if (interactableWithHeroProvider != this) {
                        return interactableWithHeroProvider.InteractableWithHero;
                    }
                }
                return null;
            }
        }

        public IModel Model => GetComponentInChildren<VLocation>(true)?.Target;
    }
}
