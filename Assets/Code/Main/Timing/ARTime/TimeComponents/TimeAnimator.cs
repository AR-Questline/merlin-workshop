using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime.TimeComponents {
    public class TimeAnimator : FactorBasedTimeComponent<Animator> {
        readonly Animator _animator;
        
        float _cachedSpeed;

        public TimeAnimator(Animator animator) {
            _animator = animator;
        }

        protected override void CacheBeforePause() {
            _cachedSpeed = _animator.speed;
        }

        protected override void RestoreAfterPause() {
            _animator.speed = _cachedSpeed;
        }

        protected override void OnTimeScaleChangeNoPause(float @from, float to, float factor) {
            _animator.speed *= factor;
        }

        public override Component Component => _animator;
    }
}