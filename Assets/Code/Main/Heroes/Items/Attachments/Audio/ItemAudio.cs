using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Audio {
    public partial class ItemAudio : Element<Item>, IRefreshedByAttachment<ItemAudioAttachment> {
        public override ushort TypeForSerialization => SavedModels.ItemAudio;

        public SurfaceType ArmorSurfaceType { get; private set; }
        public ItemAudioContainer AudioContainer { get; private set; }

        public void InitFromAttachment(ItemAudioAttachment spec, bool isRestored) {
            ArmorSurfaceType = spec.ArmorSurfaceType;
            AudioContainer = spec.itemAudioContainerWrapper.Data;
        }
    }
}
