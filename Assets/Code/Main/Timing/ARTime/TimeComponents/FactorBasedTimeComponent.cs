using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime.TimeComponents {
    public abstract class FactorBasedTimeComponent<T> : ITimeComponent where T : Component {
        float _timeScaleBeforePause;

        public void OnTimeScaleChange(float from, float to) {
            if (to == 0) {
                CacheBeforePause();
                _timeScaleBeforePause = from;
            } else if (from == 0) {
                RestoreAfterPause();
                OnTimeScaleChange(_timeScaleBeforePause, to);
            } else {
                OnTimeScaleChangeNoPause(from, to, to / from);
            }
        }


        protected abstract void CacheBeforePause();
        protected abstract void RestoreAfterPause();
        protected abstract void OnTimeScaleChangeNoPause(float from, float to, float factor);

        public virtual void OnFixedUpdate(float fixedDeltaTime) { }

        public abstract Component Component { get; }
    }
}