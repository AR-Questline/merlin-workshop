using System;
using Awaken.TG.Main.Scenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Utility.Availability {
    public abstract class AvailabilityBase {
        [ShowInInspector, ReadOnly, NonSerialized] bool _enabled;
        [ShowInInspector, ReadOnly, NonSerialized] bool _available;
        Action _onBecomeAvailable;
        Action _onBecomeUnavailable;

        IEventListener _sceneInitializationListener;

        protected abstract bool SceneInitializationNeeded { get; }

        public void Init(Action onBecomeAvailable, Action onBecomeUnavailable, bool startingAvailability = false) {
            InitListeners();
            _onBecomeAvailable = onBecomeAvailable;
            _onBecomeUnavailable = onBecomeUnavailable;
            _available = startingAvailability;
            AvailabilityInitialization.RefreshAvailability(this);
        }

        public void Deinit() {
            _onBecomeAvailable = null;
            _onBecomeUnavailable = null;
            DisposeListeners();
            AvailabilityInitialization.RemoveFromRefreshAwaiting(this);
        }

        public void Enable() {
            _enabled = true;
            CheckChanged();
        }

        public void Disable() {
            _enabled = false;
            CheckChanged();
        }

        void InitListeners() {
            if (SceneInitializationNeeded) {
                if (SceneLifetimeEvents.Get.EverythingInitialized) {
                    OnSceneInitialized();
                } else {
                    _sceneInitializationListener = World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.AfterSceneFullyInitialized, OnSceneInitializedBase);
                }
            }
        }

        void OnSceneInitializedBase(SceneLifetimeEventData _) {
            World.EventSystem.TryDisposeListener(ref _sceneInitializationListener);
            OnSceneInitialized();
        }
        
        protected abstract void OnSceneInitialized();
        
        protected virtual void DisposeListeners() {
            World.EventSystem.TryDisposeListener(ref _sceneInitializationListener);
        }

        protected void CheckChanged() {
            var available = IsEnabledAndAvailable();
            if (_available != available) {
                _available = available;
                var callback = _available ? _onBecomeAvailable : _onBecomeUnavailable;
                callback?.Invoke();
            }
        }

        bool IsEnabledAndAvailable() {
            if (!_enabled) {
                return false;
            }

            return CalculateAvailability();
        }
        
        public void Refresh() {
            if (_enabled) {
                CheckChanged();
            }
        }

        protected abstract bool CalculateAvailability();
        
        public static implicit operator bool(AvailabilityBase availabilityBase) {
            return availabilityBase._available;
        }
    }
}