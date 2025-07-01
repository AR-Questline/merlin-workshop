using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Saving;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class WaitBehaviour : MovementBehaviour<EnemyBaseClass>, IWaitBehaviour {
        // === Serialized Fields
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 999)] 
        [SerializeField] int weight = 0;

        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1)] 
        [SerializeField] CombatBehaviourCooldown cooldown = CombatBehaviourCooldown.None;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1), ShowIf(nameof(ShowCooldownDuration)), LabelText("Duration"), Range(0.1f, 999f), Indent(1)] 
        [SerializeField] float cooldownDuration = 1f;

        [SerializeField] bool exitWhenTargetTooFar = true;
        [SerializeField, ShowIf(nameof(exitWhenTargetTooFar))] float exitAtDistanceToTarget = 5;
        
        [SerializeField] FloatRange waitTimeRange = new(0.5f, 1.5f);
        float _waitDuration;
        
        // === Properties
        public override int Weight => weight;
        public override bool AllowStaminaRegen => true;
        public override bool IsPeaceful => true;
        protected override CombatBehaviourCooldown Cooldown => cooldown;
        protected override float CooldownDuration => cooldownDuration;

        protected override bool StartBehaviour() {
            if (!ParentModel.NpcAI.InCombat) {
                return false;
            }
            
            _waitDuration = waitTimeRange.RandomPick();
            ParentModel.NpcMovement.ChangeMainState(new NoMoveAndRotateTowardsTarget());
            ParentModel.SetAnimatorState(NpcStateType.Wait);
            return true;
        }

        public override void Update(float deltaTime) {
            _waitDuration -= deltaTime;
            if (_waitDuration <= 0 || (exitWhenTargetTooFar && ParentModel.DistanceToTarget > exitAtDistanceToTarget)) {
                ParentModel.StopCurrentBehaviour(true);
            }
        }

        public override bool UseConditionsEnsured() => false;
        
        // === Editor
        bool ShowCooldownDuration => cooldown == CombatBehaviourCooldown.UntilTimeElapsed;

        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<WaitBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.Wait.Yield();

            // === Constructor
            public Editor_Accessor(WaitBehaviour behaviour) : base(behaviour) { }
        }
    }
}