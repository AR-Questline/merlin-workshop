using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.UI.Menu.DeathUI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class TeleportHeroOnHeroKill : Element<Location>, IRefreshedByAttachment<TeleportHeroOnHeroKillAttachment> {
        public override ushort TypeForSerialization => SavedModels.TeleportHeroOnHeroKill;

        TeleportHeroOnHeroKillAttachment _spec;

        public void InitFromAttachment(TeleportHeroOnHeroKillAttachment spec, bool isRestored) {
            _spec = spec;
        }

        public void HeroKilled() {
            World.Add(new DeathUI(_spec.targetScene, _spec.indexTag));
        }
    }
}