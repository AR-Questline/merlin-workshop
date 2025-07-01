using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class ShieldManKeepPositionBehaviour : KeepPositionBehaviour {
        [SerializeField, Range(0f, 1f)] float angularSpeedModifier = 0.5f;
        [SerializeField] FloatRange waitTimeInStateToPerformNextAttack = new(3.5f, 5.5f);
        [BoxGroup("Distance Parameters"), SerializeField] bool distanceCondition;
        [BoxGroup("Distance Parameters"), SerializeField, ShowIf(nameof(distanceCondition))] float minDistance;
        [BoxGroup("Distance Parameters"), SerializeField, ShowIf(nameof(distanceCondition))] float maxDistance = 7f;
        float _minimumTimeInState;
        
        public override bool CanBlockDamage => true;
        protected override bool CanChangeBehaviourWhenInCorrectPosition => false;
        protected override float MinimumTimeInStateToStartNewBehaviour => _minimumTimeInState;

        public override bool UseConditionsEnsured() {
            var distanceToTarget = ParentModel.DistanceToTarget;
            bool inRange = !distanceCondition || (distanceToTarget >= minDistance && distanceToTarget<= maxDistance);
            return inRange && base.UseConditionsEnsured();
        }

        protected override void OnInitialize() {
            _minimumTimeInState = waitTimeInStateToPerformNextAttack.RandomPick();
        }
        
        protected override void CreateMovementState() {
            if (changeVelocityOnDistance) {
                _keepPosition = new ShieldManKeepPosition(_targetPosition, CloseVelocity, maxStrafeDistance, distanceToChangeVelocity, ChaseVelocity, invertRotationSchemes);
            } else {
                _keepPosition = new ShieldManKeepPosition(_targetPosition, ChaseVelocity, maxStrafeDistance);
            }
        }

        protected override bool StartBehaviour() {
            NpcAngularSpeedMultiplier.AddAngularSpeedMultiplier(Npc, angularSpeedModifier, new UntilEndOfCombatBehaviour(this));
            _minimumTimeInState = waitTimeInStateToPerformNextAttack.RandomPick();
            return base.StartBehaviour();
        }

        protected override void VelocitySchemeChanged(VelocityScheme velocityScheme) {
            if (velocityScheme == CloseVelocity) {
                ParentModel.SetAnimatorState(NpcStateType.ShieldManMovement);
            } else {
                ParentModel.SetAnimatorState(NpcStateType.Movement);
            }
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<ShieldManKeepPositionBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => new[]
                { NpcStateType.CombatIdle, NpcStateType.CombatMovement, NpcStateType.ShieldManMovement };

            // === Constructor
            public Editor_Accessor(ShieldManKeepPositionBehaviour behaviour) : base(behaviour) { }
        }
    }
}