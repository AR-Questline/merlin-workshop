using System;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    [Serializable]
    public partial class MeleeAttackHammerSmash : KnockBackAttackBehaviour {
        // === Serializable Fields
        [SerializeField] float staminaCost = 10;
        [SerializeField] bool allowRotationToTarget;
        
        public override bool RequiresCombatSlot => true;
        public override bool CanBeUsed => true;
        protected override float StaminaCost => staminaCost;
        protected override NpcStateType StateType => animatorStateType;
        protected override MovementState OverrideMovementState => allowRotationToTarget ? new NoMoveAndRotateTowardsTarget() : new NoMove();

        protected override bool OnStart() {
            return true;
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                SpawnDamageSphere();
            }
        }
    }
}