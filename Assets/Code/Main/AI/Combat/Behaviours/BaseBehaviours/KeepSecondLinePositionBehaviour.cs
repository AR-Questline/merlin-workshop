using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Utility.RichEnums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class KeepSecondLinePositionBehaviour : EnemyBehaviourBase, IBehaviourBase {
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 999)] 
        [SerializeField] int weight = 15;
        [SerializeField] float escapeFromTargetAtDistance = 5f;
        [SerializeField, RichEnumExtends(typeof(VelocityScheme))] RichEnumReference velocityScheme = VelocityScheme.Run;

        protected KeepPositionInfrequent _keepPosition;
        
        public override int Weight => weight;
        public override bool CanBeInterrupted => true;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => true;
        public override bool CanBeAggressive => true;
        public override bool IsPeaceful => true;

        CharacterPlace TargetPosition => ParentModel.TryGetCombatSlotPosition(out var combatSlotPosition)
            ? new CharacterPlace(combatSlotPosition, KeepPositionBehaviour.TargetPositionAcceptRange)
            : new CharacterPlace(ParentModel.Coords, KeepPositionBehaviour.TargetPositionAcceptRange);
        
        protected override bool StartBehaviour() {
            _keepPosition = new KeepPositionInfrequent(velocityScheme.EnumAs<VelocityScheme>(), TargetPosition, escapeFromTargetAtDistance);
            ParentModel.NpcMovement.ChangeMainState(_keepPosition);
            ParentModel.SetAnimatorState(NpcStateType.Idle);
            return true;
        }

        public override void Update(float deltaTime) {
            if (!ParentModel.TryToStartNewBehaviourExcept(this)) {
                _keepPosition.UpdatePlace(TargetPosition);
            }
        }

        public override bool UseConditionsEnsured() => true;

        protected override void BehaviourExit() {
            ParentModel.NpcMovement.ResetMainState(_keepPosition);
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<KeepSecondLinePositionBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => new[] { NpcStateType.CombatIdle, NpcStateType.CombatMovement };

            // === Constructor
            public Editor_Accessor(KeepSecondLinePositionBehaviour behaviour) : base(behaviour) { }
        }
    }
}