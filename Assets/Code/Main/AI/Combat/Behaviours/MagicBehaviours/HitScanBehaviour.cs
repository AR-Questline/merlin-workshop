using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Fights.SolarBeam;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.VisualGraphUtils;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public class HitScanBehaviour : SpellCastingBehaviourBase {
        [SerializeField] SolarBeamCreationData solarBeamCreationData = SolarBeamCreationData.Default;
        [SerializeField] NpcDamageData damageData = NpcDamageData.DefaultMagicAttackData;
        [BoxGroup(BaseCastingGroup), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)]
        ShareableARAssetReference hitScanVfx;
        protected override bool IsInValidState => NpcGeneralFSM.CurrentAnimatorState.Type == NpcStateType.MagicLoopHold
                                                  || base.IsInValidState;

        List<IAlive> _damagedTargets = new();
        SolarBeamData _data;

        protected override UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            _data = solarBeamCreationData.Create(damageData.GetRawDamageData(Npc));
            ThrowCast(ParentModel.GetDeltaTime(), GetHand());
            ReturnInstantiatedPrefabs();
            return UniTask.CompletedTask;
        }

        void ThrowCast(float deltaTime, Transform hand) {
            var handPosition = hand.position;
            var handRotation = hand.rotation;
            PrefabPool.InstantiateAndReturn(hitScanVfx, handPosition, handRotation).Forget();
            var startPosition = handPosition + handRotation * _data.raycastOffset;
            
            Vector3 forward = hand.forward;
            ICharacter currentTarget = ParentModel.NpcElement.GetCurrentTarget();
            if (currentTarget != null) {
                forward = currentTarget.Coords - startPosition;
            }
            
            if (_data.pierceTargets) {
                List<HitResult> hitResults = _data.targetDetection.RaycastMultiHit(startPosition, forward, _data.maxRange);
                if (hitResults.Count > 0) {
                    foreach (var hitResult in hitResults) {
                        CheckCastResult(hitResult, deltaTime);
                    }
                }
            } else {
                HitResult hitResult = _data.targetDetection.Raycast(startPosition, forward, _data.maxRange);
                if (hitResult.Collider != null) {
                    CheckCastResult(hitResult, deltaTime);
                }
            }
            
            _damagedTargets.Clear();
        }

        void CheckCastResult(HitResult hitResult, float deltaTime) {
            if (hitResult.Prevented) {
                return;
            }

            Collider hitCollider = hitResult.Collider;
            if (hitCollider == null) {
                return;
            }

            var iAlive = VGUtils.GetModel<IAlive>(hitCollider.gameObject);
            if (iAlive != Npc && iAlive != null) {
                OnAliveHit(iAlive, hitCollider, deltaTime);
            }
        }

        void OnAliveHit(IAlive alive, Collider collider, float deltaTime) {
            if (_damagedTargets.Contains(alive)) {
                return;
            }

            if (_data.damageData.isDamageOverTime) {
                var rawDamageOverTime = new RawDamageData(_data.rawDamageData);
                rawDamageOverTime.MultiplyMultModifier(deltaTime);
                DealDamage(alive, collider, rawDamageOverTime);
                return;
            }
            
            _damagedTargets.Add(alive);
            DealDamage(alive, collider, new RawDamageData(_data.rawDamageData));
        }
        
        void DealDamage(IAlive alive, Collider collider, RawDamageData rawDamageData) {
            DamageParameters damageParams = _data.damageData.Get();
            damageParams.KnockdownType = KnockdownType;
            damageParams.KnockdownStrength = KnockdownStrength;
            Damage damage = new Damage(damageParams, Npc, alive, rawDamageData).WithHitCollider(collider);
            alive.HealthElement.TakeDamage(damage);

            if (_data.statusToApplyOnHit != null && alive is ICharacter character) {
                var statusTemplate = _data.statusToApplyOnHit;
                VGUtils.ApplyStatus(character.Statuses, statusTemplate, StatusSourceInfo.FromStatus(statusTemplate),
                    _data.statusBuildupStrengthIfPossible, _data.statusDurationOverride, null);
            }
        }
    }
}