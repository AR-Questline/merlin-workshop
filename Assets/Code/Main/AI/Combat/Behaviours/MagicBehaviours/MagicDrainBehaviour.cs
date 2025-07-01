using System;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Relations;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public partial class MagicDrainBehaviour : SpellCastingBehaviourBase {
        const string VFXTargetPropertyName = "TargetPosition";
        
        // === Serialized Fields
        [SerializeField] bool exposeWeakspot;
        
        [ARAssetReferenceSettings(new[] { typeof(VisualEffect) }, group: AddressableGroup.VFX)]
        [SerializeField] ShareableARAssetReference drainTrailVfxPrefab;
        
        [SerializeField] DrainStatType drainStatType;
        [SerializeField] float targetDrainRate;
        [SerializeField, ShowIf(nameof(CanCasterGainFromDrain))] float casterGainFromDrainRate;

        CancellationTokenSource _drainTrailCancellationToken;
        IPooledInstance _drainTrailInstance;
        VisualEffect _drainTrailVfx;
        Transform _drainTrailVfxTransform;
        Transform _drainTrailVfxTargetTransform;
        Transform _ownHandTransform;
        StatType _statToAffect;
        ContractContext _drainContext;
        ContractContext _gainContext;
        
        bool _draining;
        bool CanCasterGainFromDrain => drainStatType == DrainStatType.Health;
        protected override bool ExposeWeakspot => exposeWeakspot;

        protected override void OnInitialize() {
            base.OnInitialize();
            
            _statToAffect = GetStatTypeToAffect();
            Npc.ListenTo(AITargetingUtils.Relations.Targets.Events.BeforeDetached, OnTargetLost, this);
        }
        
        StatType GetStatTypeToAffect() => drainStatType switch {
            DrainStatType.Mana => CharacterStatType.Mana,
            DrainStatType.Health => AliveStatType.Health,
            DrainStatType.Stamina => CharacterStatType.Stamina,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        void OnTargetLost(RelationEventData data) {
            if (ParentModel.CurrentBehaviour.Get() == this) {
                ParentModel.StopCurrentBehaviour(true);
            }
        }
        
        protected override async UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            bool drainTrailLoaded = await TrySetupDrainTrailVFX();

            if (!drainTrailLoaded) {
                Stop();
            } else {
                StartDraining();
            }
            
            if (returnFireballInHandAfterSpawned) {
                base.ReturnInstantiatedPrefabs();
            }
        }

        async UniTask<bool> TrySetupDrainTrailVFX() {
            if (drainTrailVfxPrefab is not { IsSet: true }) {
                return false;
            }

            _drainTrailCancellationToken = new CancellationTokenSource();
            _drainTrailInstance = await PrefabPool.Instantiate(drainTrailVfxPrefab, 
                castingPointOffset, Quaternion.identity,
                cancellationToken: _drainTrailCancellationToken.Token);
            
            if (_drainTrailInstance == null || HasBeenDiscarded) {
                return false;
            }

            var target = ParentModel.NpcElement?.GetCurrentTarget();
            _drainTrailVfx = _drainTrailInstance.Instance.GetComponent<VisualEffect>();
            _drainTrailVfxTransform = _drainTrailVfx.transform;
            _drainTrailVfxTargetTransform = target?.Torso;
            _drainTrailVfx.Play();

            _ownHandTransform = GetHand();
            
            return true;
        }

        void StartDraining() {
            _draining = true;

            var target = ParentModel.NpcElement?.GetCurrentTarget();
            _drainContext = new ContractContext(ParentModel.NpcElement, target, ChangeReason.CombatDamage);
            if (CanCasterGainFromDrain) {
                _gainContext = new ContractContext(ParentModel.NpcElement, ParentModel.NpcElement, ChangeReason.AttackBehaviour);
            }
        }
        
        void StopDraining() {
            if (_draining) {
                ReturnDrainTrailVFX();

                _drainContext = null;
                _gainContext = null;
                
                _draining = false;
            }
        }

        public override void Update(float deltaTime) {
            base.Update(deltaTime);
            HandleDraining(deltaTime);
            UpdateDrainVFX();
        }

        void UpdateDrainVFX() {
            if (_drainTrailVfx == null) {
                return;
            }
            _drainTrailVfxTransform.position = _ownHandTransform.position;

            if (_drainTrailVfxTargetTransform != null) {
                _drainTrailVfx.SetVector3(VFXTargetPropertyName, _drainTrailVfxTargetTransform.position);
            }
        }

        void HandleDraining(float deltaTime) {
            if (!_draining) {
                return;
            }
            
            if (_statToAffect == AliveStatType.Health) {
                DrainHealthFromTargetOverTime(deltaTime);
            } else {
                ParentModel.NpcElement.GetCurrentTarget().Stat(_statToAffect).DecreaseBy(targetDrainRate * deltaTime, _drainContext);
            }

            if (CanCasterGainFromDrain) {
                Npc.Stat(_statToAffect).IncreaseBy(casterGainFromDrainRate * deltaTime, _gainContext);
            }
        }

        void DrainHealthFromTargetOverTime(float deltaTime) {
            var npc = Npc;
            var target = npc.GetCurrentTarget();
            var targetCoords = target.Coords;
            var damageParameters = new DamageParameters {
                DamageTypeData = new RuntimeDamageTypeData(DamageType.MagicalHitSource),
                IgnoreArmor = true,
                Inevitable = true,
                IsDamageOverTime = true,
                Position = targetCoords,
                Direction = (npc.Coords - targetCoords).normalized,
            };
            var damage = new Damage(damageParameters, npc, target, new RawDamageData(targetDrainRate * deltaTime));
            target.HealthElement.TakeDamage(damage);
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            base.TriggerAnimationEvent(animationEvent);
            if (animationEvent.actionType == ARAnimationEvent.ActionType.AttackEnd) {
                StopDraining();
            }
        }

        public override void StopBehaviour() {
            StopDraining();
            base.StopBehaviour();
        }
        
        protected override void ReturnInstantiatedPrefabs() {
            ReturnDrainTrailVFX();
            
            base.ReturnInstantiatedPrefabs();
        }
        
        void ReturnDrainTrailVFX() {
            _drainTrailCancellationToken?.Cancel();
            _drainTrailCancellationToken = null;
            
            if (_drainTrailVfx != null) {
                _drainTrailVfx.Stop();
            }
            _drainTrailInstance?.Return(PrefabPool.DefaultVFXLifeTime).Forget();
            _drainTrailInstance = null;
        }

        [Serializable]
        enum DrainStatType : byte {
            Mana,
            Health,
            Stamina
        }
    }
}