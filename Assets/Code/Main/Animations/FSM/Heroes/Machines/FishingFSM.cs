using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Fishing;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class FishingFSM : HeroAnimatorSubstateMachine {
        const string LayerName = "Fishing";

        public sealed override bool IsNotSaved => true;
        
        SynchronizedHeroSubstateMachine _head;

        public float fishingFightWeight;
        public override string ParentLayerName => LayerName;
        public override HeroLayerType LayerType => HeroLayerType.Fishing;
        public override HeroStateType DefaultState => HeroStateType.Idle;
        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;

        public new static class Events {
            public static readonly Event<Hero, Hero> StartThrow = new(nameof(StartThrow));
            public static readonly Event<Hero, Hero> Throw = new(nameof(Throw));
            public static readonly Event<Hero, Hero> BobberHitWater = new(nameof(BobberHitWater));
            public static readonly Event<Hero, Hero> StartFight = new(nameof(StartFight));
            public static readonly Event<Hero, Hero> Abort = new(nameof(Abort));
            public static readonly Event<Hero, Hero> PullOut = new(nameof(PullOut));
            public static readonly Event<Hero, Hero> Inspect = new(nameof(Inspect));
            public static readonly Event<Hero, Hero> Fail = new(nameof(Fail));
        }
        
        public FishingFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }

        protected override void OnInitialize() {
            base.OnInitialize();
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadFishing));
            
            AddState(new FishingEmptyIdle());
            AddState(new FishingThrow());
            AddState(new FishingIdle());
            AddState(new FishingCancel());
            AddState(new FishingBite());
            AddState(new FishingFail());
            AddState(new FishingFight());
            AddState(new FishingFightStart());
            AddState(new FishingInspect());
            AddState(new FishingPullOut());
            AddState(new FishingTakeFish());
            AddState(new FishingBiteLoop());
        }
        
        protected override void AttachListeners() {
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
            World.Only<ScreenShakesProactiveSetting>().ListenTo(Setting.Events.SettingRefresh, ScreenShakesToggled, this);
            ParentModel.ListenTo(HeroToolAction.Events.HeroToolInteracted, OnToolInteraction, this);
            ParentModel.ListenTo(CharacterFishingRod.Events.AbortFishing, OnAbortFishing, this);
        }

        protected override void OnUpdate(float deltaTime) {
            if (!_isMapInteractive || CurrentAnimatorState == null) {
                return;
            }

            if (ParentModel.Element<TwoHandedFSM>().GeneralStateType != HeroGeneralStateType.General) {
                SetCurrentState(HeroStateType.Idle, 0f);
            }
            
            base.OnUpdate(deltaTime);
        }

        void OnToolInteraction(bool _) {
            HeroStateType heroStateType = CurrentAnimatorState.Type;
            if (heroStateType == HeroStateType.Idle) {
                SetCurrentState(HeroStateType.FishingThrow, 0f);
            } else if (heroStateType == HeroStateType.FishingIdle) {
                SetCurrentState(HeroStateType.FishingCancel);
            } else if (heroStateType is HeroStateType.FishingBite or HeroStateType.FishingBiteLoop) {
                SetCurrentState(HeroStateType.FishingFightStart);
            }
        }

        void OnAbortFishing(Hero _) {
            SetCurrentState(HeroStateType.Idle, 0f);
        }
    }
}