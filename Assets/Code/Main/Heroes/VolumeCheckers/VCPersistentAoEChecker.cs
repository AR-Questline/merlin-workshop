using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    [RequireComponent(typeof(Rigidbody))]
    public class VCPersistentAoEChecker : VCVolumeChecker<Location> {
        PersistentAoE _persistentAoE;

        protected override void OnAttach() {
            base.OnAttach();
            
            Target.AfterFullyInitialized(() => {
                if (Target.TryGetElement(out PersistentAoE persistentAoE)) {
                    PersistentAoEAdded(persistentAoE);
                }
            }, this);
            
            Target.ListenTo(Model.Events.AfterElementsCollectionModified, e => {
                if (!e.HasBeenDiscarded && e is PersistentAoE aoe) {
                    PersistentAoEAdded(aoe);
                }
            }, this);
        }

        void PersistentAoEAdded(PersistentAoE persistentAoE) {
            _persistentAoE = persistentAoE;
            _persistentAoE.ListenTo(Model.Events.BeforeDiscarded, BeforePersistentAoEDiscarded, this);
            AfterPersistentAoEAdded().Forget();
        }

        async UniTaskVoid AfterPersistentAoEAdded() {
            if (!await AsyncUtil.DelayFrame(this, 3)) {
                return;
            }
            
            foreach (var col in _volumes.WhereNotUnityNull()) {
                OnEnter(col);
            }
        }

        void BeforePersistentAoEDiscarded() {
            foreach (var col in _volumes.WhereNotUnityNull()) {
                OnExit(col);
            }
        }
        
        protected override void OnEnter(Collider other) {
            if (_persistentAoE == null || _persistentAoE.HasBeenDiscarded) {
                return;
            }

            if (_persistentAoE.IsRemovingOther) {
                if (VGUtils.TryGetModel(other.gameObject, out Location location) && location.TryGetElement<PersistentAoE>(out var aoe)) {
                    if (aoe != _persistentAoE) {
                        _persistentAoE.NewPersistentAoEInRange();
                    }
                    return;
                }
            }

            if (!VGUtils.TryGetModel(other.gameObject, out IAlive alive)) {
                return;
            }
            
            _persistentAoE.AliveEnteredZone(alive);
        }

        protected override void OnExit(Collider other, bool destroyed = false) {
            if (_persistentAoE == null || _persistentAoE.HasBeenDiscarded) {
                return;
            }
            
            if (other == null) {
                return;
            }
            
            if (!VGUtils.TryGetModel(other.gameObject, out IAlive alive)) {
                return;
            }
            _persistentAoE.AliveExitedZone(alive);
        }

        protected override void OnFirstVolumeEnter(Collider other) { }

        protected override void OnAllVolumesExit() { }

        protected override void OnStay() { }
    }
}