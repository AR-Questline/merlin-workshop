using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class Lockpick : Element<Item>, IRefreshedByAttachment<LockpickAttachment> {
        public override ushort TypeForSerialization => SavedModels.Lockpick;

        public void InitFromAttachment(LockpickAttachment spec, bool isRestored) { }
    }
}