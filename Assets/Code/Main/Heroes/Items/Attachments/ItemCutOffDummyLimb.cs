using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemCutOffDummyLimb : ItemOnTakeFromDummyBase, IRefreshedByAttachment<ItemCutOffDummyLimbAttachment> {
        public override ushort TypeForSerialization => SavedModels.ItemCutOffDummyLimb;

        LimbData _limbData;
        
        public void InitFromAttachment(ItemCutOffDummyLimbAttachment spec, bool isRestored) {
            _limbData = spec.limbData;
        }
        
        protected override void OnTakenFromDummy(NpcDummy dummy) {
            dummy.AddElement(new DummyLimbCutOff(_limbData));
            this.Discard();
        }
    }
}