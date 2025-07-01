using System;
using System.Collections.Generic;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions {
    public class WaterFishingAction : MonoBehaviour, IInteractableWithHeroProvider, IInteractableWithHero, IHeroAction {
        public static bool fishingAvailable;
        
        public Vector3 Coords => transform.position;
        public bool Interactable => IsValidAction;
        public string DisplayName => null;
        public GameObject InteractionVSGameObject => null;
        public Vector3 InteractionPosition => Coords;
        public bool IsIllegal => false;

        public bool IsValidAction => true;
        public InfoFrame ActionFrame => new(DefaultActionName, true);
        public InfoFrame InfoFrame1 => new(string.Empty, false);
        public InfoFrame InfoFrame2 => new(string.Empty, false);
        HeroStateType CurrentFishingState => Hero.Current.Element<FishingFSM>().CurrentStateType;
        
        public string DefaultActionName => CurrentFishingState switch {
                HeroStateType.FishingIdle => LocTerms.FishingCatch.Translate(),
                HeroStateType.FishingBite => LocTerms.FishingCatch.Translate(),
                HeroStateType.FishingFight => LocTerms.FishingPull.Translate(),
                _ => LocTerms.Fishing.Translate()
            };

        public IInteractableWithHero InteractableWithHero => this;

        public IEnumerable<IHeroAction> AvailableActions(Hero hero) {
            yield return this;
        }
        public IHeroAction DefaultAction(Hero hero) => this;

        public void DestroyInteraction() => throw new Exception("FishingWater cannot be destroyed");

        public bool StartInteraction(Hero hero, IInteractableWithHero interactable) {
            hero.TryGetElement<HeroToolAction>()?.StartToolAction();
            
            return true;
        }
        
        public void FinishInteraction(Hero hero, IInteractableWithHero interactable) { }
        public void EndInteraction(Hero hero, IInteractableWithHero interactable) { }
        public ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            bool isWaitingForBait = CurrentFishingState is HeroStateType.FishingIdle or HeroStateType.FishingBite or HeroStateType.FishingBiteLoop;
            bool isFightingWithFish = CurrentFishingState is HeroStateType.FishingFight;
            return IsValidAction && !isFightingWithFish && (fishingAvailable || isWaitingForBait) ? ActionAvailability.Available : ActionAvailability.Disabled;
        }
    }
}