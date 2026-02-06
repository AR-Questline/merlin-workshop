using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemCutOffDummyHead : ItemOnTakeFromDummyBase {
        public override ushort TypeForSerialization => SavedModels.ItemCutOffDummyHead;

        protected override void OnTakenFromDummy(NpcDummy dummy) {
            dummy.AddElement(new DummyHeadCutOff());
            this.Discard();
        }
    }
}