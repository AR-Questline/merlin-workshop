using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    [Serializable]
    public partial class ChargeThenExplodeBehaviour : MovementBehaviour<EnemyBaseClass> {
        // === Serialized Fields
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 999)] 
        [SerializeField] int weight = 999;
        
        [SerializeField] float explodeWhenInRange = 3.5f;
        [SerializeField] float explodeAfterSeconds = 5f;

        public override int Weight => weight;
        public override bool IsPeaceful => false;

        float _inStateDuration;
        Wander _wander;

        protected override bool StartBehaviour() {
            if (ParentModel.DistanceToTarget <= explodeWhenInRange) {
                Explode();
                return false;
            }
            _wander = new Wander(TargetPosition(), VelocityScheme.Run);
            _wander.OnEnd += Explode;
            ParentModel.NpcMovement.ChangeMainState(_wander);
            ParentModel.SetAnimatorState(NpcStateType.Movement);
            return true;
        }

        public override void Update(float deltaTime) {
            if (ParentModel.NpcMovement.CurrentState != _wander) {
                ParentModel.NpcMovement.ChangeMainState(_wander);
            }
            
            _wander.UpdateDestination(TargetPosition());
            _inStateDuration += deltaTime;
            if (_inStateDuration >= explodeAfterSeconds || ParentModel.DistanceToTarget <= explodeWhenInRange) {
                Explode();
            }
        }

        public override bool UseConditionsEnsured() => true;
        
        CharacterPlace TargetPosition() {
            var target = ParentModel.NpcElement.GetCurrentTarget();
            return new(target.Coords, explodeWhenInRange * 0.5f);
        }

        void Explode() {
            if (!ParentModel.TryStartBehaviour<ExplodeBehaviour>()) {
                Log.Important?.Error($"Failed to invoke ExplodeBehaviour from {this}! This is GameBreaking bug, add ExplodeBehaviour to enemy: {ParentModel.ParentModel.Spec}");
            }
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<ChargeThenExplodeBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.CombatMovement.Yield();

            // === Constructor
            public Editor_Accessor(ChargeThenExplodeBehaviour behaviour) : base(behaviour) { }
        }
    }
}
