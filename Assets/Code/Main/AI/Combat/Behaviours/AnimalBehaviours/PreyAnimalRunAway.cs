using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;

namespace Awaken.TG.Main.AI.Combat.Behaviours.AnimalBehaviours {
    [Serializable]
    public partial class PreyAnimalRunAway : EnemyBehaviourBase {
        public override int Weight => 999;
        public override int Priority => CombatBehaviourPriority.MostImportant;
        public override bool UseConditionsEnsured() => true;
        public override bool CanBeInterrupted => true;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeAggressive => false;
        public override bool IsPeaceful => true;
        Flee _flee;
        
        protected override bool StartBehaviour() {
            _flee = new Flee(ParentModel.NpcElement?.GetCurrentTarget());
            ParentModel.NpcMovement.ChangeMainState(_flee);
            ParentModel.SetAnimatorState(NpcStateType.Movement);
            return true;
        }
        
        public override void Update(float deltaTime) { }
        
        public override void StopBehaviour() {
            ParentModel.NpcMovement.ResetMainState(_flee);
            _flee = null;
        }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<PreyAnimalRunAway> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => Array.Empty<NpcStateType>();

            // === Constructor
            public Editor_Accessor(PreyAnimalRunAway behaviour) : base(behaviour) { }
        }
    }
}
