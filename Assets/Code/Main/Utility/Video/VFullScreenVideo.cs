using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.Animations;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.Video {
    [UsesPrefab("UI/Video/VFullScreenVideo")]
    public class VFullScreenVideo : View<Video>, IVideoHost, IAutoFocusBase, IFocusSource, IUIPlayerInput {
        [BoxGroup("Video"), SerializeField] RawImage videoDisplay;
        [BoxGroup("Video"), SerializeField] GameObject videoTextureHolder;
        [BoxGroup("Video"), SerializeField] Transform subtitlesHost;
        [BoxGroup("Prompt"), SerializeField] CanvasGroup skipButton;
        [BoxGroup("Prompt"), SerializeField] Image fillImage;
        [BoxGroup("Prompt"), SerializeField] KeyIcon keyIcon;

        Sequence _sequence;
        Sequence _anyButtonSequence;
        bool _isShown;
        bool _mouseHeld;

        public RawImage VideoDisplay => videoDisplay;
        public GameObject VideoTextureHolder => videoTextureHolder;
        public Transform SubtitlesHost => subtitlesHost;
        public KeyIcon SkipKeyIcon => keyIcon;

        public bool ForceFocus => true;
        public Component DefaultFocus => this;

        IUIAware _listener;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            PrepareSkipButton();
        }

        void PrepareSkipButton() {
            fillImage.fillAmount = 0f;
            if (Target.AllowSkip) {
                SkipKeyIcon.Setup(new KeyIcon.Data(KeyBindings.UI.Items.SelectItem, true), this);
            } else if (skipButton != null) {
                skipButton.alpha = 0f;
            }
        }
        
        public IEnumerable<KeyBindings> PlayerKeyBindings {
            get {
                yield return KeyBindings.UI.Generic.Menu;
            }
        }

        // === Input handling
        public UIResult Handle(UIEvent evt) {
            bool changePauseStatus = RewiredHelper.IsGamepad ? evt is UIKeyDownAction keyUpAction && keyUpAction.Name == KeyBindings.UI.Generic.Menu : Input.GetKeyDown(KeyCode.Escape);
            if (changePauseStatus) {
                if (Target.IsPlaying) {
                    Target.Pause();
                } else {
                    Target.UnPause();
                }

                return UIResult.Accept;
            }
            
            if (!Target.IsPlaying || !Target.AllowSkip) {
                return UIResult.Ignore;
            }
            
            if (evt is ISubmit) {
                _mouseHeld = true;
            } else if (evt is UIEMouseUp || (evt is UIKeyUpAction action && action.Name == KeyBindings.UI.Generic.Accept)) {
                _mouseHeld = false;
            }
            return UIResult.Prevent;
        }

        void Update() {
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
                Target.Discard();
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

        public void OnVideoStarted() { }

        protected override IBackgroundTask OnDiscard() {
            _sequence?.Complete();
            _anyButtonSequence?.Complete();
            return base.OnDiscard();
        }
    }
}