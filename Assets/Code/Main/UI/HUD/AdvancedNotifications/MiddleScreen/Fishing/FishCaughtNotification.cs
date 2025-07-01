using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Fishing {
    public partial class FishCaughtNotification : Element<MiddleScreenNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        public readonly FishCaughtData data;

        public FishCaughtNotification(FishCaughtData data) {
            this.data = data;
        }
        
        public void Show() {
            World.SpawnView<VFishCaughtNotification>(this, true);
        }
    }
}