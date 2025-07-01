using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class StaggerBehaviour : EnemyBehaviourBase, IInterruptBehaviour {
        const float StaggerDuration = 5f;

        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;
        public float DurationElapsedNormalized => (_duration - _durationRemaining).Remap(0, _duration, 0, 1, true);
        LimitedStat Stamina => ParentModel.CharacterStats.Stamina;

        bool _isExiting;
        float _duration, _durationRemaining;

        protected override bool StartBehaviour() {
            _duration = _durationRemaining;
            _isExiting = false;
            ParentModel.SetAnimatorState(NpcStateType.StaggerEnter);

            var npc = ParentModel.NpcElement;
            npc.Movement.InterruptState(new NoMove());
            npc.Trigger(EnemyBaseClass.Events.Staggered, npc);
            npc.ListenTo(EnemyBaseClass.Events.StaggerAnimExitEnded, ParentModel.StartWaitBehaviour, this);
            return true;
        }
        
        public override void Update(float deltaTime) {
            _durationRemaining -= deltaTime;
            if (!_isExiting && _durationRemaining <= 0) {
                ExitStagger();
            }
        }

        public override void StopBehaviour() {
            _durationRemaining = 0;
            ParentModel.NpcElement.Movement.StopInterrupting();
            Stamina.SetToFull();
        }

        public override bool UseConditionsEnsured() => false;
        
        // === Helpers
        public void UpdateStaggerDuration(float? duration = null) {
            _durationRemaining = duration ?? StaggerDuration;
        }
        
        void ExitStagger() {
            _isExiting = true;
            ParentModel.SetAnimatorState(NpcStateType.StaggerExit);
        }

        protected virtual void MovementStateOverriden(MovementState mainState, MovementState from, MovementState to) {
#if DEBUG
            if (!DebugReferences.LogMovementUnsafeOverrides) {
                return;
            }
            Log.Important?.Error($"[{mainState.Npc?.ID}] Movement state {from?.GetType().Name}[from {GetType().Name}], was overriden by state {to?.GetType().Name}");
#endif
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<StaggerBehaviour> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => new[]
                { NpcStateType.StaggerEnter, NpcStateType.StaggerLoop, NpcStateType.StaggerExit };

            // === Constructor
            public Editor_Accessor(StaggerBehaviour behaviour) : base(behaviour) { }
        }
    }
}
