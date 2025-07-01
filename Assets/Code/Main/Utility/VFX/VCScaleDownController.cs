using System.Threading;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public class VCScaleDownController : ViewComponent<Location> {
        const float DefaultScale = 1f;
        const float MinimumScaleDown = 0.25f;
        const float MinimumScaleBack = 0.75f;
        
        [SerializeField, SuffixLabel("s", overlay: true)] float scalingDuration = 2.5f;
        [SerializeField, SuffixLabel("ms", overlay: true)] int delayBeforeStart = 1000;
        
        Transform _targetTransform;
        Vector3 _baseScale;
        float _transition;
        bool _discardOnDisappeared;
        bool _isScaling;
        IEventListener _appearEventListener;
        CancellationTokenSource _cts = new();

        bool IsCanceledOrDisappearing => _isScaling || _cts.IsCancellationRequested;

        protected override void OnAttach() {
            Target.OnVisualLoaded(OnVisualLoaded);
            if (Target.TryGetElement<TemporaryDeathElement>(out var temporaryDeathElement)) {
                temporaryDeathElement.ListenTo(TemporaryDeathElement.Events.TemporaryDeathStateChanged, dead => {
                    if (dead) {
                        StartShrinking().Forget();
                    } else {
                        ReturnToBaseScale();
                    }
                }, this);
            } else {
                _discardOnDisappeared = true;
                Target.TryGetElement<IAlive>()?.ListenTo(IAlive.Events.AfterDeath, _ => StartShrinking().Forget(), this);
            }
            
            Target.TryGetElement<DiscardParentAfterDuration>()?.ListenTo(DiscardParentAfterDuration.Events.DiscardingParent, DurationElapsed, this);
        }

        void DurationElapsed(HookResult<DiscardParentAfterDuration, Model> hook) {
            hook.Prevent();
            Shrink().Forget();
        }

        void OnVisualLoaded(Transform parentTransform) {
            _targetTransform = parentTransform;
            _baseScale = _targetTransform.localScale;
        }

        void ReturnToBaseScale() {
            CancelShrinking(); 
            ShrinkEffect(MinimumScaleBack, DefaultScale, _cts.Token).Forget();
        }

        async UniTaskVoid StartShrinking() {
            await UniTask.Delay(delayBeforeStart, cancellationToken: _cts.Token);
            if (_cts.IsCancellationRequested) {
                return;
            }

            await Shrink();

            if (_discardOnDisappeared) {
                Target.Discard();
            }
        }

        async UniTask Shrink() {
            if (IsCanceledOrDisappearing) {
                return;
            }

            _isScaling = true;
            await ShrinkEffect(DefaultScale, MinimumScaleDown, _cts.Token);
            _isScaling = false;
        }

        async UniTask ShrinkEffect(float startValue, float endValue, CancellationToken ct) {
            UpdateEffects(startValue);
            await DOTween.To(() => _transition, UpdateEffects, endValue, scalingDuration).SetEase(Ease.InCubic);//.WithCancellation(ct);
            UpdateEffects(endValue);
        }

        void UpdateEffects(float transition) {
            _transition = transition;
            _targetTransform.localScale = _baseScale * _transition;
        }

        void CancelShrinking(bool createNew = true) {
            _cts.Cancel();
            _cts.Dispose();
            if (createNew) {
                _cts = new CancellationTokenSource();
            }
        }

        protected override void OnDiscard() {
            CancelShrinking(false);
        }
    }
}