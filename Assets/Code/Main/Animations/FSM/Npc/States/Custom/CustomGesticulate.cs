using Awaken.Utility;
using System;
using System.Threading;
using Animancer;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Custom {
    public sealed partial class CustomGesticulate : NpcAnimatorState<NpcAnimatorSubstateMachine> {
        public override ushort TypeForSerialization => SavedModels.CustomGesticulate;

        public override NpcStateType Type => NpcStateType.CustomGesticulate;
        public override float EntryTransitionDuration => 0.5f;
        
        CancellationTokenSource _tokenSource;
        ARAssetReference _gestureClipRef;
        AnimancerState _lastGestureState;

        public override void Enter(float _, float? overrideCrossFadeTime, Action<ITransition> onNodeLoaded = null) {
            // Gestures are played explicitly
            onNodeLoaded?.Invoke(null);
        }

        public async UniTaskVoid PlayClip(ARAssetReference gestureClipRef) {
            _tokenSource?.Cancel();
            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;
            
            _gestureClipRef = gestureClipRef;
            var handle = _gestureClipRef.LoadAsset<AnimationClip>();
            if (!handle.IsDone) {
                await UniTask.WaitWhile(() => !handle.IsDone, cancellationToken: token).SuppressCancellationThrow();
            }
            if (handle.Result == null || token.IsCancellationRequested) {
                handle.Release();
                return;
            }
            
            var animationClip = handle.Result;
            _lastGestureState = CurrentState = AnimancerLayer.Play(animationClip, EntryTransitionDuration);
            NpcAnimancer.RebindAnimationRigging();
        }

        protected override void OnExit(bool restarted) {
            _tokenSource?.Cancel();
            _tokenSource = null;

            if (_lastGestureState != null) {
                DestroyState(_lastGestureState).Forget();
                _lastGestureState = null;
            }

            _gestureClipRef?.ReleaseAsset();
            _gestureClipRef = null;
            
            base.OnExit(restarted);
        }
        
        async UniTaskVoid DestroyState(AnimancerState stateToDestroy) {
            var npcAnimancer = ParentModel.NpcAnimancer;
            var animancerLayer = AnimancerLayer;
            bool isAnimancerActive = npcAnimancer != null && npcAnimancer.gameObject.activeInHierarchy && npcAnimancer.isActiveAndEnabled;
            await AsyncUtil.WaitWhile(this, () => isAnimancerActive && animancerLayer.Weight > 0);
            stateToDestroy.Destroy();
        }
    }
}