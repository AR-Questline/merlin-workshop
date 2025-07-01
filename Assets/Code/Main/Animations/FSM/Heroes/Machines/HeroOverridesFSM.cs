using System;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Knockdown;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class HeroOverridesFSM : HeroAnimatorSubstateMachine {
        const string LayerName = "Overrides";

        public sealed override bool IsNotSaved => true;

        SynchronizedHeroSubstateMachine _head;
        public override string ParentLayerName => LayerName;
        public override HeroLayerType LayerType => HeroLayerType.Overrides;
        public override HeroStateType DefaultState => HeroStateType.None;
        public bool IsActive => AnimancerLayer.Weight > 0 && CurrentAnimatorState != null && CurrentAnimatorState.Type != HeroStateType.None;
        protected override bool CanBeDisabled => false;
        protected override bool CanBeUpdatedInSafeZone => true;
        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;

        // === Constructor
        public HeroOverridesFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }

        protected override void OnInitialize() {
            base.OnInitialize();
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadOverrides));
            
            AddState(new HeroNoneState());
            AddState(new HeroTPoseState());
            AddState(new HeroWeaponBuff());
            AddState(new HeroThrowableThrow());
            AddState(new HeroCustomInteractionAnimation());
            AddState(new HeroPraySuccess());

            if (!Hero.TppActive) {
                AddState(new FinisherState());
                AddState(new HeroPetSharg());
                AddState(new KnockdownEnter());
                AddState(new KnockdownAirLoop());
                AddState(new KnockdownHitGround());
                AddState(new KnockdownGroundLoop());
                AddState(new KnockdownEnd());
            }

            EnableFSM();
        }

        protected override void AttachListeners() {
            World.Only<ScreenShakesProactiveSetting>().ListenTo(Setting.Events.SettingRefresh, ScreenShakesToggled, this);
        }
        
        public override void SetCurrentState(HeroStateType stateType, float? overrideCrossFadeTime = null, Action<ITransition> onNodeLoaded = null) {
            _head.SetEnable(stateType != HeroStateType.None, CameraShakesIntensity, HeroNoneState.GetTransitionSpeed(overrideCrossFadeTime));
            base.SetCurrentState(stateType, overrideCrossFadeTime, onNodeLoaded);
        }
        
        protected override void UpdateLayerWeight() {
            if (CurrentAnimatorState == null || CurrentAnimatorState.Type == HeroStateType.None) {
                return;
            }
            base.UpdateLayerWeight();
        }

        protected override void OnEnteredState(HeroAnimatorState state) {
            if (state.Type == HeroStateType.None && Hero.TppActive) {
                Hero.Current.TryGetElement<LegsFSM>()?.UpdateAvatarMask();
            }
        }
    }
}