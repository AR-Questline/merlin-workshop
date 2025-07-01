using System.Collections.Generic;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Gliding {
    public partial class HeroGlideAction : Element<Hero>, IUIPlayerInput {
        const float TrackContinuanceMargin = 0.5f;

        public sealed override bool IsNotSaved => true;

        public IEnumerable<KeyBindings> PlayerKeyBindings => KeyBindings.Gameplay.Jump.Yield();

        HeroGlideUI _glideUI;
        float _lastGroundHeight = float.MinValue;
        float _deltaHeightToContinueTracking;
        bool _trackUpwardGroundHeight = true;
        
        public GliderItem Glider { get; }
        public bool CanStartGliding { get; private set; }
        public bool IsGliding => ParentModel.HasMovementType(MovementType.Glider);

        public HeroGlideAction(GliderItem item) {
            Glider = item;
        }
        
        public new class Events {
            public static readonly Event<Hero, bool> GlideStateChanged = new(nameof(GlideStateChanged));
            public static readonly Event<Hero, bool> GlideAvailabilityChanged = new(nameof(GlideAvailabilityChanged));
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(Hero.Events.HeroJumped, OnHeroJumped, this);
            ParentModel.AfterFullyInitialized(AfterParentFullyInitialized);
            World.Only<PlayerInput>().RegisterPlayerInput(this, this);
        }

        void OnHeroJumped() {
            _trackUpwardGroundHeight = false;
            _deltaHeightToContinueTracking = ParentModel.Data.jumpHeight + TrackContinuanceMargin;
        }

        void AfterParentFullyInitialized() {
            ParentModel.GetOrCreateTimeDependent().WithUpdate(Update);
        }
        
        public bool TryStartGliding() {
            if (CanStartGliding) {
                ParentModel.TrySetMovementType<GliderMovement>(false);
                ParentModel.Trigger(Events.GlideStateChanged, true);
                return true;
            }

            return false;
        }

        public bool TryEndGliding() {
            if (IsGliding) {
                (ParentModel.MovementSystem as GliderMovement)?.EndGliding();
                ParentModel.Trigger(Events.GlideStateChanged, false);
                return true;
            }

            return false;
        }

        public bool TryToggleGliding() {
            return TryStartGliding() || TryEndGliding();
        }

        void Update(float deltaTime) {
            UpdateGroundHeightTracking();
            CheckConditionsForGliding();
        }
        
        void UpdateGroundHeightTracking() {
            float movementDelta = ParentModel.Coords.y - _lastGroundHeight;
            
            if (ParentModel.Grounded || movementDelta > _deltaHeightToContinueTracking) {
                _trackUpwardGroundHeight = true;
            }
            
            bool goingUp = movementDelta > 0f;
            
            if (ParentModel.Grounded || (goingUp && _trackUpwardGroundHeight)) {
                _lastGroundHeight = ParentModel.Coords.y;
            }
        }

        void CheckConditionsForGliding() {
            bool previousGlideAvailability = CanStartGliding;

            bool couldGlide = !ParentModel.Grounded && !ParentModel.IsSwimming && !IsGliding;
            
            if (couldGlide) {
                CanStartGliding = IsHighEnoughToGlide() && IsFallingFastEnoughToGlide() &&
                                  !ParentModel.HasElement<GravityMarker>();
            } else {
                CanStartGliding = false;
            }

            if (previousGlideAvailability != CanStartGliding) {
                UpdateGlideUIVisibility();
                ParentModel.Trigger(Events.GlideAvailabilityChanged, CanStartGliding);
            }
        }
        
        bool IsHighEnoughToGlide() {
            var ray = new Ray(ParentModel.Coords, Vector3.down);
            var currentJumpHeight = Mathf.Max(0f, ParentModel.Coords.y - _lastGroundHeight);
            var maxRayDistance = Glider.Attachment.minHeightToStartGliding + currentJumpHeight;
            return !Ground.Raycast(ray, out _, out _, maxRayDistance, Ground.LayerMask | RenderLayers.Mask.Water);
        }

        bool IsFallingFastEnoughToGlide() {
            float verticalVelocity = ParentModel.VHeroController.verticalVelocity;
            return verticalVelocity < -Glider.Attachment.minDownVelocityToStartGliding;
        }

        void UpdateGlideUIVisibility() {
            if (CanStartGliding && _glideUI == null) {
                _glideUI = ParentModel.AddElement<HeroGlideUI>();
            } else if (!CanStartGliding && _glideUI != null) {
                _glideUI.Discard();
                _glideUI = null;
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel?.GetTimeDependent()?.WithoutUpdate(Update);
            TryEndGliding();
        }
        
        public UIResult Handle(UIEvent evt) {
            if (!UIStateStack.Instance.State.IsMapInteractive) {
                return UIResult.Ignore;
            }
            
            if (evt is UIKeyDownAction) {
                return TryToggleGliding() ? UIResult.Accept : UIResult.Ignore;
            }
            return UIResult.Ignore;
        }
    }
}