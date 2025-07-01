using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.RangedBehaviours;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MeleeBehaviours {
    [Serializable]
    public partial class MeleeAttackBehaviour : AttackBehaviour, ICloseRangeAttackBehaviour {
        const float RunThresholdVelocity = 0.88f;
        const float RunThresholdVelocitySqr = RunThresholdVelocity * RunThresholdVelocity;
        const float InterruptExitDuration = 0.5f;
        
        // === Serialized Fields
        [SerializeField] float angleToPerformNextAttackInCombo = 45f;
        [SerializeField, Range(0f, 1f)] float chanceForComboAttack = 0.25f;
        [SerializeField] float staminaCost = 10f;
        [SerializeField, ShowIf(nameof(ShowComboState))] float comboAttackStaminaCost = 25f;
        [SerializeField] NpcStateType attackAnimatorState = NpcStateType.ShortRange;
        [SerializeField, ShowIf(nameof(ShowComboState))] NpcStateType comboAttackAnimatorState = NpcStateType.ShortRangeCombo;
        [SerializeField] bool canUseRunAttack = true;
        [SerializeField, ShowIf(nameof(canUseRunAttack))] NpcStateType runningAttackAnimatorState = NpcStateType.LongRangeRunning;

        public override bool CanBeUsed => CanSeeTarget();
        public override bool RequiresCombatSlot => true;
        protected override NpcStateType StateType {
            get {
                if (_wasRunning && canUseRunAttack) {
                    return runningAttackAnimatorState;
                }
                return _useCombo ? comboAttackAnimatorState : attackAnimatorState;
            }
        }
        protected override MovementState OverrideMovementState => new NoMoveAndRotateTowardsTarget();

        protected override float StaminaCost => _useCombo ? comboAttackStaminaCost : staminaCost;

        int _attacksInSequence;
        bool _wasRunning;
        bool _useCombo;
        bool _isInterrupting;
        CancellationTokenSource _interruptCancellationToken;

        protected override bool OnStart() {
            _attacksInSequence = 0;
            _wasRunning = ParentModel.NpcMovement.Controller.CurrentVelocity.sqrMagnitude > RunThresholdVelocitySqr;
            _useCombo = RandomUtil.WithProbability(chanceForComboAttack);
            ParentModel.IncreaseFatigue();
            
            _interruptCancellationToken?.Cancel();
            _interruptCancellationToken = null;
            _isInterrupting = false;
            
            return true;
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (_isInterrupting) {
                return;
            }
            
            if (animationEvent.actionType == ARAnimationEvent.ActionType.AttackRelease) {
                var npcElement = ParentModel.NpcElement;
                var target = npcElement?.GetCurrentTarget();
                if (target == null) {
                    InterruptAttack().Forget();
                    return;
                }
                
                if (target == Hero.Current && npcElement.AIEntity.CanSee(target.AIEntity, false) != VisibleState.Visible) {
                    InterruptAttack().Forget();
                    return;
                }
                
                _attacksInSequence++;
                if (_attacksInSequence <= 1) {
                    return;
                }
                
                Vector3 directionToTarget = target.Coords - ParentModel.Coords;
                if (Vector3.Angle(npcElement.Forward(), directionToTarget) > angleToPerformNextAttackInCombo) {
                    InterruptAttack().Forget();
                }
            }
        }

        protected override void OnAnimatorExitDesiredState() {
            if (_isInterrupting) {
                return;
            }
            
            var throwItemBehaviour = ParentModel.TryGetElement<ThrowItemBehaviour>();
            if (throwItemBehaviour != null && RandomUtil.WithProbability(throwItemBehaviour.TriggeringChanceAfterMelee) && throwItemBehaviour.UseConditionsEnsured()) {
                ParentModel.StartBehaviour(throwItemBehaviour);
            } else {
                throwItemBehaviour?.PreventUsageTillNextAttackBehaviour();
                base.OnAnimatorExitDesiredState();
            }
        }

        async UniTaskVoid InterruptAttack() {
            _interruptCancellationToken?.Cancel();
            _interruptCancellationToken = new CancellationTokenSource();
            
            _isInterrupting = true;
            ParentModel.NpcElement.Trigger(EnemyBaseClass.Events.AttackInterrupted, true);
            ParentModel.SetAnimatorState(NpcStateType.Wait, NpcFSMType.GeneralFSM, InterruptExitDuration);
            
            if (await AsyncUtil.DelayTime(this, InterruptExitDuration, source: _interruptCancellationToken)) {
                _isInterrupting = false;
            }
        }

        bool CanSeeTarget() {
            var target = ParentModel.NpcElement.GetCurrentTarget();
            return target != Hero.Current || AIEntity.CanSee(target.AIEntity, false) == VisibleState.Visible;
        }

        // === Editor
        bool ShowComboState => chanceForComboAttack > 0;
        
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<MeleeAttackBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour {
                get {
                    yield return Behaviour.attackAnimatorState;
                    if (Behaviour.canUseRunAttack) {
                        yield return Behaviour.runningAttackAnimatorState;
                    }

                    if (Behaviour.ShowComboState) {
                        yield return Behaviour.comboAttackAnimatorState;
                    }
                }
            }
            
            // === Constructor
            public Editor_Accessor(MeleeAttackBehaviour meleeAttackBehaviour) : base(meleeAttackBehaviour) { }
        }
    }
}
