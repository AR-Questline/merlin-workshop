using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemPlaceLocationHeroAction : Element<ItemPlaceLocation>, IHeroAction, IInteractableWithHero {
        public override ushort TypeForSerialization => SavedModels.ItemPlaceLocationHeroAction;

        public bool IsValidAction => true;
        public bool IsIllegal => false;
        public InfoFrame ActionFrame => ParentModel.IsValidPlacement ? new(DefaultActionName, true) : new(LocTerms.Blocked.Translate(), false);
        public InfoFrame InfoFrame1 => ParentModel.IsValidPlacement ? new(string.Empty, false) : new (ParentModel.GetBlockReason(), false);
        public InfoFrame InfoFrame2 => new(string.Empty, false);
        public string DefaultActionName => LocTerms.PlaceOnGround.Translate();
        public bool Interactable => ParentModel.CanHeroActionBePerformed;
        public string DisplayName => ParentModel.ParentModel.DisplayName;
        public GameObject InteractionVSGameObject => null;
        public Vector3 InteractionPosition => Hero.Current.Coords;

        public bool StartInteraction(Hero hero, IInteractableWithHero interactable) {
            ParentModel.HeroActionPerformed();
            return true;
        }

        public void FinishInteraction(Hero hero, IInteractableWithHero interactable) { }

        public void EndInteraction(Hero hero, IInteractableWithHero interactable) { }

        public ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return ParentModel.CanHeroActionBePerformed ? ActionAvailability.Available : ActionAvailability.Disabled;
        }
        
        public IEnumerable<IHeroAction> AvailableActions(Hero hero) {
            yield return this;
        }

        public IHeroAction DefaultAction(Hero hero) {
            return this;
        }

        public void DestroyInteraction() { }
    }
}