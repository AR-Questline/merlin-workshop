using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public abstract class VCHeroHUDBar : ViewComponent<Hero>, ISemaphoreObserver {
        const float HideDelay = 1f;
        const float AnimationDuration = 0.28f;
        
        [SerializeField] Bar bar;
        [SerializeField, CanBeNull] Transform valueDropAnimation;
        [SerializeField, ShowIf(nameof(valueDropAnimation))]
        float animationMinDelay;
        [SerializeField, ShowIf(nameof(valueDropAnimation))]
        float animationMaxDelay = 0.04f;

        protected abstract StatType StatType { get; }
        protected abstract float Percentage { get; }
        protected virtual float PredictionPercentage => 0f;
        public virtual bool ForceShow => !_hideSemaphore.State;
        protected virtual bool EnableAutoValueDropAnimation => true;
        protected Bar Bar => bar;
        
        bool _canShowAnimation;
        bool _randomAnimationDelay;
        FragileSemaphore _hideSemaphore;
        Sequence _animationSequence;
        PlayerInput _playerInput;

        protected override void OnAttach() {
            _hideSemaphore = new FragileSemaphore(true, this, HideDelay, true);
            _canShowAnimation = true;
            Target.AfterFullyInitialized(Init);
        }

        protected virtual void Init() {
            _playerInput = World.Only<PlayerInput>();
            Target.ListenTo(Stat.Events.StatChangedBy(StatType), StatChanged, this);
            bar.SetPercentInstant(Percentage);
            bar.SetPrediction(PredictionPercentage);
            Target.GetOrCreateTimeDependent().WithUpdate(UpdateBar);
        }

        void StatChanged(Stat.StatChange change) {
            if (Mathf.Abs(change.value) >= Mathf.Epsilon && Percentage < 1) {
                _hideSemaphore.Set(true);
                if (change.value < 0 && EnableAutoValueDropAnimation) {
                    TryPlayAnimation();
                }
            }
        }
        
        void UpdateBar(float deltaTime) {
            bar.SetPercent(Percentage);
            bar.SetPrediction(PredictionPercentage);
            _hideSemaphore.Update();
        }

        protected void TryPlayAnimation() {
            HandlePlayAnimationRestriction(out bool canPlayAnimation);
            
            if (!canPlayAnimation || bar is not GlowingBar glowingBar || glowingBar.Indicator == null || valueDropAnimation == null) {
                return;
            }

            _canShowAnimation = false;
            float randomDelay = _randomAnimationDelay ? Random.Range(animationMinDelay, animationMaxDelay) : 0f;
            valueDropAnimation.position = glowingBar.Indicator.position;
            valueDropAnimation.TrySetActiveOptimized(true);
            _animationSequence = DOTween.Sequence()
                .AppendInterval(AnimationDuration)
                .AppendCallback(HideAnimation)
                .AppendInterval(randomDelay)
                .SetUpdate(true)
                .OnComplete(() => _canShowAnimation = true)
                .OnKill(HideAnimation);
        }

        protected virtual void HandlePlayAnimationRestriction(out bool canPlayAnimation) {
            canPlayAnimation = _canShowAnimation;
        }
        
        protected void HandleHoldAnimationRestriction() {
            _randomAnimationDelay = _playerInput.AnyHeldActionsActive;

            if (_randomAnimationDelay && _playerInput.AnyDownActionsActive) {
                _animationSequence.Kill(true);
            }
        }
        
        void HideAnimation() {
            if (valueDropAnimation == null) return;
            valueDropAnimation.TrySetActiveOptimized(false);
        }
        
        protected override void OnDiscard() {
            Target.GetTimeDependent()?.WithoutUpdate(UpdateBar);
            UITweens.DiscardSequence(ref _animationSequence, true);
        }
    }
}