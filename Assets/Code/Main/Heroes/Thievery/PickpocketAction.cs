using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Containers;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Thievery {
    public partial class PickpocketAction : AbstractLocationAction {
        const float BlockActionDuration = 5f;

        public sealed override bool IsNotSaved => true;

        public static float lastPickpocketFailTime;

        IllegalActionTracker _illegalActionTracker;
        IInventory _inventory;

        IInventory Inventory => _inventory ??= ParentModel?.Inventory;

        public override bool IsIllegal => Crime.Pickpocket(ParentModel.Element<NpcElement>()).IsCrime();

        protected override void OnFullyInitialized() {
            _illegalActionTracker = Hero.Current.Element<IllegalActionTracker>();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            UnmarkNonPickpocketableItems();
            ParentModel.TryGetElement<ContainerUI>()?.Discard();
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return !_illegalActionTracker.IsCrouching
                   || !HasAnyItemsToShow()
                   || _illegalActionTracker.SeenByNPC(ParentModel.Element<NpcElement>())
                   || ParentModel.GetCurrentCrimeOwnersFor(CrimeArchetype.Pickpocketing(CrimeItemValue.High, CrimeNpcValue.High)).IsEmpty
                   || ParentModel.Element<NpcElement>() is not { Template: { isPickpocketable: true } } npcElement
                   || npcElement.IsInCombat()
                   || (lastPickpocketFailTime + BlockActionDuration) > Time.time
                ? ActionAvailability.Disabled
                : base.GetAvailability(hero, interactable);
        }

        public override IHeroInteractionUI InteractionUIToShow(IInteractableWithHero interactable) 
            => new AutoPickpocketHeroInteractionUI(interactable, this);

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (interactable is not Location location) return;
            World.All<ContainerUI>().Where(c => c.ParentModel != location).ToArray().ForEach(c => c.Discard());
            if (!location.HasElement<ContainerUI>()) {
                MarkNonPickpocketableItems();
                var ui = ShowContainerContents(location);
                ui.ListenTo(Events.BeforeDiscarded, _ => UnmarkNonPickpocketableItems(), this);
            }
        }
 
        protected override void OnEnd(Hero hero, IInteractableWithHero interactable) {
            ParentModel.TryGetElement<ContainerUI>()?.Discard();
        }

        bool HasAnyItemsToShow() {
            if (ParentModel.TryGetElement<SearchAction>(out var searchAction)) {
                return searchAction.HasUnlockedItemsToShow();
            }
            return Inventory != null && Inventory.AllUnlockedAndVisibleItems().Any();
        }

        void MarkNonPickpocketableItems() {
            if (Inventory == null) {
                return;
            }
            
            foreach (var item in Inventory.AllLockedItems()) {
                item.AddElement<NonPickpocketable>();
            }
        }

        void UnmarkNonPickpocketableItems() {
            if (Inventory == null) {
                return;
            }
            foreach (var item in Inventory.Items) {
                item.RemoveElementsOfType<NonPickpocketable>();
            }
        }
        
        ContainerUI ShowContainerContents(Location location, bool openTransfer = false) {
            if (location.TryGetElement<SearchAction>(out var searchAction)) {
                return searchAction.ShowContainerContents(location);
            }

            var ui = new ContainerUI(Inventory, null, false);
            location.AddElement(ui);
            if (openTransfer) {
                ui.TransferItems();
            }
            return ui;
        }

        protected override void OnEnabled() {
            foreach (var a in ParentModel.Elements<AbstractLocationAction>().Where(a => a != this)) {
                a.DisableAction();
            }
        }

        protected override void OnDisabled() {
            foreach (var a in ParentModel.Elements<AbstractLocationAction>().Where(a => a != this)) {
                a.EnableAction();
            }
            ParentModel.TryGetElement<ContainerUI>()?.Discard();
        }
    }
}