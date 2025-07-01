using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    // Possibly a never discarding element so that we don;t have the costs of constant re-creation
    public sealed partial class DefaultMovement : HumanoidMovementBase {
        public override ushort TypeForSerialization => SavedModels.DefaultMovement;

        public override MovementType Type => MovementType.Default;
    }
}