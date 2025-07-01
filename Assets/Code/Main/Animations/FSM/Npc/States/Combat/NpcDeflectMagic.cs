using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcDeflectMagic : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcDeflectMagic;

        public override NpcStateType Type => NpcStateType.DeflectProjectileMagic;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(ParentModel is NpcTopBodyFSM ? NpcStateType.None : NpcStateType.Idle, 0.3f);
            }
        }
    }
}