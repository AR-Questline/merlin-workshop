using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Interactions {
    /// <summary>
    /// Helper class to do interactions
    /// </summary>
    public static class HeroInteraction {

        // === Interaction implementation

        public static bool StartInteraction(Hero hero, IInteractableWithHero place, out IHeroAction action) {
            action = null;

            if (place is not { Interactable: true }) {
                return false;
            }
            
            List<IHeroAction> actions = place.AvailableActions(hero).Where(action => IsAvailable(action, hero, place)).ToList();
            if (actions.Count == 0) {
                return false;
            }

            foreach (var a in actions) {
                if (a.StartInteraction(hero, place)) {
                    action = a;
                    return true;
                }
            }

            return true;
        }

        public static bool ShouldHappen(Hero hero, IInteractableWithHero interactable) {
            return interactable is { Interactable: true } && interactable.AvailableActions(hero).Any(action => IsAvailable(action, hero, interactable));
        }

        static bool IsAvailable(IHeroAction action, Hero hero, IInteractableWithHero interactable) {
            return action.GetAvailability(hero, interactable) == ActionAvailability.Available && !ShouldBeDisabled(action);
        }

        public static IEnumerable<IHeroAction> ActionsFromLocation(Location location) {
            HashSet<Type> overridenActions = null;
            if (location.TryGetElement(out NpcElement npc) && npc.IsUnique && npc.NpcPresence != null) {
                var presenceLocation = npc.NpcPresence.ParentModel;
                foreach (var action in presenceLocation.Elements<IHeroActionModel>()) {
                    overridenActions ??= new();
                    overridenActions.Add(action.GetType());
                    yield return action;
                }
                foreach (var actionProvider in presenceLocation.Elements<ILocationActionProvider>()) {
                    foreach (var action in actionProvider.GetAdditionalActions(Hero.Current)) {
                        overridenActions ??= new();
                        overridenActions.Add(action.GetType());
                        yield return action;
                    }
                }
            }
            foreach (var action in location.Elements<IHeroActionModel>()) {
                if (overridenActions != null && overridenActions.Contains(action.GetType())) {
                    continue;
                }
                yield return action;
            }
            foreach (var actionProvider in location.Elements<ILocationActionProvider>()) {
                foreach (var action in actionProvider.GetAdditionalActions(Hero.Current)) {
                    if (overridenActions != null && overridenActions.Contains(action.GetType())) {
                        continue;
                    }
                    yield return action;
                }
            }
        }

        static bool ShouldBeDisabled(IHeroAction action) {
            return World.All<IDisableHeroActions>().Any(a => a.HeroActionsDisabled(action));
        }
    }
}