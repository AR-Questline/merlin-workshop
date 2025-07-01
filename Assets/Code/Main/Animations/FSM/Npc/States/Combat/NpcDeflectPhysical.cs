using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcDeflectPhysical : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcDeflectPhysical;

        public override NpcStateType Type => NpcStateType.DeflectProjectilePhysical;
        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(ParentModel is NpcTopBodyFSM ? NpcStateType.None : NpcStateType.Idle, 0.3f);
            }
        }

    }
}