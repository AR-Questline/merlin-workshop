using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Utility.VFX {
    /// <summary>
    /// This element is used for overriding default VFX's for IAlive. It can be useful for enemies like ghosts.
    /// </summary>
    public partial class AliveVfx : Element<IModel>, IRefreshedByAttachment<AliveVfxAttachment> {
        public override ushort TypeForSerialization => SavedModels.AliveVfx;

        public ItemVfxContainer VfxContainer { get; private set; }
        
        public void InitFromAttachment(AliveVfxAttachment spec, bool isRestored) {
            VfxContainer = spec.vfxWrapper.Data;
        }
    }
}
