using System.Linq;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Graphics.Cutscenes {
    [UsesPrefab("UI/Video/VCutsceneSkipPauseHandler")]
    public class VCutsceneSkipPauseHandler : View<Cutscene>, IUIAware, IAutoFocusBase, IFocusSource {
        const string PauseSourceID = "vcutsceneskippausehandler.pause";
        
        [BoxGroup("Prompt"), SerializeField] CanvasGroup skipButton;
        [BoxGroup("Prompt"), SerializeField] Image fillImage;
        [BoxGroup("Prompt"), SerializeField] KeyIcon skipKeyIcon;
        [SerializeField] GameObject pauseOverlay;
        
        Sequence _sequence;
        Sequence _anyButtonSequence;
        bool _stopped;
        bool _isShown;
        bool _mouseHeld;
        
        public bool ForceFocus => true;
        public Component DefaultFocus => this;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        // === Initialization
        protected override void OnInitialize() {
            fillImage.fillAmount = 0f;
            if (Target.AllowSkip) {
                skipKeyIcon.Setup(new KeyIcon.Data(KeyBindings.UI.Items.SelectItem, true), this);
                Target.GetOrCreateTimeDependent().WithUpdate(OnUpdate).ThatProcessWhenPause();
            } else if (skipButton != null) {
                skipButton.alpha = 0f;
            }
        }
        
        void OnUpdate(float deltaTime) {
            if (Target.Stopped) {
                return;
            }
            
            if (Target.AllowSkip) {
                UpdateSkipButton();
            }
        }
        
        void UpdateSkipButton() {
            // bool anyKey = RewiredHelper.Player.controllers.Keyboard.GetAnyButton() ||
            //               RewiredHelper.Player.controllers.Joysticks.Any(j => j.GetAnyButton());
            bool anyKey = false;
            bool playingSequence = _anyButtonSequence?.IsPlaying() ?? false;

            if (_mouseHeld && !_isShown) {
                ShowSkipButton();
            } else if (!_mouseHeld && !_isShown && anyKey && !playingSequence) {
                ShowAndHideSkipButton();
            } else if (fillImage.fillAmount <= 0.01f && _isShown && !playingSequence) {
                HideSkipButton();
            }

            fillImage.fillAmount += Time.unscaledDeltaTime * (_mouseHeld ? 1 : -1);
            if (fillImage.fillAmount >= 0.99f) {
                fillImage.fillAmount = 1f;
                Target.SkipCutscene();
            }
        }
        
        void ShowSkipButton() {
            _sequence?.Complete();
            _anyButtonSequence?.Complete();
            _sequence = DOTween.Sequence();
            _sequence.SetUpdate(true);
            _sequence.Append(DOTween.To(() => skipButton.alpha, a => skipButton.alpha = a, 1, 0.5f));
            _sequence.Play();
            _isShown = true;
        }

        void HideSkipButton() {
            _sequence?.Complete();
            _anyButtonSequence?.Complete();
            _sequence = DOTween.Sequence();
            _sequence.SetUpdate(true);
            _sequence.Append(DOTween.To(() => skipButton.alpha, a => skipButton.alpha = a, 0, 0.5f));
            _sequence.Play();
            _isShown = false;
        }

        void ShowAndHideSkipButton() {
            _sequence?.Complete();
            _anyButtonSequence?.Complete();
            _anyButtonSequence = DOTween.Sequence();
            _anyButtonSequence.SetUpdate(true);
            _anyButtonSequence.Append(DOTween.To(() => skipButton.alpha, a => skipButton.alpha = a, 1, 0.5f));
            _anyButtonSequence.Append(transform.DOMove(transform.position, 0.5f));
            _anyButtonSequence.Append(DOTween.To(() => skipButton.alpha, a => skipButton.alpha = a, 0, 0.5f));
            _anyButtonSequence.Play();
            _isShown = false;
        }

        // === Input handling
        public UIResult Handle(UIEvent evt) {
            bool changePauseStatus = RewiredHelper.IsGamepad ? evt is UIKeyDownAction keyUpAction && keyUpAction.Name == KeyBindings.UI.Generic.Menu : Input.GetKeyDown(KeyCode.Escape);
            if (changePauseStatus) {
                var globalTime = World.Only<GlobalTime>();
                
                if (globalTime.GetTimeScale() > 0) {
                    globalTime.AddTimeModifier(new OverrideTimeModifier(PauseSourceID, 0));
                    Target.AddElement(new AudioMuter());
                    pauseOverlay.SetActiveOptimized(true);
                } else {
                    globalTime.RemoveTimeModifiersFor(PauseSourceID);
                    Target.RemoveElementsOfType<AudioMuter>();
                    pauseOverlay.SetActiveOptimized(false);
                }

                return UIResult.Accept;
            }
            if (!Target.TakeAwayControl) {
                return UIResult.Ignore;
            }
            if (Target.Stopped || !Target.AllowSkip) {
                return UIResult.Prevent;
            }
            
            if (evt is ISubmit) {
                _mouseHeld = true;
            } else if (evt is UIEMouseUp || (evt is UIKeyUpAction action && action.Name == KeyBindings.UI.Generic.Accept)) {
                _mouseHeld = false;
            }
            return UIResult.Prevent;
        }
        
        
        // === Discarding
        protected override IBackgroundTask OnDiscard() {
            World.Only<GlobalTime>().RemoveTimeModifiersFor(PauseSourceID);
            Target.GetTimeDependent()?.WithoutUpdate(OnUpdate);
            return base.OnDiscard();
        }
    }
}
