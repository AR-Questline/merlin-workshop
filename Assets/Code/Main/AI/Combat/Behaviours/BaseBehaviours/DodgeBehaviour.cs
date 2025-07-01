using System;
using System.Collections.Generic;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Fights.Archers;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class DodgeBehaviour : EnemyBehaviourBase, IInterruptBehaviour {
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1)] 
        [SerializeField] CombatBehaviourCooldown cooldown = CombatBehaviourCooldown.None;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1), ShowIf(nameof(ShowCooldownDuration)), LabelText("Duration"), Range(0.1f, 999f), Indent(1)] 
        [SerializeField] float cooldownDuration = 1f;
        
        [SerializeField] bool canInterruptAttacks = true;
        [SerializeField, Range(0.1f, 99f), ShowIf(nameof(canInterruptAttacks))] float interruptMeleeCooldown = 5f;
        [SerializeField, Range(0.1f, 99f), ShowIf(nameof(canInterruptAttacks))] float interruptRangedCooldown = 2f;

        [SerializeField, MinMaxSlider(0, 20, true)]
        Vector2 meleeDetectionRange = new(0.0f, 6.0f);

        [SerializeField] float meleeDodgeChance = 0.5f;

        [SerializeField, MinMaxSlider(0, 50, true)]
        Vector2 rangedDetectionRange = new(10.0f, 30.0f);

        [SerializeField, MinMaxSlider(0, 50, true)]
        Vector2 guaranteedRangedDodgeRange = new(20.0f, 30.0f);

        [SerializeField] float rangedDodgeChance = 0.5f;
        [SerializeField] Vector2 rangedTargetSize = new Vector2(2.0f, 3.0f);
        [SerializeField] float rangedMaxTimeToHitForDodge = 0.7f;

        [SerializeField, Range(0f, 1f)] float sideDodgeRandomness = 0.05f;

        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;
        protected override CombatBehaviourCooldown Cooldown => cooldown;
        protected override float CooldownDuration => cooldownDuration;
            
        float _attackInpterruptedUntilTime;
        NpcStateType _stateToEnter = NpcStateType.DodgeBack;

        IEventListener _meleeAttackListener;
        IEventListener _rangedAttackListener;

        protected override void OnInitialize() {
            base.OnInitialize();
            Npc.ListenTo(AITargetingUtils.Relations.Targets.Events.AfterAttached, OnTargetAcquired, this);
            Npc.ListenTo(AITargetingUtils.Relations.Targets.Events.BeforeDetached, OnTargetLost, this);
            var target = ParentModel.NpcElement?.GetCurrentTarget();
            if (target != null) {
                AttachTargetListeners(target);
            }
        }

        public override bool UseConditionsEnsured() {
            return false;
        }

        void OnTargetAcquired(RelationEventData data) {
            if (data.to is ICharacter target) {
                var currentValidTarget = ParentModel.NpcElement?.GetCurrentTarget();
                if (currentValidTarget == null) {
                    return;
                }

                if (target != currentValidTarget) {
                    Log.Important?.Error($"DodgeBehaviour target mismatch. Expected {currentValidTarget}, got {target}");
                    return;
                }
                AttachTargetListeners(target);
            }
        }

        void AttachTargetListeners(ICharacter target) {
            var hero = Hero.Current;
            if (target == hero) {
                _meleeAttackListener = hero.ListenTo(Hero.Events.HeroAttacked, OnHeroTargetAttacking, this);
            } else {
                _meleeAttackListener = target.ListenTo(ICharacter.Events.OnAttackStart, OnNpcTargetAttacking, this);
            }

            _rangedAttackListener = target.ListenTo(ICharacter.Events.OnFiredProjectile, OnTargetFiring, this);
        }

        void OnTargetLost(RelationEventData data) {
            World.EventSystem.TryDisposeListener(ref _rangedAttackListener);
            World.EventSystem.TryDisposeListener(ref _meleeAttackListener);
        }

        void OnHeroTargetAttacking() {
            if (ParentModel.NpcElement?.IsTargetingHero() ?? false) {
                var hero = Hero.Current;
                OnTargetAttacking(hero.Forward(), hero);
            }
        }
        void OnNpcTargetAttacking(AttackParameters attack) {
            var target = ParentModel.NpcElement?.GetCurrentTarget();
            OnTargetAttacking(attack.AttackDirection, target);
        }

        void OnTargetAttacking(Vector3 attackDirection, ICharacter target) {
            if (target is not { HasBeenDiscarded: false, IsAlive: true } || !CanInvokeDodge()) {
                return;
            }
            
            Vector3 deltaFromTarget = ParentModel.Coords - target.Coords;
            float distanceFromTarget = deltaFromTarget.magnitude;
            
            if (!ShouldDodgeMelee(distanceFromTarget, target)) {
                return;
            }

            Vector3 dirFromTarget = deltaFromTarget / distanceFromTarget;
            bool attackingTowardsUs = Vector3.Dot(attackDirection, dirFromTarget) > 0.0f;
            if (attackingTowardsUs) {
                AttemptToDodge(NpcStateType.DodgeBack, false);
            }
        }

        void OnTargetFiring(DamageDealingProjectile projectile) {
            if (!CanInvokeDodge()) {
                return;
            }
            
            Vector3 deltaFromTarget = Npc.Torso.position - projectile.transform.position;
            float distanceFromTarget = deltaFromTarget.magnitude;

            if (!ShouldDodgeRanged(distanceFromTarget)) {
                return;
            }

            float accuracyFactor = GetAimAccuracyFactor(deltaFromTarget, projectile.Velocity, projectile.UsesGravity);
            
            if (Mathf.Abs(accuracyFactor) <= 1f) {
                float horizontalSpeed = projectile.Velocity.ToHorizontal2().magnitude;
                float approxTimeToReach = deltaFromTarget.ToHorizontal2().magnitude / horizontalSpeed;

                float timeToWaitForDodge = Mathf.Max(0f, approxTimeToReach - rangedMaxTimeToHitForDodge);
                
                float directionRandomness = RandomUtil.UniformFloat(-sideDodgeRandomness, sideDodgeRandomness);
                bool shouldDodgeLeft = accuracyFactor + directionRandomness < 0.0f;
                NpcStateType dodgeState = shouldDodgeLeft ? NpcStateType.DodgeLeft : NpcStateType.DodgeRight;
                
                DodgeProjectileAfterTime(timeToWaitForDodge, dodgeState).Forget();
            }
        }

        // Returns signed factor describing how accurate the aim is, with 0 being perfect aim.
        float GetAimAccuracyFactor(Vector3 shootingDelta, Vector3 projectileVelocity, bool useGravity) {
            float projectileSpeed = projectileVelocity.magnitude;
            Vector3 projectileDirection = projectileVelocity / projectileSpeed;
            
            float targetDistance = shootingDelta.magnitude;
            Vector3 targetDirection;
            if (useGravity) {
                var shotData = new ShotData(Vector3.zero, shootingDelta, projectileSpeed, false);
                targetDirection = ArcherUtils.ShotVelocity(shotData).normalized;
            } else {
                targetDirection = shootingDelta / targetDistance;
            }
            
            float deltaAngle = Vector3.SignedAngle(targetDirection, projectileDirection, Vector3.up);
            float maxAngle = Mathf.Atan2(rangedTargetSize.x, targetDistance) * Mathf.Rad2Deg;
            
            float rangedTargetSizeProportion = rangedTargetSize.y / rangedTargetSize.x;
            float verticalDeviationFactor = Vector3.Cross(targetDirection, projectileDirection).y;
            maxAngle *= Mathf.Lerp(1.0f, rangedTargetSizeProportion, verticalDeviationFactor);

            return deltaAngle / maxAngle;
        }

        async UniTask DodgeProjectileAfterTime(float time, NpcStateType dodgeType) {
            if (!await AsyncUtil.DelayTime(Npc, time)) {
                return;
            }

            AttemptToDodge(dodgeType, true);
        }

        void AttemptToDodge(NpcStateType dodgeType, bool isRanged) {
            bool currentBehaviourPeaceful = ParentModel.CurrentBehaviour.Get()?.IsPeaceful ?? true;
            bool interruptingAttack = !currentBehaviourPeaceful;
            bool attackInterruptOnCooldown = Time.time < _attackInpterruptedUntilTime;
            bool canInterruptNow = canInterruptAttacks && !attackInterruptOnCooldown;
            
            if (interruptingAttack && !canInterruptNow) {
                return;
            }
            
            ParentModel.AttemptToDodge(dodgeType);
            if (interruptingAttack) {
                float interruptCooldown = isRanged ? interruptRangedCooldown : interruptMeleeCooldown;
                PreventAttackInterruptsFor(interruptCooldown);
            }
        }
        
        void PreventAttackInterruptsFor(float time) {
            _attackInpterruptedUntilTime = Time.time + time;
        }
        
        bool CanInvokeDodge() {
            return !DisabledForever && !HasElement<EnemyBehaviourCooldown>();
        }

        bool ShouldDodgeMelee(float attackDistance, ICharacter target) {
            bool chance = RandomUtil.UniformFloat(0.0f, 1.0f) <= meleeDodgeChance;
            return chance && IsWithinMeleeDetectionRange(attackDistance) && CanSeeTarget(target);

            bool IsWithinMeleeDetectionRange(float distance) {
                return distance >= meleeDetectionRange.x && distance <= meleeDetectionRange.y;
            }
        }

        bool ShouldDodgeRanged(float attackDistance) {
            bool chance = RandomUtil.UniformFloat(0.0f, 1.0f) <= rangedDodgeChance;
            bool isNonGuaranteedChance = chance && IsWithinRangedDetectionRange(attackDistance);
            bool isGuaranteed = IsWithinGuaranteedRangedDodgeRange(attackDistance);
            return (isNonGuaranteedChance || isGuaranteed) && CanSeeTarget(ParentModel.NpcElement?.GetCurrentTarget());

            bool IsWithinRangedDetectionRange(float distance) {
                return distance >= rangedDetectionRange.x && distance <= rangedDetectionRange.y;
            }

            bool IsWithinGuaranteedRangedDodgeRange(float distance) {
                return distance >= guaranteedRangedDodgeRange.x && distance <= guaranteedRangedDodgeRange.y;
            }
        }

        bool CanSeeTarget(ICharacter target) {
            IAIEntity targetAi = target switch {
                Hero hero => hero,
                NpcElement npc => npc.NpcAI,
                _ => null
            };
            if (targetAi == null) return false;

            return AIEntity.CanSee(targetAi, false) == VisibleState.Visible;
        }

        protected override bool StartBehaviour() {
            ParentModel.NpcMovement.ChangeMainState(new Observe());
            ParentModel.SetAnimatorState(_stateToEnter);
            return true;
        }

        public override void Update(float deltaTime) {
            if (NpcGeneralFSM.CurrentAnimatorState.Type != _stateToEnter) {
                OnAnimatorExitDesiredState();
            }
        }

        void OnAnimatorExitDesiredState() {
            ParentModel.StartWaitBehaviour();
        }

        // === Public API
        public void UpdateDodgeDirection(NpcStateType stateType) {
            _stateToEnter = stateType;
        }

        // === Editor

        bool ShowCooldownDuration => cooldown == CombatBehaviourCooldown.UntilTimeElapsed;

        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<DodgeBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => new[] {
                NpcStateType.DodgeBack,
                NpcStateType.DodgeLeft,
                NpcStateType.DodgeRight,
            };

            // === Constructor
            public Editor_Accessor(DodgeBehaviour behaviour) : base(behaviour) { }
        }
    }
}
