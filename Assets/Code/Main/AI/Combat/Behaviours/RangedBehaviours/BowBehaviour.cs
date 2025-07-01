using System;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments.Humanoids;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.RangedBehaviours {
    [Serializable]
    public partial class BowBehaviour : AttackBehaviour<HumanoidCombatBaseClass> {
        // === Serialized Fields
        [SerializeField] NpcStateType stateType = NpcStateType.RangedAttack;
        [SerializeField] bool overrideFireAngleRange;
        [SerializeField, ShowIf(nameof(overrideFireAngleRange))] FloatRange fireAngleRange = new(-60f, 60f);
        [SerializeField] float maxArrowVelocity = 33f;
        [SerializeField] float arrowVelocityMultiplier = 1.5f;
        [SerializeField] bool predictPlayerMovement = true;
        [SerializeField, Range(0, 1f), InfoBox("0 - aim at enemy feet, 1 - aim at enemy head")] float aimAtEnemyHeight = 0.8f;
        [SerializeField] [UnityEngine.Scripting.Preserve] GameObject arrowProjectilePrefab; //TODO It's unused but is set and probably should be reworked.
        [SerializeField] NpcDamageData damageData = NpcDamageData.DefaultRangedAttackData;
        
        public override bool CanBeUsed => CanSeeTarget();
        protected override NpcStateType StateType => stateType;
        protected override MovementState OverrideMovementState => new NoMoveAndRotateTowardsTarget();

        CancellationTokenSource _cancellationToken;
        IPooledInstance _arrowPrefab;
        CharacterBow _rangedWeapon;
        Vector3 _fakeTarget;

        protected override bool OnStart() {
            _rangedWeapon = ParentModel.NpcElement.CharacterView.transform.GetComponentInChildren<CharacterBow>(true);
            return true;
        }

        public override void OnUpdate(float deltaTime) {
            if (ParentModel.DistanceToTarget < ParentModel.MeleeRangedSwitchDistance.min) {
                ParentModel.TrySwitchWeapons(true, false, false);
            }
        }

        public override void OnStop() {
            ReturnArrowPrefab();
            if (_rangedWeapon != null) {
                _rangedWeapon.OnBowIdle();
                _rangedWeapon = null;
            }
        }

        public override void BehaviourInterrupted() {
            OnStop();
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackStart) {
                SpawnArrowInHand().Forget();
            } else if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                SpawnProjectile();
            }
        }
        
        protected override void OnAnimatorExitDesiredState() {
            if (CanBeInvoked) {
                ParentModel.StartWaitBehaviour();
                return;
            }

            var betterPos = ParentModel.TryGetElement<GetBetterRangedPositionBehaviour>();
            if (betterPos != null && betterPos.UseConditionsEnsured() && ParentModel.StartBehaviour(betterPos)) {
                return;
            }
            ParentModel.StopCurrentBehaviour(true);
        }

        public void SetArrowPrefab(GameObject arrowPrefab) {
            arrowProjectilePrefab = arrowPrefab;
        }

        public void SetFakeTarget(Vector3 fakeTarget) {
            _fakeTarget = fakeTarget;
        }

        public void SetFakeBow(CharacterBow fakeBow) {
            _rangedWeapon?.ResetBowDrawSpeed();
            _rangedWeapon = fakeBow;
        }

        bool CanSeeTarget() {
            var target = ParentModel.NpcElement?.GetCurrentTarget();
            return target != null && ParentModel.NpcAI.CanSee(target.AIEntity, false) != VisibleState.Covered;
        }
        
        // === Helpers

        public async UniTaskVoid SpawnArrowInHand(float? bowDrawSpeed = null) {
            if (_rangedWeapon != null) {
                _rangedWeapon.OnPullBow(bowDrawSpeed);
            }

            _cancellationToken = new CancellationTokenSource();
            if (ParentModel.ItemProjectile is { } itemProjectile) {
                _arrowPrefab = await itemProjectile.GetInHandProjectile(ParentModel.NpcElement.MainHand, _cancellationToken);
            } else {
                _arrowPrefab = await ItemProjectile.GetCustomInHandProjectile(ParentModel.CustomProjectileData.visualPrefab, ParentModel.NpcElement.MainHand, _cancellationToken);
            }
        }
        
        public void SpawnProjectile() {
            var npcElement = ParentModel.NpcElement;
            var spawnPosition = _arrowPrefab?.Instance != null ?
                _arrowPrefab.Instance.transform.position :
                npcElement.MainHand.position;
            
            var parentView = npcElement.CharacterView;
            var target = npcElement.GetCurrentTarget();
            var shootPos = target?.Coords ?? _fakeTarget;
            var additionalVelocityMultiplier = (shootPos.y - ParentModel.Coords.y).Remap(0, 10, 0, 3, true);
            var fireParams = new CombatBehaviourUtils.FireProjectileParams {
                shooterView = parentView,
                target = target,
                shootPos = shootPos,
                fireAngleRange = overrideFireAngleRange ? fireAngleRange : CombatBehaviourUtils.DefaultFireAngleRange,
                aimAtEnemyHeight = aimAtEnemyHeight,
                maxVelocity = maxArrowVelocity,
                velocityMultiplier = arrowVelocityMultiplier + additionalVelocityMultiplier,
                predictPlayerMovement = predictPlayerMovement,
                parabolicShot = true
            };
            var shootParams = VGUtils.ShootParams.Default;
            shootParams.shooter = npcElement;
            if (ParentModel.ItemProjectile is { } itemProjectile) {
                shootParams = shootParams.WithItem(itemProjectile.ParentModel);
            } else {
                shootParams = shootParams.WithCustomProjectile(ParentModel.CustomProjectileData);
            }
            shootParams.startPosition = spawnPosition;
            shootParams.projectileSlotType = EquipmentSlotType.Quiver;
            shootParams.rawDamageData = damageData.GetRawDamageData(Npc);
            shootParams.damageTypeData = damageData.GetDamageTypeData(Npc);
            CombatBehaviourUtils.FireProjectile(fireParams, shootParams);

            ReturnArrowPrefab();
            if (_rangedWeapon != null) {
                _rangedWeapon.OnReleaseBow();
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            ReturnArrowPrefab();
        }
        
        // === Helpers
        public void ReturnArrowPrefab() {
            _cancellationToken?.Cancel();
            _cancellationToken = null;
            _arrowPrefab?.Return();
            _arrowPrefab = null;
        }
    }
}