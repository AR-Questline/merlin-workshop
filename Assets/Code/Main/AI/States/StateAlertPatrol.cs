using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;

namespace Awaken.TG.Main.AI.States {
    public class StateAlertPatrol : NpcState<StateAlert> {
        Patrol _patrol;

        static VelocityScheme VelocityScheme => VelocityScheme.Walk;

        [UnityEngine.Scripting.Preserve] Item EquippedItem => Npc.Inventory.EquippedItem(EquipmentSlotType.MainHand)
                                                             ?? Npc.Inventory.EquippedItem(EquipmentSlotType.OffHand);
        public override void Init() {
            _patrol = new Patrol(CharacterPlace.Default, Data.alert.SearchRadius, VelocityScheme);
        }

        protected override void OnEnter() {
            base.OnEnter();
            Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Idle);
            //AI.InAlertWithWeapons = Parent.WalkWithWeapons;
            _patrol.UpdatePlace(AI.AlertTarget);
            Movement.ChangeMainState(_patrol);
            AI.AlertStack.AlertVisionGain = (int) AlertStack.AlertStrength.Max;
        }

        public override void Update(float deltaTime) {
            _patrol.UpdateVelocityScheme(VelocityScheme);
        }

        protected override void OnExit() {
            base.OnExit();
            Movement.ResetMainState(_patrol);
        }
    }
}