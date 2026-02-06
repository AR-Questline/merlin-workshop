using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Locations.Actions {
    public struct InfoFrame {
        public string displayName;
        public bool isButtonActive;
        
        public static InfoFrame Empty => new(string.Empty, false);

        public InfoFrame(string displayName, bool isButtonActive) {
            this.displayName = displayName;
            this.isButtonActive = isButtonActive;
        }
        
        public static InfoFrame DisabledByCombat => new(LocTerms.PortalBlockedByCombat.Translate(), false);
    }
}