using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Skills;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MeleeBehaviours {
    [Serializable]
    public partial class BlockAndHealBehaviour : EnemyBehaviourBase {
        // === Serialized Fields
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 999)] 
        [SerializeField] int weight = 5;
        
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1)] 
        [SerializeField] CombatBehaviourCooldown cooldown = CombatBehaviourCooldown.None;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1), ShowIf(nameof(ShowCooldownDuration)), LabelText("Duration"), Range(0.1f, 999f), Indent(1)] 
        [SerializeField] float cooldownDuration = 1f;

        [SerializeField] float maxDistanceToInvoke = 5f;
        [SerializeField] float duration = 5f;
        [SerializeField] bool rotateTowardsTarget = true;
        [SerializeField] bool requiresMinimumHealthPercentage;
        [SerializeField, ShowIf(nameof(requiresMinimumHealthPercentage)), Range(0f, 1f)] float minimumHealthPercentage = 0.5f;
        [SerializeField] float healAmountPerSecond = 10f;
        [SerializeField] StatusTemplate bonusStatusTemplate;
        [SerializeField] bool useEndState;
        [SerializeField, ShowIf(nameof(useEndState))] NpcStateType endStateType;

        MovementState _overrideMovementState;
        float _inStateDuration;
        StatTweak _hpRegenStatTweak;
        Status _bonusStatus;
        bool _blocking;
        bool _stopping;

        public override bool IsPeaceful => false;
        public override bool CanBlockDamage => true;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => true;
        protected override CombatBehaviourCooldown Cooldown => cooldown;
        protected override float CooldownDuration => cooldownDuration;
        NpcStateType StateType => _stopping ? endStateType : _blocking ? NpcStateType.BlockHold : NpcStateType.BlockStart;
        MovementState OverrideMovementState => rotateTowardsTarget ? new NoMoveAndRotateTowardsTarget() : new NoMove();

        public override int Weight {
            get {
                int staminaWeight = (int) ParentModel.NpcElement.CharacterStats.Stamina.Percentage.Remap(0, 1, 7, 0);
                int hpWeight = (int) ParentModel.NpcElement.AliveStats.Health.Percentage.Remap(0, 1, 7, 0);
                return weight + (staminaWeight + hpWeight) / 2;
            }
        }
        
        public bool CanBeUsed {
            get {
                if (!requiresMinimumHealthPercentage || Npc.Health.Percentage <= minimumHealthPercentage) {
                    return ParentModel.DistanceToTarget <= maxDistanceToInvoke;
                }
                return false;
            }
        }
        
        protected override bool StartBehaviour() {
            if (OnStart()) {
                _overrideMovementState = ParentModel.NpcMovement.ChangeMainState(OverrideMovementState);
                ParentModel.SetAnimatorState(StateType);
                return true;
            }
            return false;
        }
        
        public override void StopBehaviour() {
            if (_overrideMovementState != null) {
                ParentModel.NpcMovement.ResetMainState(_overrideMovementState);
                _overrideMovementState = null;
            }
            OnStop();
        }
        
        public override void Update(float deltaTime) {
            OnUpdate(deltaTime);
        }
        
        bool OnStart() {
            _inStateDuration = 0;
            return true;
        }

        public void OnUpdate(float deltaTime) {
            var currentType = NpcGeneralFSM.CurrentAnimatorState.Type;
            if (!_blocking && !_stopping) {
                if (currentType == NpcStateType.BlockHold) {
                    StartBlocking();
                    _blocking = true;
                    return;
                }
            }
            
            if (currentType != StateType) {
                StopBlocking();
                return;
            }

            if (_stopping) {
                return;
            }
            
            if (Npc.Health.Percentage >= 1f) {
                StopBlocking();
                return;
            }
            
            _inStateDuration += deltaTime;
            if (_inStateDuration >= duration) {
                StopBlocking();
            }
        }

        public override bool UseConditionsEnsured() => CanBeUsed;

        void OnStop() {
            DisableEffects();
            _stopping = false;
            _blocking = false;
        }

        void DisableEffects() {
            if (_hpRegenStatTweak is { HasBeenDiscarded: false }) {
                _hpRegenStatTweak.Discard();
            }
            if (_bonusStatus is { HasBeenDiscarded: false }) {
                _bonusStatus.Discard();
            }
            _hpRegenStatTweak = null;
            _bonusStatus = null;
        }
        
        void StartBlocking() {
            _blocking = true;
            _hpRegenStatTweak = new StatTweak(Npc.Stat(AliveStatType.HealthRegen), healAmountPerSecond, TweakPriority.AddPreMultiply, OperationType.Add, this);
            if (bonusStatusTemplate != null) {
                _bonusStatus = Npc.Statuses.AddStatus(bonusStatusTemplate, new StatusSourceInfo().WithCharacter(Npc)).newStatus;
            }
        }
        
        void StopBlocking() {
            if (useEndState && !_stopping) {
                _blocking = false;
                _stopping = true;
                ParentModel.SetAnimatorState(endStateType);
                DisableEffects();
            } else {
                ParentModel.TryToStartNewBehaviourExcept(this);
            }
        }
        
        // === Editor
        
        bool ShowCooldownDuration => cooldown == CombatBehaviourCooldown.UntilTimeElapsed;
        
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<BlockAndHealBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => Behaviour.StateType.Yield();

            // === Constructor
            public Editor_Accessor(BlockAndHealBehaviour behaviour) : base(behaviour) { }
        }
    }
}