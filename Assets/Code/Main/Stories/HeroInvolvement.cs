using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.States;
using UnityEngine;

namespace Awaken.TG.Main.Stories {
    public interface IHeroInvolvement : IElement, IUIStateSource {
        Location FocusedLocation { get; }
        Transform FocusParent { get; }
        bool HideHands { get; }
        
        bool TryGetFocus(out Transform focus);
    }
    
    public abstract partial class HeroInvolvement<T> : Element<T>, IHeroInvolvement, IOverlapRecoveryDisablingBlocker where T : Model {
        readonly bool _hideWeapons;
        
        public virtual Hero Hero => Hero.Current;
        public abstract Location FocusedLocation { get; }
        public virtual bool HideHands => true;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown);

        public Transform FocusParent {
            get {
                if (ParentModel.TryGetElement(out StoryInteractionFocusOverride focusOverride) && focusOverride.FocusParent != null) {
                    return focusOverride.FocusParent;
                }
                return FocusedLocation?.ViewParent;
            }
        }

        protected HeroInvolvement() {
            _hideWeapons = true;
        }
        
        protected HeroInvolvement(bool hideWeapons) {
            _hideWeapons = hideWeapons;
        }
        
        public virtual bool TryGetFocus(out Transform focus) {
            if (ParentModel.TryGetElement(out StoryInteractionFocusOverride focusOverride) && focusOverride.FocusPoint != null) {
                focus = focusOverride.FocusPoint;
                return true;
            }
            
            if(ParentModel.TryGetElement(out HeroInteractionFocusOverride heroFocusOverride) && heroFocusOverride.FocusPoint != null) {
                focus = heroFocusOverride.FocusPoint;
                return true;
            }

            Location focusedLocation = FocusedLocation;
            if (focusedLocation == null) {
                focus = null;
                return false;
            }

            if (focusedLocation.TryGetElement(out DialogueAction dialogueAction) &&
                dialogueAction.ViewFocus != null) {
                focus = dialogueAction.ViewFocus;
                return true;
            }

            if (focusedLocation.TryGetElement(out IWithLookAt lookAtElement) && lookAtElement.LookAtTarget != null) {
                focus = lookAtElement.LookAtTarget;
                return true;
            }

            focus = null;
            return false;
        }

        protected override void OnInitialize() {
            HeroOverlapRecoveryHandler.AddOverlapRecoveryDisablingBlocker(this);
            
            if (_hideWeapons) {
                bool instantHide = Time.timeScale == 0;
                Hero?.Trigger(Hero.Events.HideWeapons, instantHide);
            }
            
            foreach (var heroSummon in World.All<NpcHeroSummon>()) {
                heroSummon.TryExitCombat();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            HeroOverlapRecoveryHandler.RemoveOverlapRecoveryDisablingBlocker(this);
        }
    }
}