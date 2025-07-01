using System;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.RangedBehaviours;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Saving;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    [Serializable]
    public partial class AttackThenThrowRock : AttackBehaviour<EnemyBaseClass> {
        // Serializable Fields
        [SerializeField] float staminaCost = 10;
        [SerializeField] NpcStateType animatorState = NpcStateType.MediumRange;
        
        public override bool CanBeUsed => true;
        protected override NpcStateType StateType => animatorState;
        protected override MovementState OverrideMovementState => new NoMoveAndRotateTowardsTarget();
        protected override float StaminaCost => staminaCost;

        protected override bool OnStart() {
            return true;
        }

        protected override void OnAnimatorExitDesiredState() {
            if (!ParentModel.TryStartBehaviour<ThrowItemBehaviour>()) {
                ParentModel.StopCurrentBehaviour(true);
            }
        }
    }
}