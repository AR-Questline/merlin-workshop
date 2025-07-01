using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Semaphores {
    /// <summary>
    /// Semaphore that after each change is set to false. If you set it to true it will be true after given time <br/>
    /// Its Update must be called manually for it to work properly. <br/>
    /// </summary>
    public struct FragileSemaphore {
        readonly float _delayAfterTrue;
        readonly float _delayAfterFalse;
        readonly bool _unscaledTime;

        bool _state;
        bool _desiredState;
        bool _isValid;
        
        float _buildUpDeadline;

        ISemaphoreObserver _observer;

        float MyTime => _unscaledTime ? Time.unscaledTime : Time.time;

        public bool IsValid => _isValid;
        public bool DesiredState => _desiredState;
        
        public FragileSemaphore(bool defaultValue, [CanBeNull] ISemaphoreObserver observer, float delayAfterTrue, bool unscaledTime = false) {
            _delayAfterTrue = delayAfterTrue;
            _delayAfterFalse = delayAfterTrue;
            _unscaledTime = unscaledTime;
            _state = defaultValue;
            _desiredState = defaultValue;
            _buildUpDeadline = 0;
            _observer = observer;
            _isValid = true;
        }
        public FragileSemaphore(bool defaultValue, [CanBeNull] ISemaphoreObserver observer, float delayAfterTrue, float delayAfterFalse = -1, bool unscaledTime = false) {
            _delayAfterTrue = delayAfterTrue;
            _delayAfterFalse = delayAfterFalse == -1 ? delayAfterTrue : delayAfterFalse;
            _unscaledTime = unscaledTime;
            _state = defaultValue;
            _desiredState = defaultValue;
            _buildUpDeadline = 0;
            _observer = observer;
            _isValid = true;
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
            if (!State && _desiredState && MyTime >= _buildUpDeadline) {
                State = true;
            }
        }

        public void Set(bool desiredState) {
            State = false;
            _desiredState = desiredState;
            if (desiredState) {
                _buildUpDeadline = MyTime + (_desiredState ? _delayAfterTrue : _delayAfterFalse);
            }
        }

        public void ForceTrue() {
            State = true;
            _desiredState = true;
        }
        
        public static implicit operator bool(FragileSemaphore semaphore) {
            return semaphore.State;
        }
    }
}