using System.Collections.Generic;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using Awaken.Utility.Extensions;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Interactions {
    public interface IVolumeChecker { }
    
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class VCVolumeChecker<T> : ViewComponent<T>, IVolumeChecker where T : IGrounded {
        [SerializeField] LayerMask volumeMask;
        [SerializeField] string volumeTag;
        [SerializeField] bool acceptOtherVolumeTriggers;

        protected HashSet<Collider> _volumes = new();
        bool _entered;
        
        public LayerMask VolumeMask => volumeMask;
        [UnityEngine.Scripting.Preserve] public string VolumeTag => volumeTag;

        protected override void OnAttach() {
            Target.ListenTo(GroundedEvents.AfterTeleported, Clear, this);
        }

        void OnTriggerEnter(Collider other) {
            if (IsValidVolume(other.gameObject) && _volumes.Add(other)) {
                Enter(other);
            }
        }

        void OnTriggerStay(Collider other) {
            if (IsValidVolume(other.gameObject) && _volumes.Add(other)) {
                Enter(other);
            }
        }

        void OnTriggerExit(Collider other) {
            if (_volumes.Remove(other)) {
                Exit(other);
            }
        }

        void Update() {
            if (_volumes.AnyNonAlloc()) {
                foreach (var c in _volumes) {
                    if (c == null || !c.gameObject.activeInHierarchy) {
                        Exit(c, true);
                    }
                }

                if (_volumes.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy) > 0 && !_volumes.AnyNonAlloc()) {
                    ExitedAllVolumes();
                } else {
                    OnStay();
                }
            }
        }

        bool IsValidVolume(GameObject go) {
            return LayerMatch() && TagMatch() && IsNotVolumeTrigger();

            bool LayerMatch() => volumeMask.Contains(go.layer);
            bool TagMatch() => volumeTag.IsNullOrWhitespace() || go.CompareTag(volumeTag);
            bool IsNotVolumeTrigger() => acceptOtherVolumeTriggers || go.GetComponent<IVolumeChecker>() == null;
        }

        void Enter(Collider other) {
            if (!_entered) {
                OnFirstVolumeEnter(other);
                _entered = true;
            }
            OnEnter(other);
        }

        void Exit(Collider other, bool destroyed = false) {
            OnExit(other, destroyed);
            if (!_volumes.AnyNonAlloc()) {
                ExitedAllVolumes();
            }
        }

        void ExitedAllVolumes() {
            if (_entered) {
                OnAllVolumesExit();
                _entered = false;
            }
        }

        protected void Clear() {
            if (_volumes.AnyNonAlloc()) {
                _volumes.ForEach(collider => OnExit(collider));
                ExitedAllVolumes();
                _volumes.Clear();
            }
        }

        protected abstract void OnFirstVolumeEnter(Collider other);
        protected abstract void OnAllVolumesExit();
        protected abstract void OnStay();
        protected virtual void OnEnter(Collider other) {}
        protected virtual void OnExit(Collider other, bool destroyed = false) { }
    }

    public abstract class VCHeroVolumeChecker : VCVolumeChecker<Hero> {
        protected Hero Hero => Target;
    }
}