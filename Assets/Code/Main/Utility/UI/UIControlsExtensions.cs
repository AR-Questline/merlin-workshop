using Awaken.Utility.GameObjects;
using TMPro;

namespace Awaken.TG.Main.Utility.UI {
    public static class UIControlsExtensions {
        public const string PlaceholderText = "Lorem ipsum";
        
        public static void SetActiveAndText(this TMP_Text target, bool active, string text) {
            target.TrySetActiveOptimized(active);
            target.SetText(text);
        }

        public static void TrySetText(this TMP_Text target, string text, bool fillWithPlaceholderIfEmpty = false) {
            if (target == null) {
                return;
            }
            
            if (!string.IsNullOrEmpty(text)) {
                target.SetText(text);
            } else if (fillWithPlaceholderIfEmpty) {
                target.SetText(PlaceholderText);
            }
        }
    }
}
