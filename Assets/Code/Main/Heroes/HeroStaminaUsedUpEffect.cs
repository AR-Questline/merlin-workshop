using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.CharacterCreators;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Rendering;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes {
    [SpawnsView(typeof(VHeroStaminaUsedUpEffect))]
    public partial class HeroStaminaUsedUpEffect : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroStaminaUsedUpEffect;

        const float StaminaValueToStopBeingZeroed = 10f;
        
        static VolumeWrapper PostProcess => World.Services.Get<SpecialPostProcessService>().VolumeStaminaUsedUp;
        [Saved] StatTweak _speedTweak;

        bool _wasZeroed;

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(() => AfterHeroFullyInitialized(true));
        }

        protected override void OnRestore() {
            ParentModel.AfterFullyInitialized(() => AfterHeroFullyInitialized(false));
        }

        void AfterHeroFullyInitialized(bool initialized) {
            if (initialized) {
                _speedTweak = StatTweak.Multi(ParentModel.CharacterStats.MovementSpeedMultiplier, 1f, TweakPriority.Multiply, this);
                World.EventSystem.ListenTo(EventSelector.AnySource, CharacterCreator.Events.CharacterCreated, this, c => ChangeGenderAudio(c.GetGender()));
            } else {
                _speedTweak.SetModifier(1f);
            }
            InitListeners();
        }

        void InitListeners() {
            ParentModel.ListenTo(Stat.Events.StatChanged(CharacterStatType.Stamina), OnStaminaChanged, this);
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
            ParentModel.AfterFullyInitialized(() => ChangeGenderAudio(ParentModel.GetGender()));
        }

        void OnUIStateChanged(UIState state) {
            if (!state.IsMapInteractive) {
                HideEffect(View<VHeroStaminaUsedUpEffect>());
            } else {
                OnStaminaChanged(ParentModel.Stamina);
            }
        }

        void OnStaminaChanged(Stat stat) {
            var view = View<VHeroStaminaUsedUpEffect>();
            var value = stat.ModifiedValue;
            if (value > 0) {
                if (_wasZeroed) {
                    _wasZeroed = value <= StaminaValueToStopBeingZeroed;
                }
                HideEffect(view);
            } else {
                _wasZeroed = true;
                ShowEffect(view);
            }
        }

        void HideEffect(VHeroStaminaUsedUpEffect view) {
            PostProcess.SetWeight(0f, 1f);
            _speedTweak.SetModifier(1f);
            view.StopFlash();
        }

        void ShowEffect(VHeroStaminaUsedUpEffect view) {
            PostProcess.SetWeight(1f, 1f);
            _speedTweak.SetModifier(0.5f);
            view.StartFlash();
        }

        void ChangeGenderAudio(Gender gender) {
            View<VHeroStaminaUsedUpEffect>().ChangeGenderAudio(gender);
        }
        
        // == Continuous changes

        public bool CanDecreaseContinuously() {
            return !_wasZeroed;
        }
        
        public bool TryDecreaseContinuously(float cost, float deltaTime) {
            if (_wasZeroed) {
                return false;
            }
            ParentModel.Stamina.DecreaseBy(cost * deltaTime);
            return true;
        }
    }
}