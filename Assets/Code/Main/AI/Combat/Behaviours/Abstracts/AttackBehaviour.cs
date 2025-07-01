using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Controls;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    public abstract partial class AttackBehaviour<T> : AttackBehaviour where T : EnemyBaseClass {
        protected new T ParentModel => base.ParentModel as T;
    }

    [Serializable]
    public abstract partial class AttackBehaviour : CombatEnemyBehaviourBase, IBehaviourBase {
        [SerializeField] bool preventMovement;
        [SerializeField] bool preventRotation;
        [SerializeField] bool canBlockDamage;
        [SerializeField] protected bool leaveToKeepPosition;
        [SerializeField] bool exposeWeakspot;

        public override bool CanMove => !preventMovement; 
        public override bool CanBeInterrupted => true;
        public override bool CanBeAggressive => true;
        public override bool IsPeaceful => false;
        public override bool CanBlockDamage => canBlockDamage;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        protected override bool ExposeWeakspot => exposeWeakspot;
        protected abstract NpcStateType StateType { get; }
        protected abstract MovementState OverrideMovementState { get; }
        protected virtual float? OverrideCrossFadeTime => null;
        protected virtual float StaminaCost => 0;
        protected virtual IDuration PreventRotationDuration => new UntilEndOfAttack(Npc, CharacterWeapon.AngularSpeedModifierDurationExtend);
        Stat Stamina => ParentModel.CharacterStats?.Stamina;
        DifficultySetting DifficultySetting {
            get {
                if (_difficultySetting == null || _difficultySetting.HasBeenDiscarded) {
                    _difficultySetting = World.Only<DifficultySetting>();
                }
                return _difficultySetting;
            }
        }
        
        DifficultySetting _difficultySetting;
        MovementState _overrideMovementState;

        public override bool UseConditionsEnsured() {
            if (RequiresCombatSlot && ParentModel.NpcElement.IsTargetingHero()) {
                bool combatSlot = ParentModel.OwnedCombatSlotIndex != -1;
                bool attackersCondition = CombatDirector.AttackActionsBooked() < DifficultySetting.Difficulty.MaxEnemiesAttacking;
                if (!combatSlot || !attackersCondition) {
                    return false;
                }
            }

            return base.UseConditionsEnsured();
        }

        public void NotInCombatUpdate(float deltaTime) {
            if (NpcGeneralFSM.CurrentAnimatorState.Type != StateType) {
                OnAnimatorExitDesiredState();
            }
        }
        
        public override void Update(float deltaTime) {
            NotInCombatUpdate(deltaTime);
            OnUpdate(deltaTime);
        }
        
        protected override bool StartBehaviour() {
            if (OnStart()) {
                _overrideMovementState = ParentModel.NpcMovement.ChangeMainState(OverrideMovementState, MovementStateOverriden);
                ParentModel.SetAnimatorState(StateType, overrideCrossFadeTime: OverrideCrossFadeTime);
                if (RequiresCombatSlot) {
                    CombatDirector.BookAttackAction(ParentModel);
                }
                if (preventRotation) {
                    NpcAngularSpeedMultiplier.AddAngularSpeedMultiplier(Npc, 0, PreventRotationDuration);
                }
                AfterStart();
                return true;
            }
            return false;
        }

        public override void StopBehaviour() {
            // Notify allies about fight next to them after attack
            var npc = ParentModel.NpcElement;
            var ai = npc.NpcAI;
            var target = npc.GetCurrentTarget();
            if (ai != null && target != null) {
                ai.NotifyAlliesAboutOngoingFight(target);
            }
            if (_overrideMovementState != null) {
                ParentModel.NpcMovement.ResetMainState(_overrideMovementState);
                _overrideMovementState = null;
            }
            OnStop();
            ApplyStaminaCost();
        }

        void ApplyStaminaCost() {
            if (StaminaCost > 0 && Stamina != null) {
                Stamina.DecreaseBy(StaminaCost, new ContractContext(ParentModel.NpcElement, ParentModel.NpcElement, ChangeReason.AttackBehaviour));
                PreventStaminaRegenDuration.Prevent(ParentModel.NpcElement, new TimeDuration(0.5f));
            }
        }

        protected abstract bool OnStart();
        protected virtual void AfterStart() { }
        public virtual void OnUpdate(float deltaTime) { }
        public virtual void OnStop() { }
        public override void BehaviourInterrupted() { }
        protected override void BehaviourExit() {            
            CombatDirector.UnBookAttackAction(ParentModel);
        }

        protected virtual void OnAnimatorExitDesiredState() {
            if (leaveToKeepPosition && ParentModel.TryStartBehaviour<KeepPositionBehaviour>()) {
                return;
            } 
            ParentModel.StartWaitBehaviour();
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
        
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<AttackBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => Behaviour.StateType.Yield();
            // === Constructor
            public Editor_Accessor(AttackBehaviour behaviour) : base(behaviour) { }
        }
    }
}
