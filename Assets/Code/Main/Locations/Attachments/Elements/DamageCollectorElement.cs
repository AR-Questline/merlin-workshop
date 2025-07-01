using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Magic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class DamageCollectorElement : Element<IAlive> {
        const string IntensityName = "Intensity";
        const string SizeName = "Particle Size";
        const string HitEffectName = "HitColorLerp";
        const string OnHitEventName = "OnHit";
        const float StartingIntensity = 0.4f;
        const float MaximumIntensity = 1.0f;
        const float StartingSize = 0.2f;
        const float MaximumSize = 0.35f;
        const float MaximumIntensityAtDamage = 100f;

        public sealed override bool IsNotSaved => true;

        readonly ARAssetReference _effect;
        readonly WeakModelRef<Skill> _spawnedWithSkill;
        readonly bool _preventDamage;
        float _collectedDamage;
        float _hitEffectPower;
        VisualEffect _effectVfx;

        CancellationTokenSource _cancellationTokenSource;

        public DamageCollectorElement(Skill skill, ShareableARAssetReference effectPrefab, bool preventDamage) {
            _spawnedWithSkill = skill;
            _preventDamage = preventDamage;
            _effect = effectPrefab.Get();
        }

        protected override void OnFullyInitialized() {
            ParentModel.HealthElement.ListenTo(HealthElement.Events.BeforeTakenFinalDamage, OnBeforeTakenFinalDamage, this);
            if (_effect?.IsSet ?? false) {
                Transform transform;
                Quaternion rotation;
                Vector3 position;
                if (ParentModel is Hero hero) {
                    transform = hero.Head;
                    rotation = hero.Head.rotation;
                    position = hero.Head.position + transform.forward * 0.5f;
                } else {
                    transform = ParentModel.ParentTransform;
                    rotation = ParentModel.Rotation;
                    position = ParentModel.Coords + Vector3.up;
                }

                InstantiateEffect(position, rotation, transform);
            }
        }

        void InstantiateEffect(Vector3 position, Quaternion rotation, Transform parent) {
            _effect.LoadAsset<GameObject>().OnComplete(h => {
                if (h.Status != AsyncOperationStatus.Succeeded || h.Result == null) {
                    h.Release();
                    return;
                }
                if (HasBeenDiscarded) {
                    h.Release();
                    return;
                }
                
                var instance = Object.Instantiate(h.Result, position, rotation, parent);
                _effectVfx = instance.GetComponent<VisualEffect>();
                _effectVfx.SetFloat(IntensityName, StartingIntensity);
                _effectVfx.SetFloat(SizeName, StartingSize);
            });
        }

        void OnBeforeTakenFinalDamage(HookResult<HealthElement, Damage> hook) {
            if (hook.Value.Type is DamageType.Fall) {
                return;
            }

            _collectedDamage += hook.Value.Amount;

            if (_effectVfx != null) {
                _effectVfx.SendEvent(OnHitEventName);
                _effectVfx.SetFloat(HitEffectName, 1f);

                float powerPercent = _collectedDamage / MaximumIntensityAtDamage;

                float intensity = powerPercent.Remap(0, 1, StartingIntensity, MaximumIntensity, true);
                _effectVfx.SetFloat(IntensityName, intensity);

                float size = powerPercent.Remap(0, 1, StartingSize, MaximumSize, true);
                _effectVfx.SetFloat(SizeName, size);

                _hitEffectPower = 1f;

                if (_cancellationTokenSource == null) {
                    OnHitEffectDeactivate().Forget();
                }
            }

            if (_preventDamage) {
                hook.Value.WithHitSurface(SurfaceType.HitMagic);
                hook.Value.RawData.SetToZero();
            }
        }

        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public float GetDamage(bool reset = false, bool discard = false) {
            float damage = _collectedDamage;
            if (reset) {
                ResetDamage();
            }

            if (discard) {
                Discard();
            }

            return damage;
        }

        public void ResetDamage() {
            _collectedDamage = 0f;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_effectVfx != null) {
                _cancellationTokenSource?.Cancel();
                Object.Destroy(_effectVfx.gameObject);
                _effectVfx = null;
                _effect.ReleaseAsset();
            }

            if (_spawnedWithSkill.TryGet(out var skill) && skill.SourceItem != null) {
                skill.SourceItem.Trigger(MagicFSM.Events.EndCasting, MagicEndState.MagicEnd);
            }
        }

        async UniTaskVoid OnHitEffectDeactivate() {
            _cancellationTokenSource = new CancellationTokenSource();
            while (await AsyncUtil.DelayFrame(this, 1, _cancellationTokenSource.Token) && _hitEffectPower > 0f) {
                _hitEffectPower -= Time.deltaTime * 2f;
                _effectVfx.SetFloat("HitColorLerp", _hitEffectPower);
            }

            _cancellationTokenSource = null;
        }
    }
}
