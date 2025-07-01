using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class WaitForTargetInStoryBehaviour : PersistentEnemyBehaviour, IInterruptBehaviour {
        const int PriorityConst = CombatBehaviourPriority.Important;

        public override int Weight => 999;
        public override int Priority => PriorityConst;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;
        
        KeepPosition _keepPosition;
        CharacterPlace _targetPosition;

        protected override void OnInitialize() {
            _keepPosition = new KeepPosition(default, VelocityScheme.Trot);
        }

        protected override bool StartBehaviour() {
            _targetPosition = KeepPositionBehaviour.GetTargetPosition(ParentModel, 2.5f);
            _keepPosition.UpdatePlace(_targetPosition);
            ParentModel.NpcMovement.ChangeMainState(_keepPosition);
            ParentModel.SetAnimatorState(NpcStateType.Idle, overrideCrossFadeTime: 0f);
            return true;
        }

        public override void Update(float deltaTime) {
            var npc = ParentModel.NpcElement;
            var target = npc?.GetCurrentTarget();
            _targetPosition = KeepPositionBehaviour.GetTargetPosition(ParentModel, npc, target);
            _keepPosition.UpdatePlace(_targetPosition);
            
            if (!target?.HasElement<DialogueInvisibility>() ?? true) {
                ParentModel.StopCurrentBehaviour(true);
            }
        }

        public override void StopBehaviour() {
            ParentModel.NpcMovement.ResetMainState(_keepPosition);
        }

        public static void Start(ICharacter character) {
            if (character is NpcElement npc && npc.ParentModel.TryGetElement(out EnemyBaseClass enemy)) {
                var behaviour = enemy.CurrentBehaviour.Get();
                if (behaviour is WaitForTargetInStoryBehaviour) {
                    return;
                }

                if (behaviour == null || behaviour.Priority < PriorityConst) {
                    enemy.TryStartBehaviour<WaitForTargetInStoryBehaviour>();
                }
            }
        }

        public static void Stop(ICharacter character) {
            if (
                character is NpcElement npc 
                && npc.ParentModel.TryGetElement(out EnemyBaseClass enemy) 
                && enemy.CurrentBehaviour.Get() is WaitForTargetInStoryBehaviour waitForHeroInStoryBehaviour
            ) {
                if (enemy.NpcAI?.InCombat ?? false) {
                    enemy.TryToStartNewBehaviourExcept(waitForHeroInStoryBehaviour);
                } else {
                    enemy.StopCurrentBehaviour(false);
                }
            }
        }

        public override bool UseConditionsEnsured() {
            return ParentModel.NpcElement?.GetCurrentTarget()?.HasElement<DialogueInvisibility>() ?? false;
        }

        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<WaitForTargetInStoryBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.None.Yield();

            // === Constructor
            public Editor_Accessor(WaitForTargetInStoryBehaviour behaviour) : base(behaviour) { }
        }
    }
}