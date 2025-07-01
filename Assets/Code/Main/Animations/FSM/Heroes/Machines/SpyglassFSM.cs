using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Interactions;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Spyglass;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class SpyglassFSM : HeroAnimatorSubstateMachine {
        const string LayerName = "Spyglass";

        public sealed override bool IsNotSaved => true;

        float _spyglassInteractionCooldown;
        SynchronizedHeroSubstateMachine _head;

        public override string ParentLayerName => LayerName;
        public override HeroLayerType LayerType => HeroLayerType.Spyglass;
        public override HeroStateType DefaultState => HeroStateType.EquipWeapon;
        protected override bool CanBeUpdatedInSafeZone => true;
        protected override bool CanBeDisabled => true;
        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;

        bool ShouldInteractWithSpyglass => _spyglassInteractionCooldown <= 0 && IsAttackUp();
        bool IsInZoom => CurrentStateType == HeroStateType.ToolInteract;

        // === Constructor
        public SpyglassFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }

        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadSpyglass));

            AddState(new EmptyState());
            AddState(new SpyglassIdle());
            AddState(new MovementState());
            AddState(new SpyglassInteraction());
            AddState(new SpyglassExit());
            AddState(new EquipWeapon());
            AddState(new UnEquipWeapon());
        }

        protected override void AttachListeners() {
            base.AttachListeners();
            ParentModel.ListenTo(HeroToolAction.Events.HeroToolInteracted, OnToolInteraction, this);
        }

        protected override void OnUpdate(float deltaTime) {
            if (!_isMapInteractive || CurrentAnimatorState == null) {
                return;
            }

            if (_spyglassInteractionCooldown > 0) {
                _spyglassInteractionCooldown -= deltaTime;
            }

            if (ShouldInteractWithSpyglass) {
                ToggleSpyglass();
                return;
            }

            CurrentAnimatorState.Update(deltaTime);
        }

        void ToggleSpyglass() {
            _spyglassInteractionCooldown = 0.5f;
            SetCurrentState(IsInZoom ? HeroStateType.SpyglassExit : HeroStateType.ToolInteract);
        }

        void OnToolInteraction() {
            Spyglass.TryPlaceMarker();
        }
    }
}