using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcDeath : NpcAnimatorState<NpcOverridesFSM> {
        public override ushort TypeForSerialization => SavedModels.NpcDeath;

        public override NpcStateType Type => AnimType switch {
            DeathAnimType.Custom => NpcStateType.CustomDeath,
            DeathAnimType.Finisher => NpcStateType.CustomFinisherDeath,
            _ => NpcStateType.Death
        };
        public override bool CanUseMovement => false;
        public override bool CanBeExited => false;

        DeathAnimType AnimType => ParentModel?.DeathAnimType ?? DeathAnimType.Default;

        public static DeathAnimType GetHigherPriority(DeathAnimType a, DeathAnimType b) {
            return a > b ? a : b;
        }

        public enum DeathAnimType : byte {
            Default = 0,
            Custom = 1,
            Finisher = 2,
        }
    }
}