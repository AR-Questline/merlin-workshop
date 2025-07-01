using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class RestBehaviour : EnemyBehaviourBase, IBehaviourBase {
        // Serializable Fields
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 999)] 
        [SerializeField] int weight = 30;

        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1)] 
        [SerializeField] CombatBehaviourCooldown cooldown = CombatBehaviourCooldown.None;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1), ShowIf(nameof(ShowCooldownDuration)), LabelText("Duration"), Range(0.1f, 999f), Indent(1)] 
        [SerializeField] float cooldownDuration = 1f;
        
        [SerializeField] float defaultRestDuration = 2.5f;
        [SerializeField] float staminaRegenMultiplier = 2f;

        public override int Weight => weight;

        public override bool CanBeInterrupted => true;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;
        protected override CombatBehaviourCooldown Cooldown => cooldown;
        protected override float CooldownDuration => cooldownDuration;
        LimitedStat Stamina => ParentModel.CharacterStats.Stamina;
        
        StatTweak _staminaRegenTweak;
        KeepPosition _keepPosition;
        float? _durationRemaining;

        protected override bool StartBehaviour() {
            _durationRemaining ??= defaultRestDuration;
            ParentModel.SetAnimatorState(NpcStateType.Idle);
            _staminaRegenTweak = StatTweak.Multi(ParentModel.CharacterStats.StaminaRegen, staminaRegenMultiplier, parentModel: this);

            _keepPosition = new KeepPosition(new CharacterPlace(ParentModel.DesiredPosition, 0.5f), VelocityScheme.Walk);
            ParentModel.NpcElement.Movement.ChangeMainState(_keepPosition, MovementStateOverriden);
            return true;
        }
        
        public override void Update(float deltaTime) {
            _keepPosition.UpdatePlace(new CharacterPlace(ParentModel.DesiredPosition, 0.5f));
            _durationRemaining -= deltaTime;
            if (_durationRemaining is <= 0 or null) {
                ParentModel.StartWaitBehaviour();
            }
        }
        
        public override void StopBehaviour() {
            _durationRemaining = null;
            ParentModel.NpcElement.Movement.ResetMainState(_keepPosition);
            _staminaRegenTweak?.Discard();
            _staminaRegenTweak = null;
        }

        public override bool UseConditionsEnsured() {
            bool staminaCondition = Stamina.Percentage < 0.3f;
            if (ParentModel.NpcElement.IsTargetingHero()) {
                return staminaCondition && ParentModel.InRangeWithCombatSlot(0.5f) && CombatDirector.AnyAttackActionBooked();
            }
            return staminaCondition;
        }

        // === Helpers
        public void UpdateRestDuration(float? duration = null) {
            _durationRemaining = duration ?? defaultRestDuration;
        }

        protected virtual void MovementStateOverriden(MovementState mainState, MovementState from, MovementState to) {
#if DEBUG
            if (!DebugReferences.LogMovementUnsafeOverrides) {
                return;
            }
            Log.Important?.Error($"[{mainState.Npc?.ID}] Movement state {from?.GetType().Name}[from {GetType().Name}], was overriden by state {to?.GetType().Name}");
#endif
        }
        
        // === Editor
        bool ShowCooldownDuration => cooldown == CombatBehaviourCooldown.UntilTimeElapsed;
        
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<RestBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.None.Yield();

            // === Constructor
            public Editor_Accessor(RestBehaviour behaviour) : base(behaviour) { }
        }
    }
}