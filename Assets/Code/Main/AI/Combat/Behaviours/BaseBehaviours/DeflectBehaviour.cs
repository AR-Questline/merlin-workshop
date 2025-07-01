using System;
using System.Collections.Generic;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class DeflectBehaviour : EnemyBehaviourBase, IInterruptBehaviour {
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1)] 
        [SerializeField] CombatBehaviourCooldown cooldown = CombatBehaviourCooldown.None;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1), ShowIf(nameof(ShowCooldownDuration)), LabelText("Duration"), Range(0.1f, 999f), Indent(1)] 
        [SerializeField] float cooldownDuration = 1f;
        
        [SerializeField, Range(0, 1f)] float magicDeflectChance = 0.5f;
        [SerializeField, Range(0, 1f)] float physicalDeflectChance = 0.5f;
        [SerializeField] bool deflectTowardsTarget;
        [SerializeField] float deflectPrecision = 0.75f;
        
        bool _isMagic;
        MovementState _previousMovementState;
        StatTweak _deflectPrecisionTweak;

        public override bool CanMove => true;
        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;
        protected override CombatBehaviourCooldown Cooldown => cooldown;
        protected override float CooldownDuration => cooldownDuration;

        bool CanDeflect => !HasElement<EnemyBehaviourCooldown>()
                           && Npc.NpcAI.InCombat
                           && (ParentModel.CurrentBehaviour.TryGet(out IBehaviourBase currentBehaviour) == false ||
                               currentBehaviour.CanBeInterrupted);
        NpcStateType StateType => _isMagic ? NpcStateType.DeflectProjectileMagic : NpcStateType.DeflectProjectilePhysical;

        protected override void OnInitialize() {
            ParentModel.NpcElement.HealthElement.ListenTo(HealthElement.Events.TakingDamage, OnTakingDamage, this);
        }

        void OnTakingDamage(HookResult<HealthElement, Damage> hook) {
            Projectile projectile = hook.Value.Projectile;
            if (projectile == null) {
                return;
            }

            if (!CanDeflect) {
                return;
            }

            _isMagic = hook.Value.Type == DamageType.MagicalHitSource;
            
            float chance = RandomUtil.UniformFloat(0, 1f);
            bool deflect = (_isMagic && magicDeflectChance > chance) || (!_isMagic && physicalDeflectChance > chance);
            if (deflect) {
                _previousMovementState = ParentModel.NpcMovement.CurrentState;
                
                if (ParentModel.StartBehaviour(this)) {
                    hook.Prevent();
                    CharacterProjectileDeflection.GetOrCreate(Npc).DeflectProjectile(hook.Value, projectile, !deflectTowardsTarget);
                }
            }
        }

        protected override bool StartBehaviour() {
            ParentModel.SetAnimatorState(StateType, NpcFSMType.TopBodyFSM, 0f);
            if (_previousMovementState != null) {
                ParentModel.NpcMovement.ChangeMainState(_previousMovementState);
            }
            _deflectPrecisionTweak = StatTweak.Override(ParentModel.CharacterStats.DeflectPrecision, deflectPrecision, parentModel: this);
            return true;
        }
        
        public void NotInCombatUpdate(float deltaTime) {
            VerifyCurrentState();
        }

        public override void Update(float deltaTime) {
            VerifyCurrentState();
        }

        void VerifyCurrentState() {
            if (NpcTopBodyFSM.CurrentAnimatorState.Type != StateType) {
                ParentModel.StartWaitBehaviour();
            }
        }

        protected override void BehaviourExit() {
            if (_previousMovementState != null) {
                ParentModel.NpcMovement.ResetMainState(_previousMovementState);
                _previousMovementState = null;
            }
            
            _deflectPrecisionTweak?.Discard();
            _deflectPrecisionTweak = null;
        }

        public override bool UseConditionsEnsured() => false;

        // === Editor
        bool ShowCooldownDuration => cooldown == CombatBehaviourCooldown.UntilTimeElapsed;
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);
        public new class Editor_Accessor : Editor_Accessor<DeflectBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => new[]
                { NpcStateType.DeflectProjectilePhysical, NpcStateType.DeflectProjectileMagic };

            // === Constructor
            public Editor_Accessor(DeflectBehaviour behaviour) : base(behaviour) { }
        }
    }
}