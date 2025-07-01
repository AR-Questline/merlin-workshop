using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Housing {
    public partial class FurnitureSlot : FurnitureSlotBase<FurnitureSlotAttachment> {
        public override ushort TypeForSerialization => SavedModels.FurnitureSlot;
    }
}