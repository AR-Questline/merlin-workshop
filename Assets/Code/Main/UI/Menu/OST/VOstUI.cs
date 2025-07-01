using System;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using FMOD.Studio;
using FMODUnity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Menu.OST {
    [UsesPrefab("TitleScreen/" + nameof(VOstUI))]
    public class VOstUI : View<OstUI>, IAutoFocusBase, IPromptHost, IUIAware {
        [SerializeField] ARFmodEventEmitter ostEmitter;
        [SerializeField] TrackInfo[] ostTracks = Array.Empty<TrackInfo>();
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI artistText;
        [SerializeField] TextMeshProUGUI currentTimeText;
        [SerializeField] TextMeshProUGUI totalTimeText;
        [SerializeField] Slider progressSlider;
        [SerializeField] MenuUIButton previousButton;
        [SerializeField] MenuUIButton playButton;
        [SerializeField] MenuUIButton pauseButton;
        [SerializeField] MenuUIButton nextButton;
        [SerializeField] Transform promptHost;
        
        Prompt _playPrompt;
        bool _isPaused;
        int _lastPositionBeforePause;
        int _currentTrackIndex;
        float _trackTotalSeconds;
        
        public Transform PromptsHost => promptHost;
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnFullyInitialized() {
            InitButtons();
            InitPrompts();
            PlayCurrentTrack();
            progressSlider.value = 0;
            RefreshPlayComponents();
        }

        protected override void OnMount() {
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, this, Target));
        }

        void InitButtons() {
            previousButton.button.OnClick += PreviousTrack;
            playButton.button.OnClick += TogglePause;
            pauseButton.button.OnClick += TogglePause;
            nextButton.button.OnClick += NextTrack;
        }

        void InitPrompts() {
            var prompts = Target.AddElement(new Prompts(this));
            _playPrompt = prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Accept, LocTerms.Pause.Translate(), TogglePause, controllers: ControlSchemeFlag.Gamepad), Target);
            prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Generic.IncreaseValue, LocTerms.PreviousNextTrack.Translate(), controllers: ControlSchemeFlag.Gamepad), Target);
            prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), Close), Target);
        }

        void Update() {
            // if (ostEmitter.EventInstance.isValid()) {
            //     ostEmitter.EventInstance.getTimelinePosition(out int ms);
            //     float seconds = ms / 1000f;
            //
            //     
            //
            //     currentTimeText.SetText(FormatTime(seconds));
            //     progressSlider.value = seconds / _trackTotalSeconds;
            // } else {
            //     NextTrack();
            // }
        }

        [Button]
        void PlayCurrentTrack() {
            // if (ostEmitter.IsPlaying()) {
            //     ostEmitter.Stop();
            // }
            //
            // TrackInfo trackInfo = ostTracks[_currentTrackIndex];
            // ostEmitter.EventReference = trackInfo.track;
            // ostEmitter.Play();
            //
            // _isPaused = false;
            // _trackTotalSeconds = GetTrackLengthSeconds();
            //
            // RefreshPlayComponents();
            // totalTimeText.SetText(FormatTime(_trackTotalSeconds));
            // titleText.SetText(trackInfo.title);
            // artistText.SetText(trackInfo.artist);
        }

        [Button]
        void TogglePause() {
            // _isPaused = !_isPaused;
            // RefreshPlayComponents();
            //
            // if (_isPaused) {
            //     ostEmitter.EventInstance.getTimelinePosition(out _lastPositionBeforePause);
            // } else {
            //     ostEmitter.EventInstance.setTimelinePosition(_lastPositionBeforePause);
            // }
            //
            // ostEmitter.EventInstance.setPaused(_isPaused);
        }

        [Button]
        void NextTrack() {
            _currentTrackIndex = (_currentTrackIndex + 1) % ostTracks.Length;
            PlayCurrentTrack();
        }

        [Button]
        void PreviousTrack() {
            _currentTrackIndex = (_currentTrackIndex - 1 + ostTracks.Length) % ostTracks.Length;
            PlayCurrentTrack();
        }
        
        void RefreshPlayComponents() {
            _playPrompt.ChangeName(_isPaused ? LocTerms.Play.Translate() : LocTerms.Pause.Translate());
            playButton.TrySetActiveOptimized(_isPaused);
            pauseButton.TrySetActiveOptimized(!_isPaused);
        }

        // float GetTrackLengthSeconds() {
        //     ostEmitter.EventInstance.getDescription(out EventDescription desc);
        //     desc.getLength(out int lengthMs);
        //     return lengthMs / 1000f;
        // }

        static string FormatTime(float seconds) {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }
        
        public UIResult Handle(UIEvent evt) {
            switch (evt) {
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.IncreaseValue:
                    NextTrack();
                    return UIResult.Accept;
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.DecreaseValue:
                    PreviousTrack();
                    return UIResult.Accept;
                default:
                    return UIResult.Ignore;
            }
        }

        void Close() {
            //ostEmitter.Stop();
            Target.Discard();
        }

        [Serializable]
        struct TrackInfo {
            public string title;
            public string artist;
            public EventReference track;
        }
    }
}