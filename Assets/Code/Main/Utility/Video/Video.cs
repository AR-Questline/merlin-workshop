using System.Linq;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility.Video.Subtitles;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;
using UnityEngine.Video;

namespace Awaken.TG.Main.Utility.Video {
    [SpawnsView(typeof(VVideo))]
    public partial class Video : Model, IUIStateSource {
        VideoClip[] _videoClips;
        SubtitlesData[] _subtitleDataSet;
        int _videoIndex;
        
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;
        
        public IVideoHost Host { get; private set; }
        public LoadingHandle[] LoadingHandle { get; }
        public bool AllowSkip { get; }
        public bool HideCursor { get; }
        public bool Loop { get; }
        public TransitionType FadeType { get; }
        public FadeInOptions FadeIn { get; }
        public FadeOutOptions FadeOut { get; }
        public VideoClip CurrentClip { get; private set; }
        public SubtitlesData CurrentSubtitles { get; private set; }
        public EventReference CurrentAudio { get; private set; }
        bool MuteAudio { get; }
        IModel Owner { get; }
        
        public UIState UIState => UIState.ModalState(HUDState.EverythingHidden);
        public Transform SubtitlesHost => Host.SubtitlesHost;
        public bool IsPlaying => View<VVideo>().VideoPlayer.isPlaying;
        public bool IsFullScreen => Host is VFullScreenVideo;
        public bool IsLastClip => !Loop && _videoIndex >= LoadingHandle.Length;
        
        int ClipCount => LoadingHandle.Length;
        bool CanGetNext => HasValidClips && HasValidSubtitles;
        bool HasValidClips => _videoIndex < _videoClips.Length && _videoClips[_videoIndex] != null;
        bool HasValidSubtitles => !LoadingHandle[_videoIndex].subtitlesReference.IsSet || (LoadingHandle[_videoIndex].subtitlesReference.IsSet && _subtitleDataSet[_videoIndex]);
        
        Video(IVideoHost host, LoadingHandle loadingHandle, IModel owner, Config config, bool muteAudio, TransitionType fadeType, FadeInOptions fadeIn, FadeOutOptions fadeOut) {
            Host = host;
            LoadingHandle = new[] { loadingHandle };
            Owner = owner;
            AllowSkip = config.allowSkip;
            HideCursor = config.hideCursor;
            Loop = config.loop;
            MuteAudio = muteAudio;
            FadeType = fadeType;
            FadeIn = fadeIn;
            FadeOut = fadeOut;
        }

        Video(IVideoHost host, LoadingHandle[] handleSet, IModel owner, Config config, bool muteAudio, TransitionType fadeType, FadeInOptions fadeIn, FadeOutOptions fadeOut) {
            Host = host;
            LoadingHandle = handleSet;
            Owner = owner;
            AllowSkip = config.allowSkip;
            HideCursor = config.hideCursor;
            Loop = config.loop;
            MuteAudio = muteAudio;
            FadeType = fadeType;
            FadeIn = fadeIn;
            FadeOut = fadeOut;
        }
        
        public static Video FullScreen(LoadingHandle[] handleSet, IModel owner = null, bool muteAudio = true, TransitionType fadeType = TransitionType.Transition, FadeInOptions fadeIn = FadeInOptions.ToCamera, FadeOutOptions fadeOut = FadeOutOptions.None) {
            return new(null, handleSet, owner, new Config(), muteAudio, fadeType, fadeIn, fadeOut);
        }

        public static Video FullScreen(LoadingHandle handle, IModel owner = null, bool muteAudio = true, TransitionType fadeType = TransitionType.Transition, FadeInOptions fadeIn = FadeInOptions.ToCamera, FadeOutOptions fadeOut = FadeOutOptions.None) {
            return new(null, handle, owner, new Config(), muteAudio, fadeType, fadeIn, fadeOut);
        }

        public static Video Custom(IVideoHost host, LoadingHandle handle, Config config, IModel owner = null, TransitionType fadeType = TransitionType.Transition, FadeInOptions fadeIn = FadeInOptions.ToCamera, FadeOutOptions shouldFadeOut = FadeOutOptions.None) {
            return new(host, handle, owner, config, false, fadeType, fadeIn, shouldFadeOut);
        }
        
        public void Pause() {
            View<VVideo>().Pause();
        }
        
        public void UnPause() {
            View<VVideo>().UnPause();
        }

        protected override void OnInitialize() {
            Owner?.ListenTo(Events.AfterDiscarded, Discard, this);
            if (Host == null) {
                World.SpawnView<VModalBlocker>(this);
                Host = World.SpawnView<VFullScreenVideo>(this);
            }

            if (MuteAudio) {
                AddElement(new AudioMuter());
            }
        }

        protected override void OnFullyInitialized() {
            TryToAddSubtitles();
            GatherClips().Forget();
            GatherSubtitles().Forget();
        } 
        
        async UniTaskVoid GatherClips() {
            _videoClips = new VideoClip[ClipCount];
            for (int i = 0; i < LoadingHandle.Length; i++) {
                var clip = await LoadingHandle[i].GetClip();
                _videoClips[i] = clip;
            }
        }

        async UniTaskVoid GatherSubtitles() {
            _subtitleDataSet = new SubtitlesData[ClipCount];
            for (int i = 0; i < LoadingHandle.Length; i++) {
                var subtitles = await LoadingHandle[i].GetSubtitles();
                _subtitleDataSet[i] = subtitles;
            }
        }

        public bool TryMoveToNextClip() {
            if (!CanGetNext) {
                return false;
            }

            CurrentClip = _videoClips[_videoIndex];
            CurrentSubtitles = _subtitleDataSet[_videoIndex];
            CurrentAudio = LoadingHandle[_videoIndex].videoAudio;

            if (Loop && _videoIndex == LoadingHandle.Length - 1) {
                _videoIndex = 0;
            } else {
                _videoIndex++;
            }

            return true;
        }

        void TryToAddSubtitles() {
            if (LoadingHandle.Any(p => p.subtitlesReference.IsSet)) {
                AddElement(new VideoSubtitles(View<VVideo>().VideoPlayer));
            }
        }

        public class Config {
            public bool allowSkip = true;
            public bool hideCursor = true;
            public bool loop = false;
        }

        public enum TransitionType {
            Transition,
            FadeIn,
        }

        public enum FadeInOptions {
            None = 0,
            ToCamera = 1 << 0,
            ToCameraInstant = 1 << 1 | ToCamera,
        }

        public enum FadeOutOptions {
            None = 0,
            ToBlack = 1 << 0,
            ToBlackToCamera = 1 << 1 | ToBlack,
        }
    }
}