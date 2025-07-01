using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments.Humanoids;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Grounds;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.RangedBehaviours {
    [Serializable]
    public partial class GetBetterRangedPositionBehaviour : MovementBehaviour<HumanoidCombatBaseClass> {
        // === Serialized Fields
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-2), Range(0, 999)] 
        [SerializeField] int weight = 999;
        
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1)] 
        [SerializeField] CombatBehaviourCooldown cooldown = CombatBehaviourCooldown.None;
        [BoxGroup(BasePropertiesGroup), PropertyOrder(-1), ShowIf(nameof(ShowCooldownDuration)), LabelText("Duration"), Range(0.1f, 999f), Indent(1)] 
        [SerializeField] float cooldownDuration = 1f;
        
        public override int Weight => weight;
        public override bool UseConditionsEnsured() {
            var npcElement = ParentModel.NpcElement;
            var target = npcElement?.GetCurrentTarget();
            return target != null &&
                   !npcElement.IsBlinded &&
                   ParentModel.NpcAI.CanSee(target.AIEntity, false) == VisibleState.Covered;
        }

        protected override CombatBehaviourCooldown Cooldown => cooldown;
        protected override float CooldownDuration => cooldownDuration;
        Wander _wander;

        protected override bool StartBehaviour() {
            var npcElement = ParentModel.NpcElement;
            var target = npcElement.GetCurrentTarget();
            var npcAI = npcElement.NpcAI;
            var betterPositionOption = AIUtils.FindBetterPositionForArcher(npcAI.VisionDetectionOrigin, target.Head.position, 5);
            if (!betterPositionOption.TryGetValue(out var betterPosition)) {
                ParentModel.StartWaitBehaviour();
                return false;
            }

            betterPosition = Ground.SnapNpcToGround(betterPosition) + Vector3.up * 0.1f;
            _wander = new Wander(new CharacterPlace(betterPosition, 0.25f), VelocityScheme.Run, true);
            _wander.OnEnd += ExitBehaviour;
            ParentModel.NpcMovement.ChangeMainState(_wander);
            ParentModel.SetAnimatorState(NpcStateType.Movement);
            return true;
        }

        public override void Update(float deltaTime) {
            if (ParentModel.NpcMovement.CurrentState != _wander) {
                ParentModel.NpcMovement.ChangeMainState(_wander);
            }
        }
        void ExitBehaviour() {
            if (ParentModel == null || ParentModel.HasBeenDiscarded) {
                return;
            }
            
            ParentModel.NpcMovement?.ResetMainState(_wander);
            ParentModel.TryToStartNewBehaviourExcept(this);
        }
        
        // === Editor
        bool ShowCooldownDuration => cooldown == CombatBehaviourCooldown.UntilTimeElapsed;

        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<GetBetterRangedPositionBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.CombatMovement.Yield();

            // === Constructor
            public Editor_Accessor(GetBetterRangedPositionBehaviour behaviour) : base(behaviour) { }
        }
    }
}
