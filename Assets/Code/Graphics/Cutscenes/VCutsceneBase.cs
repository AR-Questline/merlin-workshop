using System.Threading;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Animations;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.Cutscenes {
    public abstract class VCutsceneBase : View<Cutscene> {
        [SerializeField] protected bool pauseGame = true;
        [SerializeField, FoldoutGroup("Start")] protected bool useStartTransition = true;
        [SerializeField, Range(0f, 10f), FoldoutGroup("Start"), Tooltip("How long wait in black before starting ToCameraDuration is used")]
        protected float blackWaitDuration = 1f;
        [SerializeField, Range(0f, 10f), FoldoutGroup("End"), Tooltip("How long is transition to black at end of cutscene"), ShowIf(nameof(ShowToBlackAtEndDuration))]
        protected float toBlackAtEndDuration = 0.5f;
        [SerializeField, Range(0f, 10f), FoldoutGroup("End"), Tooltip("How long to stay in black at the end of cutscene")]
        protected float blackWaitAtEndDuration = 0f;
        [SerializeField, Range(0f, 10f), FoldoutGroup("End"), Tooltip("How logn to wait after blackToWait and before ToCameraDuration")]
        protected float waitDuration = 1f;
        [SerializeField, Range(0f, 10f), FoldoutGroup("Special End"), Tooltip("Used when triggering portal at end or when cutscene is skipped")]
        protected float toBlackDuration = 0.5f;
        [SerializeField, Range(0f, 10f), FoldoutGroup("Both"), Tooltip("How long is transition from black to camera")] 
        protected float toCameraDuration = 0.5f;

        [SerializeField] protected bool useBlinkingEffect;
        [SerializeField, ShowIf(nameof(useBlinkingEffect))] protected TransitionBlinking.Data blinkingData;
        [SerializeField] Transform positionToTeleportOnSkip;
        
        protected WeakModelRef<DirectTimeMultiplier> _timeMultiplier;
        protected CancellationTokenSource _cancellationTokenSource;
        protected bool _isPlaying, _stopped;
        protected float _timeElapsed;
        bool _skipped;
        
        bool ShowToBlackAtEndDuration => blackWaitAtEndDuration > 0;

        public CinemachineVirtualCamera CutsceneCamera { get; protected set; }
        public Transform RootSocket { get; protected set; }
        public Transform TeleportHeroPosition => _skipped && positionToTeleportOnSkip != null
            ? positionToTeleportOnSkip
            : TeleportHeroTo;
        public override Transform DetermineHost() => Services.Get<ViewHosting>().LocationsHost(Target.CurrentDomain);
        protected virtual float ToCameraAwaitDuration => toCameraDuration;
        protected abstract Transform TeleportHeroTo { get; }
        
        // === Initialization
        protected override void OnInitialize() {
            if (pauseGame) {
                var timeMultiplier = new DirectTimeMultiplier(0, ID);
                World.Only<GlobalTime>().AddTimeModifier(timeMultiplier);
                _timeMultiplier = timeMultiplier;
            }
            _isPlaying = false;
            Load().Forget();
        }

        protected abstract UniTaskVoid Load();
        
        // === Playing Animation
        protected abstract void ProcessUpdate(float deltaTime);
        
        // === Transitions
        protected async UniTaskVoid StartTransition() {
            Target.CutsceneStarted();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            
            var transitionService = World.Services.Get<TransitionService>();
            if (!useStartTransition) {
                transitionService.ToCamera(0, 0, true, null).Forget();
                _isPlaying = true;
                return;
            }
            
            // --- Wait
            if (!await AsyncUtil.DelayTime(gameObject, blackWaitDuration, true)) {
                return;
            }
            // --- Start animation
            transitionService.ToCamera(toCameraDuration, 0, true, useBlinkingEffect ? blinkingData : null).Forget();
            if (!await AsyncUtil.DelayTime(this, ToCameraAwaitDuration, pauseGame, _cancellationTokenSource)) {
                return;
            }
            _isPlaying = true;
        }

        protected async UniTask<bool> StopTransition(bool skipCutscene = false) {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            
            _isPlaying = false;
            
            var transitionService = World.Services.Get<TransitionService>();
            if (skipCutscene || Target.IsTriggeringPortalOnExit) {
                // --- To black
                await transitionService.ToBlack(toBlackDuration);
                // --- Wait
                if (this == null) {
                    if (Target is { HasBeenDiscarded: false }) {
                        Target.Discard();
                    }
                    return false;
                }
            }

            _timeMultiplier.Get()?.Remove();
            if (!await AsyncUtil.DelayFrame(Target)) {
                return false;
            }
            
            if (blackWaitAtEndDuration > 0) {
                await transitionService.ToBlack(toBlackAtEndDuration);
                if (!await AsyncUtil.DelayTime(Target, blackWaitAtEndDuration, true)) {
                    return false;
                }
            }
            
            Target.OnCutsceneEnd();
            RootSocket.gameObject.SetActive(false);

            if (!await AsyncUtil.DelayTime(Target, waitDuration, true)) {
                return false;
            }
            // --- To Camera
            if (!Target.IsTriggeringPortalOnExit) {
                if (blackWaitAtEndDuration > 0) {
                    transitionService.ToCamera(toCameraDuration).Forget();
                } else {
                    await transitionService.ToCamera(toCameraDuration);
                }
                if (this == null || Target is not { HasBeenDiscarded: false }) {
                    return false;
                }
            }
            // --- Fail means Target was discarded so we shouldn't invoke discard on it again
            Target.Discard();
            return true;
        }

        // === Skipping
        public async UniTaskVoid SkipCutsceneWithTransition() {
            if (!_stopped) {
                _stopped = true;
                _skipped = true;
                await StopTransition(true);
            }
        }

        protected override IBackgroundTask OnDiscard() {
            _timeMultiplier.Get()?.Remove();
            return base.OnDiscard();
        }
    }
}
