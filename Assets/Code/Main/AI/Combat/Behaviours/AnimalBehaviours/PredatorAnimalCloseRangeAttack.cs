using System;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Combat.Behaviours.MeleeBehaviours;

namespace Awaken.TG.Main.AI.Combat.Behaviours.AnimalBehaviours {
#pragma warning disable AR0002
    [Serializable]
    public partial class PredatorAnimalCloseRangeAttack : MeleeAttackBehaviour {
        protected override bool OnStart() => true;
        public override bool RequiresCombatSlot => false;

        protected override void OnAnimatorExitDesiredState() {
            if (leaveToKeepPosition && ParentModel.TryStartBehaviour<KeepPositionBehaviour>()) {
                return;
            } 
            ParentModel.StartWaitBehaviour();
        }
    }
#pragma warning restore AR0002
}