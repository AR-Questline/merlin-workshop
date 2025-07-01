using UnityEngine;

namespace Awaken.Utility.LowLevel {
    public abstract class PlayerLoopBasedLifetimeMonoBehaviour
        : MonoBehaviour, PlayerLoopBasedLifetime.IWithPlayerLoopEnable, PlayerLoopBasedLifetime.IWithPlayerLoopDisable {
        protected void OnEnable() {
            PlayerLoopBasedLifetime.Instance.ScheduleEnable(this);
            OnUnityEnable();
        }

        protected void OnDisable() {
            PlayerLoopBasedLifetime.Instance.ScheduleDisable(this);
            OnUnityDisable();
        }

        void PlayerLoopBasedLifetime.IWithPlayerLoopEnable.Enable() {
            OnPlayerLoopEnable();
        }

        void PlayerLoopBasedLifetime.IWithPlayerLoopDisable.Disable() {
            OnPlayerLoopDisable();
        }

        protected abstract void OnPlayerLoopEnable();
        protected abstract void OnPlayerLoopDisable();

        protected virtual void OnUnityEnable() {}
        protected virtual void OnUnityDisable() {}
    }
}
