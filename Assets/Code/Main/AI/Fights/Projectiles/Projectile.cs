using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public abstract class Projectile : ProjectileBehaviour {
        IEnumerable<SkillReference> _skillsReferences;
        bool _releaseAddressableOnDestroy;
        IPooledInstance _visualInstance;
        protected Rigidbody _rb;
        protected bool _isSetup;
        protected bool _initialized;
        protected ProjectileLogicData _logicData;

        public ProjectileVisualData VisualData {get; private set;}
        public abstract void SetVelocityAndForward(Vector3 velocity, ProjectileOffsetData? offsetData = null);
        public abstract void DeflectProjectile(DeflectedProjectileParameters parameters);
        public abstract ICharacter Owner { get; }
        public abstract Vector3 Velocity { get; }
        public abstract Transform VisualParent { get; }
        public virtual bool UsesGravity => _rb.useGravity;
        public bool IsProjectileInitialized => _initialized;
        public ProjectileData CreationData { get; private set; }

        protected override void OnMount() {
            base.OnMount();
            TryApplySkills();
        }

        public void Setup(ProjectileLogicData logicData, ProjectileVisualData visualData, IEnumerable<SkillReference> skillReferences, Transform firePoint, ProjectileData creationData) {
            VisualData = visualData;
            _logicData = logicData;
            _skillsReferences = skillReferences;
            _rb = GetComponentInChildren<Rigidbody>(true);
            _rb.isKinematic = false;
            CreationData = creationData;
            TryApplySkills();
            OnSetup(firePoint);
            _isSetup = true;
        }

        public void FinalizeConfiguration() {
            if (!_isSetup) {
                Log.Important?.Error($"Attempting to finalize projectile configuration when it's not setup.");
                return;
            }
            if (!_initialized) {
                OnFullyConfigured();
                _initialized = true;
            }
        }

        protected virtual void OnSetup(Transform firePoint) { }

        protected virtual void OnFullyConfigured() {
            if (VisualData != null && VisualData.trailHolder != null) {
                VisualData.trailHolder.SetActive(true);
            }
        }
        
        public void ReleaseAddressablesInstanceOnDestroy(IPooledInstance visualInstance = null) {
            if (_releaseAddressableOnDestroy) {
                Log.Important?.Error($"Trying to mark projectiles to release addressable instance on destroy when it's already set to be released.");
                return;
            }
            _releaseAddressableOnDestroy = true;
            _visualInstance = visualInstance;
        }

        void TryApplySkills() {
            if (_skillsReferences != null && Target != null) {
                foreach (var skillRef in _skillsReferences) {
                    var skill = skillRef.CreateSkill();
                    Target.AddElement(skill);
                }
            }
        }

        protected void SendVSEvent(VSCustomEvent eventType, params object[] parameters) {
            var skills = Target.Elements<Skill>();
            var go = gameObject;
            foreach (var skill in skills) {
                VGUtils.SendCustomEvent(skill.Machine, go, eventType, parameters);
            }
            VGUtils.SendCustomEvent(go, go, eventType, parameters);
        }

        protected void BeforeGameObjectDestroy() {
            if (_visualInstance != null) {
                _visualInstance?.Return();
                _visualInstance = null;
            }
        }

        protected async UniTaskVoid CustomTrailHolderBasedDestroy(Vector3 hitPosition, CancellationTokenSource cancellationTokenSource = null, bool returnVisualInstance = true) {
            var trailHolder = VisualData.trailHolder;
            Transform trailTransform;
            bool ignoreCustomDestroy = false;
            if (trailHolder == null) {
                trailHolder =  GetComponentInChildren<TrailRenderer>()?.gameObject;
                if (trailHolder == null) {
                    return;
                }
                trailTransform = trailHolder.transform;
                ignoreCustomDestroy = !trailTransform.IsChildOf(_visualInstance.Instance.transform);
            } else {
                trailTransform = trailHolder.transform;
            }
            
            ignoreCustomDestroy = ignoreCustomDestroy || _visualInstance == null;
            if (ignoreCustomDestroy) {
                trailTransform.SetParent(null);
                trailTransform.position = hitPosition;
                Destroy(trailHolder, VisualData.timeForTrailsToDie);
                return;
            }
            
            // Cache Trail Data
            var oldParent = trailTransform.parent;
            var oldLocalPosition = trailTransform.localPosition;
            var visualInstance = _visualInstance;
            if (returnVisualInstance) {
                _visualInstance = null;
            }

            // Unparent Trail
            trailTransform.SetParent(null);
            trailTransform.position = hitPosition;
            if (returnVisualInstance) {
                visualInstance.Instance.transform.SetParent(null);
                visualInstance.Instance.SetActive(false);
            }
            if (!await AsyncUtil.DelayTime(trailHolder, VisualData.timeForTrailsToDie, source: cancellationTokenSource)) {
                if (cancellationTokenSource is not { IsCancellationRequested: true }) {
                    // Trail Holder object was destroyed, probably from domain drop
                    // This pooled instance is no longer valid.
                    if (visualInstance is AddressablesPooledInstance addressablesPooledInstance) {
                        addressablesPooledInstance.Release();
                    }
                    return;
                }
            }
            
            // Restore Trail to Pooled Instance and return it to the pool
            trailTransform.SetParent(oldParent);
            trailTransform.localPosition = oldLocalPosition;
            if (VisualData.trailHolder != null) {
                VisualData.trailHolder.SetActive(false);
            }
            if (returnVisualInstance) {
                visualInstance.Return();
            }
        }

        protected override void OnGameObjectDestroy() {
            if (_releaseAddressableOnDestroy) {
                _visualInstance?.Return();
                Addressables.ReleaseInstance(gameObject);
            }
        }
    }
}