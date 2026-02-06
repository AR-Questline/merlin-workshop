using Awaken.TG.MVC;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Fishing {
    public partial class FishCaughtNotification : AdvancedNotification {
        public readonly FishCaughtData data;

        public FishCaughtNotification(FishCaughtData data) {
            this.data = data;
        }
        
        public override void Show() {
            World.SpawnView<VFishCaughtNotification>(this, true);
        }
    }
}