using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments.Humanoids;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class RunBackBehaviour : RunBackBehaviourBase<HumanoidCombatBaseClass> {
        // === Serialized Fields
        [SerializeField] float minDistanceToInvoke = 3.5f;
        [SerializeField] float distanceToStop = 6f;
        [SerializeField] float distanceToStopWhenRanged = 7.5f;
        [SerializeField] float forceExitAfterSecondsElapsed = 5f;
        
        // === Properties
        protected override float DistanceToStop => IsRanged ? distanceToStopWhenRanged : distanceToStop;
        bool IsRanged => ParentModel.CurrentFighterType == FighterType.Ranged;

        public override void Update(float deltaTime) {
            _duration += deltaTime;
            _wander.UpdateDestination(TargetPlace());

            if (ParentModel.DistanceToTarget > DistanceToStop) {
                ParentModel.StartWaitBehaviour();
                return;
            }

            if (IsRanged && ParentModel.DistanceToTarget < ParentModel.MeleeRangedSwitchDistance.min) {
                ParentModel.TrySwitchWeapons(true, false, false);
                return;
            }

            if (_duration > forceExitAfterSecondsElapsed) {
                ParentModel.StopCurrentBehaviour(true);
            }
        }

        public override bool UseConditionsEnsured() => ParentModel.DistanceToTarget <= minDistanceToInvoke;
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<RunBackBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.CombatMovement.Yield();

            // === Constructor
            public Editor_Accessor(RunBackBehaviour behaviour) : base(behaviour) { }
        }
    }
}