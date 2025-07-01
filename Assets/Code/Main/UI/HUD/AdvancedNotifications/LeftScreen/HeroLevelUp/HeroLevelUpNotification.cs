using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.HeroLevelUp {
    public partial class HeroLevelUpNotification : Element<HeroLevelUpNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        public readonly int heroLevel;

        public HeroLevelUpNotification(int heroLevel) {
            this.heroLevel = heroLevel;
        }
    }
}