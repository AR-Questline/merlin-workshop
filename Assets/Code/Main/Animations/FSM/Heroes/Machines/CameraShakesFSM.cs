using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Modifiers;
using Awaken.TG.Main.Animations.FSM.Heroes.States.CameraShakes;
using Awaken.TG.Main.Animations.FSM.Heroes.States.CameraShakes.Dash;
using Awaken.TG.Main.Animations.FSM.Heroes.States.CameraShakes.Jump;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class CameraShakesFSM : HeroAnimatorSubstateMachine {
        const string LayerName = "CameraShakes";

        public sealed override bool IsNotSaved => true;

        // === Fields
        bool _enabled = true;

        // === Properties
        public override string ParentLayerName => LayerName;
        public override HeroLayerType LayerType => HeroLayerType.CameraShakes;
        public override HeroStateType DefaultState => HeroStateType.None;
        protected override bool CanBeDisabled => false;
        protected override bool CanBeUpdatedInSafeZone => true;
        
        
        // === Events
        public new static class Events {
            public static readonly Event<Hero, CameraShakeType> ShakeHeroCamera = new(nameof(ShakeHeroCamera));
            public static readonly Event<Hero, Vector2> DashHeroCamera = new(nameof(DashHeroCamera));
        }

        // === Constructor
        public CameraShakesFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }

        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();

            AddState(new HeroNoneState());
            // --- Shakes
            AddState(new CameraShakeLight());
            AddState(new CameraShakeMedium());
            AddState(new CameraShakeStrong());
            // --- Dashing
            AddState(new DashFront(HeroStateType.None));
            AddState(new DashFrontLeft(HeroStateType.None));
            AddState(new DashFrontRight(HeroStateType.None));
            AddState(new DashLeft(HeroStateType.None));
            AddState(new DashRight(HeroStateType.None));
            AddState(new DashBack(HeroStateType.None));
            AddState(new DashBackLeft(HeroStateType.None));
            AddState(new DashBackRight(HeroStateType.None));
            // --- Jumping
            AddState(new JumpStart());
            AddState(new JumpEndLight());
            AddState(new JumpEndStrong());

            EnableFSM();
            UpdateLayerWeightInternal();
            World.EventSystem.ListenTo(EventSelector.AnySource, LoadingScreenUI.Events.SceneInitializationEnded, this, _ => UpdateLayerWeightInternal());
        }

        protected override void AttachListeners() {
            ParentModel.ListenTo(Events.ShakeHeroCamera, ShakeCamera, this);
            ParentModel.ListenTo(Events.DashHeroCamera, DashCamera, this);
            World.Only<ScreenShakesProactiveSetting>().ListenTo(Setting.Events.SettingRefresh, s => UpdateLayerWeightInternal(s), this);
        }

        protected override void UpdateLayerWeight() {
            UpdateLayerWeightInternal(updateSettingValues: false);
        }
        
        void UpdateLayerWeightInternal(Setting setting = null, bool updateSettingValues = true) {
            if (updateSettingValues) {
                ScreenShakesProactiveSetting screenShakesSetting = setting as ScreenShakesProactiveSetting;
                screenShakesSetting ??= World.Any<ScreenShakesProactiveSetting>();
                if (screenShakesSetting != null) {
                    _cameraShakesEnabled = screenShakesSetting.Enabled;
                }
            }

            AnimancerLayer.SetWeight(_cameraShakesEnabled && _enabled ? CameraShakesIntensity : 0);
        }

        public void SetActive(bool active) {
            _enabled = active;
            UpdateLayerWeightInternal(updateSettingValues: false);
        }

        void ShakeCamera(CameraShakeType shakeType) {
            if (!_cameraShakesEnabled) {
                return;
            }
            
            SetCurrentState(shakeType.AnimatorStateType, 0f);
        }

        void DashCamera(Vector2 dashDirection) {
            if (!_cameraShakesEnabled) {
                return;
            }
            
            SetCurrentState(DetermineDashState(dashDirection));
        }

        public static HeroStateType DetermineDashState(Vector2 dashDirection) {
            float horizontal = dashDirection.x;
            float vertical = dashDirection.y;

            return vertical switch {
                >= 0.5f when horizontal >= 0.5f => HeroStateType.DashFrontRight,
                >= 0.5f when horizontal <= -0.5f => HeroStateType.DashFrontLeft,
                >= 0.5f when horizontal is <= 0.5f and >= -0.5f => HeroStateType.DashFront,
                <= -0.5f when horizontal >= 0.5f => HeroStateType.DashBackRight,
                <= -0.5f when horizontal <= -0.5f => HeroStateType.DashBackLeft,
                _ => horizontal switch {
                    >= 0.5f => HeroStateType.DashRight,
                    <= -0.5f => HeroStateType.DashLeft,
                    _ => HeroStateType.DashBack
                }
            };
        }
    }
}