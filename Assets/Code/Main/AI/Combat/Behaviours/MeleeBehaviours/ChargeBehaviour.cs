using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Saving;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MeleeBehaviours {
    [Serializable]
    public partial class ChargeBehaviour : MovementBehaviour<EnemyBaseClass> {
        // === Serialized Fields
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 100)] 
        [SerializeField] int weight = 30;
        // FOR TODO in UseConditionsEnsured
#pragma warning disable CS0414 // Field is assigned but its value is never used
        [SerializeField] float minDistanceToTarget = 5f;
#pragma warning restore CS0414 // Field is assigned but its value is never used

        public override int Weight => weight;
        public override bool RequiresCombatSlot => true;
        public override bool IsPeaceful => false;

        ICloseRangeAttackBehaviour _selectedBehaviour;
        Wander _wander;
        bool _initialized;
        
        protected override bool StartBehaviour() {
            _selectedBehaviour = ParentModel.Elements<ICloseRangeAttackBehaviour>().MinBy(m => m.MaxDistance);
            if (_selectedBehaviour == null) {
                return false;
            }
            _wander = new Wander(TargetPlace(), VelocityScheme.Run);
            ParentModel.NpcMovement.Controller.ForceForwardMovement(true);
            ParentModel.NpcMovement.ChangeMainState(_wander);
            ParentModel.SetAnimatorState(NpcStateType.Movement);
            _initialized = true;
            return true;
        }

        public override void Update(float deltaTime) {
            if (!_initialized) {
                return;
            }

            if (ParentModel.NpcMovement.CurrentState != _wander) {
                ParentModel.NpcMovement.ChangeMainState(_wander);
            }
            
            _wander.UpdateDestination(TargetPlace());
            
            if (ParentModel.DistanceToTarget < VHeroCombatSlots.CombatSlotOffset * 2f) {
                CombatDirector.BookAttackAction(ParentModel);
            }

            if (_selectedBehaviour.InRange) {
                ParentModel.StartBehaviour(_selectedBehaviour);
            }
        }
        
        public override void StopBehaviour() {
            _initialized = false;
            ParentModel.NpcMovement.Controller.ForceForwardMovement(false);
            ParentModel.NpcMovement.ResetMainState(_wander);
            _wander = null;

            CombatDirector.UnBookAttackAction(ParentModel);
        }

        public override void BehaviourInterrupted() {
            CombatDirector.UnBookAttackAction(ParentModel);
            base.BehaviourInterrupted();
        }

        public override bool UseConditionsEnsured() {
            // TODO: Temporarly disabled, maybe this behaviour should be removed.
            return false;
            // bool combatSlotCondition = true;
            // if (ParentModel.Target is Hero h) {
            //     combatSlotCondition = ParentModel.OwnedCombatSlot != null && !h.CombatSlots.ChargeActionBooked;
            // }
            //
            // bool behaviourCondition = ParentModel.HasElement<ICloseRangeAttackBehaviour>();
            // return ParentModel.DistanceToTarget > minDistanceToTarget && combatSlotCondition && behaviourCondition;
        }

        CharacterPlace TargetPlace() {
            var targetPos = ParentModel.NpcElement.GetCurrentTarget().Coords;
            return new CharacterPlace(targetPos, 0.5f);
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<ChargeBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.CombatMovement.Yield();

            // === Constructor
            public Editor_Accessor(ChargeBehaviour behaviour) : base(behaviour) { }
        }
    }
}
