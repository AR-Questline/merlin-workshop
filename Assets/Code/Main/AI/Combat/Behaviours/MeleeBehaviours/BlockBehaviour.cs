using System;
using Awaken.TG.Main.AI.Combat.Attachments.Humanoids;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Saving;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MeleeBehaviours {
    [Serializable]
    public partial class BlockBehaviour : AttackBehaviour<HumanoidCombatBaseClass> {
        // === Serialized Fields
        [SerializeField] float staminaCost = 10;
        [SerializeField] bool usesShield;
        [SerializeField] float duration = 5f;
        
        public override int Weight {
            get {
                int staminaWeight = (int) ParentModel.NpcElement.CharacterStats.Stamina.Percentage.Remap(0, 1, 7, 0);
                int hpWeight = (int) ParentModel.NpcElement.AliveStats.Health.Percentage.Remap(0, 1, 7, 0);
                return base.Weight + (staminaWeight + hpWeight) / 2;
            }
        }

        public override bool CanBeUsed {
            get {
                if (!usesShield && ParentModel.NpcElement.IsTargetingHero()) {
                    // Don't block when there is no other enemy that is attacking Hero
                    return ParentModel.InRangeWithCombatSlot(0.5f) && CombatDirector.AnyAttackActionBooked();
                }
                return true;
            }
        }
        
        public override bool CanBlockDamage => true;
        protected override NpcStateType StateType => NpcStateType.BlockStart;
        protected override MovementState OverrideMovementState => new NoMoveAndRotateTowardsTarget();
        protected override float StaminaCost => staminaCost;
        [UnityEngine.Scripting.Preserve] NpcElement NpcElement => ParentModel.NpcElement;
        float _inStateDuration;

        protected override bool OnStart() {
            _inStateDuration = 0;
            return true;
        }

        public override void OnUpdate(float deltaTime) {
            _inStateDuration += deltaTime;
            if (_inStateDuration >= duration) {
                ParentModel.TryToStartNewBehaviourExcept(this);
            }
        }

        public override void OnStop() {
            ParentModel.NpcElement.SetAnimatorState(NpcFSMType.TopBodyFSM, NpcStateType.None);
        }
    }
}