using System;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Combat;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    [Serializable]
    public partial class PreventDamageBehaviour : AttackBehaviour<EnemyBaseClass> {
        const float PercentageMultiplier = 100;

        [SerializeField] bool preventAllDamage;
        [SerializeField] bool canBeAloneInCombat;
        [SerializeField] bool canBeInterruptedByDamage;
        [SerializeField, ShowIf(nameof(canBeInterruptedByDamage))] float damageToInterrupt;
        [SerializeField] float maxHealthPercentToStart;
        [SerializeField] float duration;
        [SerializeField] bool renewDurationOnHit;
        [SerializeField, OnValueChanged(nameof(OnRequirementChanged))] bool requireAmountOfHitsToTrigger;
        [SerializeField, ShowIf(nameof(requireAmountOfHitsToTrigger))] int amountOfHits = 3;
        [SerializeField, OnValueChanged(nameof(OnRequirementChanged))] bool requireHpLoseToTrigger;
        [SerializeField, ShowIf(nameof(requireHpLoseToTrigger))] float hpLosePercentage = 25f;
        [SerializeField, ShowIf(nameof(ShowRequireBothToTrigger))] bool requireBothToTrigger;
        [SerializeField] protected NpcStateType animatorPrepareStateType = NpcStateType.PreventDamageEnter;
        [SerializeField, HideIf(nameof(HideLoopState))] NpcStateType animatorStateType = NpcStateType.PreventDamageLoop;
        [SerializeField] NpcStateType animatorEndStateType = NpcStateType.PreventDamageExit;
        [SerializeField] protected NpcStateType animatorInterruptedStateType = NpcStateType.PreventDamageInterrupt;

        NpcPreventDamage _preventDamageElement;
        
        public override bool CanBeUsed {
            get {
                if (ParentModel.NpcElement?.GetCurrentTarget() == null) {
                    return false;
                }

                if (!canBeAloneInCombat && Hero.Current.PossibleAttackers.Count() == 1) {
                    return false;
                }
                
                if (maxHealthPercentToStart < ParentModel.NpcElement.HealthElement.Health.Percentage * PercentageMultiplier) {
                    return false;
                }

                if (!requireHpLoseToTrigger && !requireAmountOfHitsToTrigger) {
                    return true;
                }
                
                if (RequireBothToTrigger) {
                    return AmountsOfHitTriggered && HpLoseTriggered;
                } 
                if (requireHpLoseToTrigger && HpLoseTriggered) {
                    return true;
                }
                if (requireAmountOfHitsToTrigger && AmountsOfHitTriggered) {
                    return true;
                }
                return false;
            }
        }

        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        protected override NpcStateType StateType => _isPreparing ? animatorPrepareStateType : animatorStateType;
        protected override MovementState OverrideMovementState => new NoMove();
        public override bool CanBeAggressive => true;
        public override bool IsPeaceful => false;
        protected override IDuration PreventRotationDuration => new UntilEndOfCombatBehaviour(this);
        protected virtual bool HideLoopState => false;
        protected virtual bool ListenToDamagePrevented => canBeInterruptedByDamage || renewDurationOnHit;
        bool ShowRequireBothToTrigger => requireHpLoseToTrigger && requireAmountOfHitsToTrigger;
        bool RequireBothToTrigger => requireBothToTrigger;
        bool AmountsOfHitTriggered => _amountOfHitsToTrigger <= 0;
        bool HpLoseTriggered => ParentModel.NpcElement.HealthElement.Health.Percentage <= _hpLoseToTrigger;

        protected NpcAnimatorState CurrentAnimatorState => ParentModel.NpcElement.GetAnimatorSubstateMachine(NpcFSMType.GeneralFSM).CurrentAnimatorState;

        float _inStateDuration;
        protected bool _isExiting;
        protected bool _isPreparing;
        bool _isPreventionActive;
        float _damageTaken;
        float _hpLoseToTrigger;
        int _amountOfHitsToTrigger;
        IEventListener _damagePreventedByHitboxListener;
        IEventListener _damagePreventedByHookListener;

        protected override void OnInitialize() {
            base.OnInitialize();
            _hpLoseToTrigger = 1f - hpLosePercentage / 100f;
            _amountOfHitsToTrigger = amountOfHits;
            if (requireAmountOfHitsToTrigger) {
                ParentModel.NpcElement.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, OnDamageTaken, this);
            }
        }

        protected override bool OnStart() {
            if (ListenToDamagePrevented) {
                _damagePreventedByHitboxListener ??= ParentModel.NpcElement.HealthElement.ListenTo(HealthElement.Events.DamagePreventedByHitbox, OnDamagePrevented, this);
                _damagePreventedByHookListener ??= ParentModel.NpcElement.HealthElement.ListenTo(HealthElement.Events.DamagePreventedByHook, OnDamagePrevented, this);
            }
            
            ParentModel.SetAnimatorState(animatorPrepareStateType);
            ParentModel.NpcMovement.ChangeMainState(OverrideMovementState);
            _isExiting = false;
            _isPreparing = true;
            _isPreventionActive = false;
            _damageTaken = 0f;
            _inStateDuration = 0f;

            return true;
        }
        
        protected virtual void OnDamagePrevented(Damage damage) {
            _damageTaken += damage.Amount;
            if (renewDurationOnHit) {
                _inStateDuration = 0f;
            }
        }

        public override void OnUpdate(float deltaTime) {
            if (_isExiting) {
                bool isExitingFinished = CurrentAnimatorState.Type != animatorEndStateType && CurrentAnimatorState.Type != animatorInterruptedStateType;
                if (isExitingFinished) {
                    ChangeBehaviourAfterExit();
                }

                return;
            }
            
            if (!_isPreparing) {
                if (!_isPreventionActive) {
                    if (preventAllDamage) {
                        _preventDamageElement ??= Npc.AddElement(new NpcPreventDamage(true));
                    } else {
                        ParentModel.Trigger(EnemyBaseClass.Events.TogglePreventDamageState, true);
                    }

                    _isPreventionActive = true;
                    OnPreventionStarted();
                }
                
                _inStateDuration += deltaTime;
                
                if (_inStateDuration > duration) {
                    Exit(animatorEndStateType);
                }

                if (canBeInterruptedByDamage && _damageTaken > damageToInterrupt) {
                    Exit(animatorInterruptedStateType);
                }
            }
        }
        
        void OnDamageTaken(DamageOutcome damageOutcome) {
            if (damageOutcome.Damage.IsPrimary) {
                _amountOfHitsToTrigger--;
            }
        }

        void ChangeBehaviourAfterExit() {
            if (leaveToKeepPosition && ParentModel.TryStartBehaviour<KeepPositionBehaviour>()) {
                return;
            }

            ParentModel.StartWaitBehaviour();
        }

        protected virtual void Exit(NpcStateType exitState) {
            if (CurrentAnimatorState is PreventDamageStateLoop loopState) {
                loopState.Leave(exitState);
            }
            
            _isExiting = true;
        }

        protected override void BehaviourExit() {
            ParentModel.NpcElement.Movement.StopInterrupting();
            if (preventAllDamage) {
                _preventDamageElement?.Discard();
                _preventDamageElement = null;
            } else {
                ParentModel.Trigger(EnemyBaseClass.Events.TogglePreventDamageState, false);
            }

            _hpLoseToTrigger = ParentModel.NpcElement.HealthElement.Health.Percentage - hpLosePercentage / 100f;
            _amountOfHitsToTrigger = amountOfHits;
            
            World.EventSystem.TryDisposeListener(ref _damagePreventedByHitboxListener);
            World.EventSystem.TryDisposeListener(ref _damagePreventedByHookListener);
            
            base.BehaviourExit();
        }
        
        protected override void OnAnimatorExitDesiredState() {
            _isPreparing = false;
        }
        
        protected virtual void OnPreventionStarted() { }
        
        // Helpers

        void OnRequirementChanged() {
            if (!ShowRequireBothToTrigger) {
                requireBothToTrigger = false;
            }
        }
    }
}