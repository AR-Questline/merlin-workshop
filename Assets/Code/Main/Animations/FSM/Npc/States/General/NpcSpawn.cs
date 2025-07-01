using Awaken.CommonInterfaces.Animations;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcSpawn : NpcAnimatorState<NpcGeneralFSM>, IAnimatorBridgeStateProvider {
        public override ushort TypeForSerialization => SavedModels.NpcSpawn;

        public bool AlwaysAnimate => true;
        public override NpcStateType Type => NpcStateType.Spawn;
        public override bool CanUseMovement => Npc.CanMoveInSpawn;
        public override bool CanBeExited => _canBeExited;
        public override float EntryTransitionDuration => 0f;
        
        bool _canBeExited = true;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _canBeExited = false;
            Npc.Trigger(NpcElement.Events.AnimatorEnteredSpawnState, Npc);
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                _canBeExited = true;
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }
    }
}