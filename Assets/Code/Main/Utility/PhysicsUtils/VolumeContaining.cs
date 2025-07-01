using System.Collections.Generic;
using System.Linq;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.PhysicsUtils {
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class VolumeContaining<TItem> : MonoBehaviour {
        bool _shouldUpdate;

        Atomic _atomic;
        
        HashSet<TItem> _newItems = new();
        HashSet<TItem> _currentItems = new();

        ICallbacks _callbacks;

        protected abstract TItem GetItemFrom(Collider collider);

        void Awake() {
            _atomic = new Atomic(this);
        }

        void OnTriggerStay(Collider other) {
            var item = GetItemFrom(other);
            if (item != null) {
                _newItems.Add(item);
            }
        }

        void FixedUpdate() {
            // _newItems are updated in physics loop, after that we want to refresh _currentItems
            // but we don't want it to be done on FixedUpdate because we trigger events there and it should not affect physics
            // with low FPS physics loop may not occurred between Updates,
            // so here we only set this bool so on Update we know if we should refresh _currentItems
            _shouldUpdate = true;
        }

        void Update() {
            if (_shouldUpdate) {
                _shouldUpdate = false;
                _atomic.Update();
            }
        }

        void OnDisable() {
            _atomic.Exit();
        }

        public void AssignCallback(ICallbacks callbacks) {
            if (_callbacks != null) {
                Log.Important?.Error("VolumeContaining cannot has more than one callback", this);
                return;
            }
            _callbacks = callbacks;
            _atomic.AfterCallbackAssigned();
        }

        void AtomicUpdate() {
            foreach (var item in _newItems.Except(_currentItems)) {
                _callbacks.OnVolumeEnter(item);
            }

            foreach (var item in _currentItems.Except(_newItems)) {
                _callbacks.OnVolumeExit(item);
            }

            (_currentItems, _newItems) = (_newItems, _currentItems);
            _newItems.Clear();
        }
        
        void AtomicExit() {
            foreach (var item in _currentItems) {
                _callbacks.OnVolumeExit(item);
            }
            
            _currentItems.Clear();
            _newItems.Clear();
        }

        void AtomicAfterCallbackAssigned() {
            foreach (var item in _currentItems) {
                _callbacks.OnVolumeEnter(item);
            }
        }
        
        class Atomic : AtomicGuardian<VolumeContaining<TItem>> {
            bool _exit;
            bool _update;
            bool _afterCallbackAssigned;

            public Atomic(VolumeContaining<TItem> outer) : base(outer) { }
            
            public void Exit() => Call(ref _exit);
            public void Update() => Call(ref _update);
            public void AfterCallbackAssigned() => Call(ref _afterCallbackAssigned);

            protected override void CheckRequest(VolumeContaining<TItem> outer) {
                if (Requested(ref _exit)) {
                    outer.AtomicExit();
                } else if (Requested(ref _update)) {
                    outer.AtomicUpdate();
                } else if (Requested(ref _afterCallbackAssigned)) {
                    outer.AtomicAfterCallbackAssigned();
                }
            }
        }
        
        public interface ICallbacks {
            void OnVolumeEnter(TItem item);
            void OnVolumeExit(TItem item);
        }
    }
}