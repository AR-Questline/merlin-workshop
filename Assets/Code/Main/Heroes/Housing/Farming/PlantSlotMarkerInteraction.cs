using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.UI.Housing.Farming;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Times;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing.Farming {
    /// <summary>
    /// Marker for plant slot. Used to determine size and transform where plant should be spawned. Also handles player interaction.
    /// </summary>
    public class PlantSlotMarkerInteraction : MonoBehaviour, IInteractableWithHeroProvider, IInteractableWithHero, IHeroAction {
        public PlantSize plantSize;
        public PlantSlotMarkerInteraction parent;
        public PlantSlotMarkerInteraction[] children = Array.Empty<PlantSlotMarkerInteraction>();
        
        PlantSlot _plantSlot;
        IEventListener _plantSlotListener;
        IEventListener _plantGrowthListener;
        
        public IInteractableWithHero InteractableWithHero => this;
        public bool Interactable => IsValidAction;
        public string DisplayName => _plantSlot.PlantName;
        public GameObject InteractionVSGameObject => null;
        public Vector3 InteractionPosition => transform.position;
        public bool IsValidAction => _plantSlot != null &&
                                     _plantSlot.ParentModel.IsInteractable();
        public bool IsIllegal => false;

        public InfoFrame ActionFrame {
            get {
                if (IsBlocked) {
                    return InfoFrame.Empty;
                }

                if (IsHarvestable) {
                    ARTimeSpan remainingTime = _plantSlot.TotalTimeLeft;
                    string info = _plantSlot.State == PlantState.FullyGrown
                        ? $"{_plantSlot.DisplayState}"
                        : $"{_plantSlot.DisplayState} | <sprite=0> {(int) remainingTime.TotalHours:00}:{remainingTime.Minutes:00}";
                    return new InfoFrame(info, false);
                }
                
                return new InfoFrame(DefaultActionName, false);
            }
        }

        public InfoFrame InfoFrame1 => IsReadyForPlanting
            ? new InfoFrame(LocTerms.FarmingPlant.Translate(), true)
            : InfoFrame.Empty;
        public InfoFrame InfoFrame2 => IsHarvestable
            ? new InfoFrame(LocTerms.FarmingHarvest.Translate(), true)
            : InfoFrame.Empty;
        public string DefaultActionName => _plantSlot.DisplayState;
        
        Flowerpot Flowerpot => _plantSlot.ParentModel;
        bool IsBlocked => _plantSlot is { State: PlantState.Blocked };
        bool IsHarvestable => _plantSlot is { State: PlantState.FullyGrown or PlantState.Growing };
        bool IsReadyForPlanting => _plantSlot is { State: PlantState.ReadyForPlanting };

        public void Reset() {
            List<PlantSlotMarkerInteraction> childObjects = new();
            foreach (Transform child in transform) {
                PlantSlotMarkerInteraction childMarker = child.GetComponent<PlantSlotMarkerInteraction>();
                if (childMarker != null) {
                    childObjects.Add(childMarker);
                }
            }
            
            parent =  transform.parent.GetComponent<PlantSlotMarkerInteraction>();
            children = childObjects.ToArray();
        }

        public void AssignPlantSlot(PlantSlot plantSlot) {
            _plantSlot = plantSlot;
            _plantSlotListener = _plantSlot.ListenTo(PlantSlot.Events.PlantSlotStateChanged, OnPlantDataRefreshed);
            _plantGrowthListener = _plantSlot.ListenTo(PlantSlot.Events.PlantGrowthTimeChanged, OnPlantDataRefreshed);
        }
        
        public IEnumerable<IHeroAction> AvailableActions(Hero hero) {
            yield return this;
        }

        public IHeroAction DefaultAction(Hero hero) => this;

        public void DestroyInteraction() {
            throw new Exception("Plant Slot Interaction can't be destroyed");
        }

        public bool StartInteraction(Hero hero, IInteractableWithHero interactable) {
            if (IsReadyForPlanting) {
                // TODO delete FarmingUI model and all connections when it's REALLY not needed
                SimpleFarmingUI.OpenSimpleFarmingUI(Flowerpot);
                return true;
            }
            
            if (IsHarvestable) {
                _plantSlot.Harvest();
            }
            return true;
        }

        public void FinishInteraction(Hero hero, IInteractableWithHero interactable) { }

        public void EndInteraction(Hero hero, IInteractableWithHero interactable) { }

        public ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return IsValidAction ? ActionAvailability.Available : ActionAvailability.Disabled;
        }

        static void OnPlantDataRefreshed(PlantSlot _) {
            World.Any<HeroInteractionUI>()?.TriggerChange();
        }

        void OnDestroy() {
            World.EventSystem.TryDisposeListener(ref _plantSlotListener);
            World.EventSystem.TryDisposeListener(ref _plantGrowthListener);
            _plantSlot = null;
        }
    }
}