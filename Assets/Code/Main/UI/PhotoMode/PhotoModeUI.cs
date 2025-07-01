using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.UI.HeroRendering;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.UI.PhotoMode {
    [SpawnsView(typeof(VModalBlocker), false)]
    [SpawnsView(typeof(VPhotoModeUI))]
    public partial class PhotoModeUI : Model, IPromptHost {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;
        
        // === Fields
        bool _isCameraToggled;
        Prompts _prompts;
        Prompt _toggleCameraPrompt;
        MarvinMode _marvinMode;
        bool _enabledUI = true;
        UIState _state;
        
        // === Properties
        public int AnimationPosesCount => Element<WorldHeroRenderer>().View<VWorldHeroRenderer>().AnimationPosesCount;
        public Transform PromptsHost => View<VPhotoModeUI>().PromptsHost;
        
        public new static class Events {
            public static readonly Event<PhotoModeUI, bool> UIToggled = new(nameof(UIToggled));
            public static readonly Event<PhotoModeUI, bool> CameraToggled = new(nameof(CameraToggled));
            public static readonly Event<PhotoModeUI, int> NextPoseChanged = new(nameof(NextPoseChanged));
            public static readonly Event<PhotoModeUI, int> PreviousPoseChanged = new(nameof(PreviousPoseChanged));
            public static readonly Event<PhotoModeUI, float> GammaChanged = new(nameof(GammaChanged));
            public static readonly Event<PhotoModeUI, float> ContrastChanged = new(nameof(ContrastChanged));
            public static readonly Event<PhotoModeUI, float> SaturationChanged = new(nameof(SaturationChanged));
        }

        // === Initialization
        protected override void OnInitialize() {
            UIUtils.HideUI();
            Hero.Current.Hide();
            AddElement(new WorldHeroRenderer(true));
            World.Only<GameCamera>().SetIgnoreTimescale(true);
            
            this.ListenTo(VModalBlocker.Events.ModalDismissed, Close, this);
            ToggleUIState(true);
        }

        protected override void OnFullyInitialized() {
            InitPrompts();
        }
        
        void InitPrompts() {
            _prompts = AddElement(new Prompts(this));
            _prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), Close, Prompt.Position.Last), this);
            _prompts.AddPrompt(Prompt.Tap(KeyBindings.Gameplay.PhotoMode.ToggleUI, LocTerms.UIGenericToggleUI.Translate(), ToggleUI), this);
            if (CheatController.CheatsEnabled()) {
                _toggleCameraPrompt = _prompts.AddPrompt(Prompt.Tap(KeyBindings.Gameplay.PhotoMode.ToggleCamera, LocTerms.FreeCamera.Translate(), ToggleCamera), this);
            }
        }

        void ToggleCamera() {
            _isCameraToggled = !_isCameraToggled;
            _toggleCameraPrompt.ChangeName(_isCameraToggled
                ? LocTerms.CharacterCamera.Translate()
                : LocTerms.FreeCamera.Translate());

            this.Trigger(Events.CameraToggled, true);
        }

        void ToggleUI() {
            ToggleUIState(!_enabledUI);
        }

        void ToggleUIState(bool uiEnabled) {
            _enabledUI = uiEnabled;
            if (_state != null) {
                UIStateStack.Instance.RemoveState(_state);
            }
            _state = (_enabledUI ? UIState.Cursor : UIState.BlockInput).WithPauseTime();
            UIStateStack.Instance.PushState(_state, this);
            
            this.Trigger(Events.UIToggled, _enabledUI);
        }

        public void NextPoseChanged(int poseIndex) {
            this.Trigger(Events.NextPoseChanged, poseIndex);
        }
        
        public void PreviousPoseChanged(int poseIndex) {
            this.Trigger(Events.PreviousPoseChanged, poseIndex);
        }

        void Close() {
            if (Element<WorldHeroRenderer>().IsLoading) {
                return;
            }
            Discard();
        }

        protected override void OnBeforeDiscard() {
            World.Only<GameCamera>()?.SetIgnoreTimescale(false);
            Hero.Current.Show();
            UIUtils.ShowUI();
            base.OnBeforeDiscard();
        }
    }
}