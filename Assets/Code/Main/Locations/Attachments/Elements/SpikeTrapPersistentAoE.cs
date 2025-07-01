using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class SpikeTrapPersistentAoE : PersistentAoE, IRefreshedByAttachment<SpikeTrapWithPersistentAoEAttachment>, ILogicReceiverElement {
        public override ushort TypeForSerialization => SavedModels.SpikeTrapPersistentAoE;

        const string DistanceParameterName = "CollisionDistance";
        
        [Saved] bool _activated;
        
        ShareableARAssetReference _vfx;
        Transform _vfxSpawnPoint;
        float _maxDistanceToHeroToSpawnVFX;
        Hero _hero;
        bool _hasToBeActivated;
        
        public BoxCollider DamageCollider { get; private set; }
        bool HeroWithinRangeToShowVFX => Vector3.Distance(_hero.Coords, _vfxSpawnPoint.position) <= _maxDistanceToHeroToSpawnVFX;

        public SpikeTrapPersistentAoE(SpikeTrapWithPersistentAoEAttachment spikeTrapWithPersistentAoEAttachment, float? tick, IDuration duration, StatusTemplate statusTemplate, float buildupStrength,
            SkillVariablesOverride overrides, SphereDamageParameters? damageParameters, bool onlyOnGrounded, bool isRemovingOther, bool isRemovable, 
            bool canApplyToSelf, bool discardParentOnEnd, bool discardOnDamageDealerDeath)
            : base(tick, duration, statusTemplate, buildupStrength, overrides, damageParameters, onlyOnGrounded, isRemovingOther, isRemovable, 
                canApplyToSelf, discardParentOnEnd, discardOnDamageDealerDeath) {
            _vfx = spikeTrapWithPersistentAoEAttachment.Vfx;
            _vfxSpawnPoint = spikeTrapWithPersistentAoEAttachment.VfxSpawnPoint;
            _maxDistanceToHeroToSpawnVFX = spikeTrapWithPersistentAoEAttachment.MaxDistanceToHeroToSpawnVFX;
            DamageCollider = spikeTrapWithPersistentAoEAttachment.DamageCollider;
            _hasToBeActivated = spikeTrapWithPersistentAoEAttachment.HasToBeActivated;
            _activated = !_hasToBeActivated;
            
            _hero = Hero.Current;
        }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        SpikeTrapPersistentAoE() { }

        public new static class Events {
            public static readonly Event<SpikeTrapPersistentAoE, SpikeTrapPersistentAoE> OnEffectWithVFXApplied = new(nameof(OnEffectWithVFXApplied));
        }
        
        public void InitFromAttachment(SpikeTrapWithPersistentAoEAttachment spec, bool isRestored) {
            _vfx = spec.Vfx;
            _vfxSpawnPoint = spec.VfxSpawnPoint;
            _maxDistanceToHeroToSpawnVFX = spec.MaxDistanceToHeroToSpawnVFX;
            _hero = Hero.Current;
            DamageCollider = spec.DamageCollider;
            _damageParameters = spec.GetDamageParameters();
            _hasToBeActivated = spec.HasToBeActivated;
        }

        protected override void ProcessUpdate(float deltaTime) {
            ApplyRemoveStatus();

            if (!_tick.HasValue) {
                return;
            }
            
            _lastTickTime -= deltaTime;
            
            if (_lastTickTime <= 0 && HeroWithinRangeToShowVFX && (_activated || !_hasToBeActivated)) {
                _lastTickTime = _tick!.Value;
                ApplyEffectsWithVFX().Forget();
            }
        }

        async UniTaskVoid ApplyEffectsWithVFX() {
            var pooledInstance = await PrefabPool.InstantiateAndReturn(_vfx, Vector3.zero, Quaternion.identity, parent: _vfxSpawnPoint);
            var vfx = pooledInstance.Instance.GetComponent<VisualEffect>();
            vfx.SetFloat(DistanceParameterName, DamageCollider.size.z);
            await AsyncUtil.DelayTime(this, GameConstants.Get.spikeTrapDamageDelay);
            ApplyOverTimeEffects();
            this.Trigger(Events.OnEffectWithVFXApplied, this);
        }

        public void OnLogicReceiverStateChanged(bool state) {
            _activated = state;
        }
    }
}
