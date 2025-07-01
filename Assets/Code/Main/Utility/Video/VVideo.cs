using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.UI.Cursors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility.Graphics;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Video;

namespace Awaken.TG.Main.Utility.Video {
    [UsesPrefab("UI/Video/VVideo")]
    public class VVideo : View<Video> {
        // === References
        [BoxGroup("Video"), SerializeField] VideoPlayer videoPlayer;
        [BoxGroup("Video"), SerializeField] ARFmodEventEmitter videoAudioPlayer;
        [SerializeField] GameObject pauseOverlay;

        RenderTexture _videoRT;
        bool _videoFinished;

        public VideoPlayer VideoPlayer => videoPlayer;
        IVideoHost Host => Target.Host;
        
        // === State
        int ClipCount => Target.LoadingHandle.Length;
        bool RenderTextureCreated() => _videoRT != null;

        // === Initialization
        protected override void OnInitialize() {
            Target.ListenTo(Model.Events.BeforeDiscarded, () => BeforeDiscard().Forget(), this);
            videoPlayer.errorReceived += OnErrorReceived;
            CreateRenderTexture().Forget();
            
            _videoFinished = true;
            videoPlayer.prepareCompleted += OnPrepareCompleted;
            videoPlayer.loopPointReached += OnClipEndReached;
            
            Play().Forget();
        }

        async UniTaskVoid CreateRenderTexture() {
            if (!await AsyncUtil.WaitForPlayerLoopEvent(this, PlayerLoopTiming.PostLateUpdate)) {
                return;
            }

            _videoRT = TextureUtils.CreateRenderTextureFor(Host.VideoDisplay.rectTransform);
            Host.VideoDisplay.texture = _videoRT;
        }
        
        async UniTaskVoid Play() {
            await UniTask.WaitUntil(RenderTextureCreated);

            while (!Target.HasBeenDiscarded && !Target.IsLastClip) {
                await UniTask.WaitUntil(() => _videoFinished);

                if (Target.HasBeenDiscarded) {
                    return;
                }
                
                if (Target.TryMoveToNextClip()) {
                    StartIntro(Target.CurrentClip);
                } else {
                    await UniTask.WaitForEndOfFrame();
                }
            }
        } 

        // === Video logic
        void StartIntro(VideoClip clip) {
            Host.VideoTextureHolder.SetActive(true);
            videoPlayer.clip = clip;
            videoPlayer.targetTexture = _videoRT;

            videoPlayer.isLooping = Target.Loop;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            videoPlayer.Prepare();

            _videoFinished = false;

            if (Target.HideCursor && !Target.HasElement<ForceCursorVisibility>()) {
                Target.AddElement(new ForceCursorVisibility(false));
            }

            Host.OnVideoStarted();
        }

        void OnPrepareCompleted(VideoPlayer source) {
            source.Play();
            if (videoAudioPlayer && !Target.CurrentAudio.IsNull) {
                // videoAudioPlayer.PlayNewEventWithPauseTracking(Target.CurrentAudio);
            }

            if (Target.FadeIn is not Video.FadeInOptions.None) {
                InitialFade().Forget();
            }
        }

        public void Pause() {
            VideoPlayer.Pause();
            // videoAudioPlayer.Pause();
            pauseOverlay.SetActiveOptimized(true);
        }

        public void UnPause() {
            VideoPlayer.Play();
            // videoAudioPlayer.UnPause();
            pauseOverlay.SetActiveOptimized(false);
        }

        async UniTaskVoid InitialFade() {
            bool instant = Target.FadeIn.HasFlagFast(Video.FadeInOptions.ToCameraInstant);
            if (instant && !await AsyncUtil.DelayFrame(this, 3)) {
                return;
            }

            float transitionTime = instant ? 0.01f : TransitionService.DefaultFadeIn;
            switch (Target.FadeType) {
                case Video.TransitionType.Transition:
                    World.Services.Get<TransitionService>().ToCamera(transitionTime).Forget();
                    break;
                case Video.TransitionType.FadeIn:
                    World.Services.Get<TransitionService>().TransitionFromBlack(transitionTime).Forget();
                    break;
            }
        }

        void OnClipEndReached(VideoPlayer _) {
            if (ClipCount == 1 && Target.Loop) {
                return;
            }

            _videoFinished = true;

            if (Target.IsLastClip) {
                Target.Discard();
            }
        }

        // === Error Handling
        void OnErrorReceived(VideoPlayer player, string error) {
            Log.Important?.Error($"Failed to play video! Reason: {error}");
            Target.Discard();
        }

        // === Discard
        async UniTaskVoid BeforeDiscard() {
            if (Target.FadeOut.HasFlagFast(Video.FadeOutOptions.ToBlack)) {
                switch (Target.FadeType) {
                    case Video.TransitionType.Transition:
                        World.Services.Get<TransitionService>().SetToBlack();
                        break;
                    case Video.TransitionType.FadeIn:
                        World.Services.Get<TransitionService>().TransitionToBlack(0).Forget();
                        break;
                }
            }

            Host.VideoDisplay.texture = null;
            videoPlayer.prepareCompleted -= OnPrepareCompleted;
            videoPlayer.loopPointReached -= OnClipEndReached;

            videoPlayer.targetTexture = null;
            videoPlayer.Stop();
            videoPlayer.clip = null;

            _videoRT?.Release();
            _videoRT = null;

            foreach (var handle in Target.LoadingHandle) {
                handle.Release();
            }

            // videoAudioPlayer.Stop();

            Resources.UnloadUnusedAssets();

            // --- Target is discarded at this moment that's why we need to cache info about toBlackToCamera.
            var fadeType = Target.FadeType;
            if (Target.FadeOut.HasFlagFast(Video.FadeOutOptions.ToBlackToCamera)) {
                await UniTask.DelayFrame(2);
                if (World.Services.TryGet(out TransitionService transitionService)) {
                    switch (fadeType) {
                        case Video.TransitionType.Transition:
                            transitionService.ToCamera(TransitionService.DefaultFadeOut, 0.5f).Forget();
                            break;
                        case Video.TransitionType.FadeIn:
                            await UniTask.Delay(500);
                            if (transitionService && transitionService.gameObject) {
                                transitionService.TransitionFromBlack(TransitionService.DefaultFadeOut).Forget();
                            }

                            break;
                    }
                }
            }
        }
    }
}