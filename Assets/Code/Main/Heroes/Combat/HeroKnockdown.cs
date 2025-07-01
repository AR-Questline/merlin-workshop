using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.States;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class HeroKnockdown : Element<Hero>, IUIStateSource {
        public sealed override bool IsNotSaved => true;

        // === Fields
        readonly float _groundLoopDuration;
        bool _isInGroundLoop;
        float _groundLoopTimer;
        HeroOverridesFSM _cachedHeroOverridesFSM;
        CameraShakesFSM _cachedCameraShakesFSM;
        LegsFSM _cachedLegsFSM;
        HeroKnockdownMovement _knockdownMovement;

        // === Properties
        public GroundedPosition LookAt { get; }
        public Vector3 ForceDirection { get; }
        public float ForceStrength { get; }
        public AnimationCurve KnockdownCurve { get; }
        public UIState UIState => UIState.BlockInput.WithHeroBars(true);
        HeroOverridesFSM OverridesFSM => ParentModel.CachedElement(ref _cachedHeroOverridesFSM);
        CameraShakesFSM CameraShakesFSM => ParentModel.CachedElement(ref _cachedCameraShakesFSM);
        LegsFSM LegsFSM => ParentModel.CachedElement(ref _cachedLegsFSM);
        HeroAnimatorSubstateMachine KnockdownFSM => Hero.TppActive ? LegsFSM : OverridesFSM;
        
        // === Constructing
        public static void EnterKnockdown(ICharacter damageDealer, Vector3 forceDirection, float forceStrength,
            float groundLoopDuration = 0.25f) {
            if (Hero.Current.HasElement<HeroKnockdown>()) {
                return;
            }

            Hero.Current.AddElement(new HeroKnockdown(damageDealer, forceDirection, forceStrength, groundLoopDuration));
        }
        
        HeroKnockdown(ICharacter damageDealer, Vector3 forceDirection, float forceStrength, float groundLoopDuration) {
            LookAt = GroundedPosition.ByGrounded(damageDealer);
            ForceDirection = forceDirection;
            ForceStrength = forceStrength;
            KnockdownCurve = GameConstants.Get.heroKnockDownForceCurve;
            _groundLoopDuration = groundLoopDuration;
        }
        
        // === Initialization
        protected override void OnInitialize() {
            if (!Hero.TppActive) {
                ParentModel.Hide();
            }

            CameraShakesFSM.SetActive(false);
            KnockdownFSM.SetCurrentState(HeroStateType.KnockdownEnter);
            if (ParentModel.MovementSystem is MountedMovement mountedMovement) {
                mountedMovement.Dismount();
            }
            ParentModel.TrySetMovementType(out _knockdownMovement);
            ParentModel.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
        }

        // === LifeCycle
        void OnUpdate(float deltaTime) {
            if (ParentModel.ShouldDie) {
                Discard();
                return;
            }
            
            if (!_isInGroundLoop) {
                return;
            }
            
            _groundLoopTimer += deltaTime;
            
            if (_groundLoopTimer >= _groundLoopDuration) {
                KnockdownFSM.SetCurrentState(HeroStateType.KnockdownEnd);
                _knockdownMovement?.OnStartExitKnockdown();
                _isInGroundLoop = false;
            }
        }

        // === Public API
        public void KnockdownGroundLoopStarted() {
            _isInGroundLoop = true;
            _groundLoopTimer = 0;
        }

        public void KnockdownEnded() {
            Discard();
        }
        
        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            _knockdownMovement = null;
            if (fromDomainDrop) {
                return;
            }
            
            CameraShakesFSM.SetActive(true);
            ParentModel.ReturnToDefaultMovement();
            ParentModel.GetTimeDependent()?.WithoutUpdate(OnUpdate);
            ParentModel.Show();
        }
    }
}