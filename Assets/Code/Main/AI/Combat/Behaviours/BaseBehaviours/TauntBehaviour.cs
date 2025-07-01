using System;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class TauntBehaviour : AttackBehaviour {
        // === Serialized Fields
        [SerializeField] float tryToStartNewBehaviourAtDistance = 1.8f;
        [SerializeField] bool allowStaminaRegen = true;

        public override bool CanBeUsed {
            get {
                bool heroTargetsCondition = false;
                if (ParentModel.NpcElement.IsTargetingHero()) {
                    heroTargetsCondition = CombatDirector.AnyAttackActionBooked() && ParentModel.InRangeWithCombatSlot(0.5f);
                }
                return heroTargetsCondition && _canBeUsedAgain;
            }
        }
        public override bool AllowStaminaRegen => allowStaminaRegen;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;
        protected override NpcStateType StateType => NpcStateType.Taunt;
        protected override MovementState OverrideMovementState => _observe;
        Observe _observe = new();
        
        bool _canBeUsedAgain;

        public override void CopyPropertiesTo(Model behaviourBase) {
            ((TauntBehaviour)behaviourBase)._observe = new Observe();
            base.CopyPropertiesTo(behaviourBase);
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(EnemyBaseClass.Events.BehaviourStarted, OnBehaviourStarted, this);
        }

        protected override bool OnStart() {
            _canBeUsedAgain = false;
            ParentModel.DecreaseFatigue();
            return true;
        }

        void OnBehaviourStarted(IBehaviourBase combatBehaviour) {
            if (combatBehaviour != this && combatBehaviour is AttackBehaviour) {
                _canBeUsedAgain = true;
            }
        }
        
        public override void OnUpdate(float deltaTime) {
            if (ParentModel.NpcMovement.CurrentState != _observe) {
                ParentModel.NpcMovement.ChangeMainState(_observe);
            }
            
            if (ParentModel.DistanceToTarget <= tryToStartNewBehaviourAtDistance) {
                ParentModel.TryToStartNewBehaviourExcept(this);
            }
        }

        protected override void OnAnimatorExitDesiredState() {
            ParentModel.StartWaitBehaviour();
        }
    }
}
