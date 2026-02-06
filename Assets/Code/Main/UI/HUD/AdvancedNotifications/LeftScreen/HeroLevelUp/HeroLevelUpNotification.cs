namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.HeroLevelUp {
    public partial class HeroLevelUpNotification : AdvancedNotification {
        public readonly int heroLevel;

        public HeroLevelUpNotification(int heroLevel) {
            this.heroLevel = heroLevel;
        }
    }
}