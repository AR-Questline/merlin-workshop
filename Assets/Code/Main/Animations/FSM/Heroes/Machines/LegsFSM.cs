using System.Threading;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.CameraShakes.Dash;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Knockdown;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Legs;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides;
using Awaken.TG.Main.Animations.FSM.Heroes.States.TPP;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class LegsFSM : HeroAnimatorSubstateMachine {
        const string LayerName = "Legs";
        public sealed override bool IsNotSaved => true;
        
        bool _weaponsHidden;
        CancellationTokenSource _weaponToggleToken;
        HeroStateType _previousStateType = HeroStateType.None;
        HeroOverridesFSM _heroOverridesFSM;
        ToolInteractionFSM _toolInteractionFSM;
        
        public override string ParentLayerName => LayerName;
        public override HeroLayerType LayerType => HeroLayerType.Legs;
        public override HeroStateType DefaultState => HeroStateType.Idle;
        protected override bool CanBeDisabled => !Hero.TppActive;
        protected override bool CanBeUpdatedInSafeZone => true;
        protected override AvatarMask AvatarMask {
            get {
                bool heroInvolved = World.HasAny<IHeroInvolvement>();
                bool noWeapons = ActiveTopBodyLayer() == this;
                bool finisher = CurrentStateType == HeroStateType.Finisher;
                if (heroInvolved || noWeapons || finisher || ParentModel.IsSwimming) {
                    return CommonReferences.Get.wholeBodyMask;
                }
                
                bool overrideActive = HeroOverridesFSM.IsActive;
                bool toolActive = ToolInteractionFSM.IsActive;
                return overrideActive || toolActive || WeaponsVisible()
                    ? base.AvatarMask
                    : CommonReferences.Get.wholeBodyMask;

                bool WeaponsVisible() {
                    var mainHandWeapon = ParentModel.MainHandWeapon;
                    var offHandWeapon = ParentModel.OffHandWeapon;
                    return (mainHandWeapon != null && mainHandWeapon.gameObject.activeInHierarchy) ||
                           (offHandWeapon != null && offHandWeapon.gameObject.activeInHierarchy);
                }
            }
        }
        
        public bool HeroLanded { get;private set; }
        public float VerticalVelocityOnLand { get; private set; }
        public bool ShouldCrouch => ParentModel.IsCrouching || ParentModel.IsStoryCrouching;
        HeroOverridesFSM HeroOverridesFSM => ParentModel.CachedElement(ref _heroOverridesFSM);
        ToolInteractionFSM ToolInteractionFSM => ParentModel.CachedElement(ref _toolInteractionFSM);

        // === Constructor
        public LegsFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }
        
        protected override void OnInitialize() {
            base.OnInitialize();
            
            AddState(new LegsIdle());
            AddState(new TppMovementState());
            // --- Jumping
            AddState(new LegsJumpStart());
            AddState(new LegsJumpLoop());
            AddState(new LegsJumpEnd());
            // --- Dashing
            AddState(new DashFront(HeroStateType.Idle));
            AddState(new DashFrontLeft(HeroStateType.Idle));
            AddState(new DashFrontRight(HeroStateType.Idle));
            AddState(new DashLeft(HeroStateType.Idle));
            AddState(new DashRight(HeroStateType.Idle));
            AddState(new DashBack(HeroStateType.Idle));
            AddState(new DashBackLeft(HeroStateType.Idle));
            AddState(new DashBackRight(HeroStateType.Idle));
            // --- Sliding
            AddState(new LegsSlide());
            // --- Knockdown
            AddState(new KnockdownEnter());
            AddState(new KnockdownAirLoop());
            AddState(new KnockdownHitGround());
            AddState(new KnockdownGroundLoop());
            AddState(new KnockdownEnd());
            
            // --- Custom
            AddState(new FinisherState());
            AddState(new HeroPetSharg());
            
            EnableFSM();
        }
        
        protected override void AttachListeners() {
            Hero.Current.ListenTo(CharacterHandBase.Events.WeaponVisibilityToggled, _ => OnWeaponVisibilityToggled().Forget(), this);
            Hero.Current.ListenTo(Hero.Events.HeroJumped, OnHeroJumped, this);
            Hero.Current.ListenTo(Hero.Events.HeroLanded, OnHeroLanded, this);
            Hero.Current.ListenTo(Hero.Events.HeroSlid, OnHeroSlid, this);
            Hero.Current.ListenTo(Hero.Events.DashForward, OnHeroDashedForward, this);
            Hero.Current.ListenTo(MountElement.Events.HeroMounted, OnHeroMounted, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<IHeroInvolvement>(), this, OnHeroDialogueInvolvementAdded);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<IHeroInvolvement>(), this, _ => AnimancerLayer.SetMask(AvatarMask));
            ParentModel.ListenTo(CameraShakesFSM.Events.DashHeroCamera, OnHeroDashed, this);
            base.AttachListeners();
        }

        void OnHeroDialogueInvolvementAdded(Model involvement) {
            AnimancerLayer.SetMask(AvatarMask);
            SetCurrentState(HeroStateType.Movement);
        }
        
        protected override void OnShowWeapons(bool instant) { }
        protected override void OnHideWeapons(bool instant) { }
        
        async UniTaskVoid OnWeaponVisibilityToggled() {
            _weaponToggleToken?.Cancel();
            _weaponToggleToken = new CancellationTokenSource();
            HeroAnimatorSubstateMachine topBodyLayer = ActiveTopBodyLayer();
            if (topBodyLayer == null) {
                AnimancerLayer.SetMask(AvatarMask);
                return;
            }
            
            if (!await AsyncUtil.WaitUntil(this, () => {
                    bool ready = topBodyLayer.CurrentAnimatorState?.CurrentState != null;
                    if (ready && topBodyLayer.EnableAdditionalLayer) {
                        ready = topBodyLayer.AdditionalSynchronizedLayer.AnyStatePlaying;
                    }
                    return ready;
                }, _weaponToggleToken)) {
                return;
            }
            AnimancerLayer.SetMask(AvatarMask);
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (!_isMapInteractive || CurrentAnimatorState == null) {
                base.OnUpdate(deltaTime);
                return;
            }

            if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.General) {
                GeneralStateUpdate(deltaTime);
            }
            
            if (CurrentAnimatorState is ISynchronizedAnimatorState) {
                Synchronize(ActiveTopBodyLayer()?.CurrentAnimatorState, CurrentAnimatorState, deltaTime);
            }
            
            base.OnUpdate(deltaTime);
        }

        void GeneralStateUpdate(float deltaTime) {
            if (HeroLanded && !ParentModel.Grounded && !ParentModel.IsSwimming) {
                SetCurrentState(HeroStateType.LegsJumpLoop, 0.33f);
                HeroLanded = false;
            }
        }

        protected override void OnEnteredState(HeroAnimatorState state) {
            if (state.Type == HeroStateType.Finisher || _previousStateType == HeroStateType.Finisher) {
                UpdateAvatarMask();
            }
            _previousStateType = state.Type;
        }

        void OnHeroJumped() {
            SetCurrentState(HeroStateType.LegsJumpStart);
            HeroLanded = false;
        }

        void OnHeroLanded(float verticalVelocity) {
            VerticalVelocityOnLand = verticalVelocity;
            HeroLanded = true;
        }

        void OnHeroSlid() {
            SetCurrentState(HeroStateType.LegsSlide);
        }

        void OnHeroDashed(Vector2 dashVector) {
            SetCurrentState(CameraShakesFSM.DetermineDashState(dashVector));
        }
        
        void OnHeroDashedForward(bool _) {
            SetCurrentState(HeroStateType.DashFront);
        }

        void OnHeroMounted(MountElement _) {
            SetCurrentState(HeroStateType.Idle, 0f);
        }
        
        // === Helpers
        public override float SynchronizedStateOffsetNormalizedTime() {
            if (!Hero.TppActive) {
                return 0;
            }

            var currentState = GetTopBodyAnimancerState();
            if (currentState == null) {
                return 0;
            }
            return AnimancerUtils.SynchronizeNormalizedTime(currentState, ParentModel.GetDeltaTime());
        }

        public void UpdateAvatarMask() {
            AnimancerLayer.SetMask(AvatarMask);
        }
    }
}