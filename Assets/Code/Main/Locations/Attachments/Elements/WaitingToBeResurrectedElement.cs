using Awaken.Utility;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Graphics.VFX.Binders;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class WaitingToBeResurrectedElement : UnconsciousElement, IHealthBarHiddenMarker {
        public override ushort TypeForSerialization => SavedModels.WaitingToBeResurrectedElement;

        const string OnDeathVFXEvent = "OnDeath";
        const string OnDeathSFXParameter = "ReviveFailed";
        const string OnResurrectStartedVFXEvent = "OnResurrectStarted";
        const string OnResurrectStartedSFXEvent = "ReviveStarted";
        const string OnResurrectVFXEvent = "OnResurrect";
        const string OnResurrectSFXEvent = "Revived";
        [Saved] WeakModelRef<ICharacter> _killer;
        readonly HashSet<ICharacter> _resurrectors;
        VFXBodyMarker _vfxBodyMarker;
        ShareableARAssetReference _vfxReference;
        IPooledInstance _vfxInstance;
        VisualEffect _vfx;
        ARFmodEventEmitter _audioEmitter;
        CancellationTokenSource _vfxCts;

        public bool IsBeingResurrected { get; private set; }

        public override bool AddKillUnconsciousAction => false;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public WaitingToBeResurrectedElement() { }
        
        public WaitingToBeResurrectedElement(ICharacter killer, ICharacter resurrector, ShareableARAssetReference vfxReference) {
            _killer = new(killer);
            _resurrectors = new HashSet<ICharacter>() { resurrector };
            _vfxReference = vfxReference;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            LoadVfx().Forget();
        }

        protected override void OnRestore() {
            ParentModel.ParentModel.OnVisualLoaded(_ => Kill());
        }
        
        protected override void InitRegainConsciousListeners() {
            base.InitRegainConsciousListeners();
            foreach (var applier in _resurrectors) {
                applier.ListenTo(IAlive.Events.BeforeDeath, OnResurrectorKilled, this);
            }
        }

        public void AddResurrector(ICharacter resurrector) {
            if (_resurrectors.Add(resurrector) && IsInitialized) {
                resurrector.ListenTo(IAlive.Events.BeforeDeath, OnResurrectorKilled, this);
            }
        }
        
        public void RemoveResurrector(ICharacter resurrector) {
            _resurrectors.Remove(resurrector);
            if (_resurrectors.Count == 0) {
                Kill();
            }
        }

        async UniTaskVoid LoadVfx() {
            if (_vfxReference is not { IsSet: true }) {
                return;
            }

            _vfxBodyMarker = ParentModel.ParentTransform.GetComponentInChildren<VFXBodyMarker>();
            _vfxBodyMarker.MarkBeingUsed();
            _vfxCts = new CancellationTokenSource();
            _vfxInstance = await PrefabPool.Instantiate(_vfxReference, Vector3.zero, Quaternion.identity, _vfxBodyMarker.transform, Vector3.one, _vfxCts.Token, false);
            if (_vfxInstance.Instance is { } instance) {
                _vfx = instance.GetComponentInChildren<VisualEffect>();
                _audioEmitter = instance.GetComponentInChildren<ARFmodEventEmitter>();
                if (_audioEmitter != null) {
                    // _audioEmitter.SetParameter(OnResurrectSFXEvent, 0);
                    // _audioEmitter.SetParameter(OnResurrectStartedSFXEvent, 0);
                    // _audioEmitter.SetParameter(OnDeathSFXParameter, 0);
                }
                instance.SetActive(true);
            }
        }

        void Kill() {
            if (_killer.TryGet(out var killer)) {
                ParentModel.ParentModel.Kill(killer);
            } else {
                ParentModel.ParentModel.Kill();
            }

            if (_vfx != null) {
                _vfx.SendEvent(OnDeathVFXEvent);
            }
            if (_audioEmitter != null) {
                //_audioEmitter.SetParameter(OnDeathSFXParameter, 1);
            }
        }

        void OnResurrectorKilled(DamageOutcome damageOutcome) {
            RemoveResurrector(damageOutcome.Damage.Target as ICharacter);
        }
        
        protected override void OnAnyDangerEnded() { }

        public void ResurrectionStarted() {
            if (_vfx != null) {
                _vfx.SendEvent(OnResurrectStartedVFXEvent);
            }
            if (_audioEmitter != null) {
                //_audioEmitter.SetParameter(OnResurrectStartedSFXEvent, 1);
            }
            IsBeingResurrected = true;
        }

        public void Resurrect() {
            ParentModel.Health.SetToFull();
            RegainConscious();
            if (_vfx != null) {
                _vfx.SendEvent(OnResurrectVFXEvent);
            }
            if (_audioEmitter != null) {
                //_audioEmitter.SetParameter(OnResurrectSFXEvent, 1);
            }
        }

        public void StopResurrecting() {
            IsBeingResurrected = false;
            if (_audioEmitter != null) {
                //_audioEmitter.SetParameter(OnResurrectStartedSFXEvent, 0);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            if (_vfxBodyMarker != null) {
                _vfxBodyMarker.MarkBeingUnused();
            }

            if (_vfxInstance != null) {
                if (fromDomainDrop) {
                    _vfxInstance.Return();
                } else {
                    VFXUtils.StopVfxAndReturn(_vfxInstance, PrefabPool.DefaultVFXLifeTime);
                }
            } else {
                _vfxCts?.Cancel();
            }
        }
    }
}
