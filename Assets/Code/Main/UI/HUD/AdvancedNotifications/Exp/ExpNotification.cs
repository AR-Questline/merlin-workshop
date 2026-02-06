namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Exp {
    public partial class ExpNotification : AdvancedNotification {
        public float gainedXP;
        
        public override bool IsMergeable => true;
        
        public ExpNotification(float gainedXP) {
            this.gainedXP = gainedXP;
        }
        
        public void OverrideGainedExp(float newGainedXP) {
            gainedXP = newGainedXP;
        }
    }
}