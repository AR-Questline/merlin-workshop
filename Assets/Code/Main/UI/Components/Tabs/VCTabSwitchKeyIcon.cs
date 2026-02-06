using Awaken.TG.Main.Utility.UI.Keys.Components;

namespace Awaken.TG.Main.UI.Components.Tabs {
    public class VCTabSwitchKeyIcon : VCKeyIcon<VCTabSwitchKeyIcon.TabSwitch> {
        public enum TabSwitch : byte {
            Next = 0,
            Previous = 1,
        }
    }
}