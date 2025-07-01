using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.UI.Housing.FurnitureSlotOverview;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.UI.Housing {
    public class FurnitureSlotAction : MonoBehaviour, IInteractableWithHeroProvider, IInteractableWithHero, IHeroAction {
        [SerializeField] BoxCollider actionCollider;
        
        string _editFurnitureActionName;
        FurnitureSlotBase _furnitureSlot;
            
        public IInteractableWithHero InteractableWithHero => this;
        public bool Interactable => IsValidAction;
        public string DisplayName => _furnitureSlot.DisplayName;
        public GameObject InteractionVSGameObject => null;
        public Vector3 InteractionPosition => transform.position;
        
        public bool IsValidAction => World.HasAny<DecorMode>();
        public bool IsIllegal => false;
        public InfoFrame ActionFrame => new(_editFurnitureActionName, true);
        public InfoFrame InfoFrame1 => InfoFrame.Empty;
        public InfoFrame InfoFrame2 => InfoFrame.Empty;
        public string DefaultActionName => LocTerms.HousingFurnitureEdit.Translate();

        void Awake() {
            _editFurnitureActionName = LocTerms.HousingFurnitureEdit.Translate();
        }

        public void AssignFurnitureSlot(FurnitureSlotBase furnitureSlot) {
            _furnitureSlot = furnitureSlot;
        }

        public void AdjustActionCollider(Bounds bounds, Vector3 additionalSize) {
            transform.position = bounds.center;
            actionCollider.size = actionCollider.transform.InverseTransformVector(bounds.size + additionalSize);
            World.Any<HeroHousingInvolvement>()?.TriggerChange();
        }
        
        public void SetColliderActive(bool active) {
            actionCollider.enabled = active;
        }
        
        public IEnumerable<IHeroAction> AvailableActions(Hero hero) {
            yield return this;
        }

        public IHeroAction DefaultAction(Hero hero) => this;

        public void DestroyInteraction() {
            throw new Exception("Furniture slot action can't be destroyed");
        }

        public bool StartInteraction(Hero hero, IInteractableWithHero interactable) {
            if (_furnitureSlot != null) {
                FurnitureSlotOverviewUI.OpenFurnitureSlotOverviewUI(_furnitureSlot);
                return true;
            }
            return false;
        }

        public void FinishInteraction(Hero hero, IInteractableWithHero interactable) { }

        public void EndInteraction(Hero hero, IInteractableWithHero interactable) { }

        public ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return IsValidAction ? ActionAvailability.Available : ActionAvailability.Disabled;
        }
    }
}