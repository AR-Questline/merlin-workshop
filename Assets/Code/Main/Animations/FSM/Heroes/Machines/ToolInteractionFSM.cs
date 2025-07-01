using System;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Interactions;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class ToolInteractionFSM : HeroAnimatorSubstateMachine {
        const string LayerName = "Tools";
        [UnityEngine.Scripting.Preserve] const string HeadLayerName = "Tools_Head";

        public sealed override bool IsNotSaved => true;

        SynchronizedHeroSubstateMachine _head;
        public override string ParentLayerName => LayerName;
        public bool IsActive => AnimancerLayer.Weight > 0 && CurrentAnimatorState != null && CurrentAnimatorState.Type != HeroStateType.None;
        public bool IsInToolAnimation => CurrentStateType == HeroStateType.ToolInteract;
        public bool IsInInteractAnimation => CurrentStateType != HeroStateType.None && !IsInToolAnimation;
        public override HeroLayerType LayerType => HeroLayerType.Tools;
        public override HeroStateType DefaultState => HeroStateType.None;
        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;
        protected override bool CanBeUpdatedInSafeZone => true;
        protected override bool CanBeDisabled => false;

        // === Events
        public new static class Events {
            public static readonly Event<Hero, Hero> PetMount = new(nameof(PetMount));
            public static readonly Event<Hero, Hero> PatMount = new(nameof(PatMount));
            public static readonly Event<Hero, Hero> MountCalled = new(nameof(MountCalled));
        }

        // === Constructor
        public ToolInteractionFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }

        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadTools));
            
            AddState(new HeroNoneState());
            AddState(new ToolInteraction());
            AddState(new ToolMountPet());
            AddState(new ToolMountPat());
            AddState(new ToolWhistle());
            
            EnableFSM();
            ParentModel.ListenTo(HeroToolAction.Events.HeroToolInteracted, OnToolInteraction, this);
            ParentModel.ListenTo(Events.PetMount, OnPetHorse, this);
            ParentModel.ListenTo(Events.PatMount, OnPatHorse, this);
            ParentModel.ListenTo(Events.MountCalled, OnMountCalled, this);
        }

        public override void DisableFSM(bool fromDiscard = false) {
            if (IsLayerActive && !fromDiscard) {
                SetCurrentState(HeroStateType.None, 0);
            }
            base.DisableFSM(fromDiscard);
        }

        protected override void AttachListeners() {
            ParentModel.ListenTo(Hero.Events.HideWeapons, OnHideWeapons, this);
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
            World.Only<ScreenShakesProactiveSetting>().ListenTo(Setting.Events.SettingRefresh, ScreenShakesToggled, this);
        }
        
        protected override void OnHideWeapons(bool instant) {
            if (IsLayerActive) {
                SetCurrentState(HeroStateType.None, 0);
            }
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
        
        void OnMountCalled() {
            if (Hero.Current.Grounded) {
                SetCurrentState(HeroStateType.Whistle, Hero.TppActive ? 0 : null);
            }
        }

        void OnPetHorse() {
            SetCurrentState(HeroStateType.PetMount, Hero.TppActive ? 0 : null);
        }
        
        void OnPatHorse() {
            SetCurrentState(HeroStateType.PatMount, Hero.TppActive ? 0 : null);
        }

        void OnToolInteraction() {
            if (!(CurrentAnimatorState?.CanPerformNewAction ?? false)) {
                return;
            }

            if (Stamina.ModifiedValue <= 0) {
                return;
            }
            
            SetCurrentState(HeroStateType.ToolInteract);
        }
        
        protected override void OnEnteredState(HeroAnimatorState state) {
            if (state.Type == HeroStateType.None && Hero.TppActive) {
                Hero.Current.TryGetElement<LegsFSM>()?.UpdateAvatarMask();
            }
        }
    }
}