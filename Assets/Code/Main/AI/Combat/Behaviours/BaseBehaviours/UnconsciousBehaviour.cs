using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class UnconsciousBehaviour : EnemyBehaviourBase, IInterruptBehaviour {
        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;

        RagdollMovement _ragdollMovement;
        
        IEventListener _unconsciousListener;
        bool _started;

        protected override bool StartBehaviour() {
            if (_ragdollMovement == null || _started) {
                return false;
            }
            
            _started = true;
            ParentModel.NpcMovement.InterruptState(_ragdollMovement);
            ParentModel.SetAnimatorState(NpcStateType.Idle, overrideCrossFadeTime: 0f);
            _unconsciousListener = ParentModel.NpcElement.ListenTo(UnconsciousElement.Events.RegainConscious, _ => ParentModel.StopCurrentBehaviour(false), this);
            return true;
        }

        public override void Update(float deltaTime) { }

        public override void StopBehaviour() {
            if (_unconsciousListener != null) {
                World.EventSystem.RemoveListener(_unconsciousListener);
                _unconsciousListener = null;
            }

            if (_started) {
                _started = false;
                _ragdollMovement.ExitRagdoll();
            }
        }
        
        public override void BehaviourInterrupted() {
            Log.Important?.Error("Trying to Interrupt Unconscious Behaviour! This is not valid! Please Fix!");
        }

        public override bool UseConditionsEnsured() => false;
        
        // === Helpers
        public void SetRagdollParams(RagdollMovement ragdollMovement) {
            _ragdollMovement = ragdollMovement;
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<UnconsciousBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => NpcStateType.None.Yield();

            // === Constructor
            public Editor_Accessor(UnconsciousBehaviour behaviour) : base(behaviour) { }
        }
    }
}