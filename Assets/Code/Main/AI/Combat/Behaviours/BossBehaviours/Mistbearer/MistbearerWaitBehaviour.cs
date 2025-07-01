using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments.Bosses;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Saving;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BossBehaviours.Mistbearer {
    [Serializable]
    public partial class MistbearerWaitBehaviour : MovementBehaviour<MistbearerCombatBase>, IWaitBehaviour {
        [SerializeField] FloatRange waitTimeRange = new(2.5f, 5f);
        
        // === Properties
        public override int Weight => 0;
        public override bool AllowStaminaRegen => true;
        float _waitDuration;
        
        protected override bool StartBehaviour() {
            _waitDuration = waitTimeRange.RandomPick();
            ParentModel.NpcMovement.ChangeMainState(new NoMoveAndRotateTowardsTarget());
            return true;
        }
        
        public override void Update(float deltaTime) {
            _waitDuration -= deltaTime;
            if (_waitDuration <= 0) {
                ParentModel.TryToStartNewBehaviour();
            }
        }

        public override bool UseConditionsEnsured() => false;
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<MistbearerWaitBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.None.Yield();

            // === Constructor
            public Editor_Accessor(MistbearerWaitBehaviour behaviour) : base(behaviour) { }
        }
    }
}