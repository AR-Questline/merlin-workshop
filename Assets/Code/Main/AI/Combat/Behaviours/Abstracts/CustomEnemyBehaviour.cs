using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Saving;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    public abstract partial class CustomEnemyBehaviour<T> : EnemyBehaviourBase where T : EnemyBaseClass {
        public override bool CanBeAggressive => true;
        protected new T ParentModel => base.ParentModel as T;
        protected abstract NpcStateType StateType { get; }
        protected virtual NpcFSMType FSMType => NpcFSMType.GeneralFSM;
        
        protected override bool StartBehaviour() {
            if (OnStart()) {
                ParentModel.SetAnimatorState(StateType, FSMType);
                return true;
            }
            return false;
        }
        
        protected virtual bool OnStart() { return true; }
        
        // === Editor
        public override EnemyBehaviourBase.Editor_Accessor GetEditorAccessor() => new Editor_Accessor(this);

        public new class Editor_Accessor : Editor_Accessor<CustomEnemyBehaviour<T>> {
            public override IEnumerable<NpcStateType> StatesUsedByThisBehaviour => Behaviour.StateType.Yield();

            // === Constructor
            public Editor_Accessor(CustomEnemyBehaviour<T> behaviour) : base(behaviour) { }
        }
    }
}