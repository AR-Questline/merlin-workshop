using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Semaphores {
    /// <summary>
    /// Semaphore that on Notify is set to true and after few frames without notification is reset to false. <br/>
    /// Its Update must be called manually for it to work properly. <br/>
    /// </summary>
    public struct CoyoteSemaphore {
        readonly int _coyoteFrames;
        int _coyoteDeadline;
        bool _state;

        ISemaphoreObserver _observer;

        public CoyoteSemaphore([CanBeNull] ISemaphoreObserver observer, int coyoteFrames = 1) {
            this._coyoteFrames = coyoteFrames;
            _coyoteDeadline = -coyoteFrames;
            _state = false;
            _observer = observer;
        }
        
        public bool State {
            get => _state;
            private set {
                if (_state != value) {
                    _state = value;
                    TriggerObserver(value);
                }
            }
        }
        void TriggerObserver(bool value) {
            if (_observer != null) {
                if (value) {
                    _observer.OnUp();
                } else {
                    _observer.OnDown();
                }
                _observer.OnStateChanged(value);
            }
        }

        
        public void Update() {
            if (State && Time.frameCount > _coyoteDeadline) {
                State = false;
            }
        }

        public void Notify() {
            State = true;
            _coyoteDeadline = Time.frameCount + _coyoteFrames;
        }

        public void ForceDown() {
            State = false;
        }

        public static implicit operator bool(CoyoteSemaphore semaphore) {
            return semaphore.State;
        }
    }
}