using Awaken.TG.Main.Settings.Options;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Debug {
    public partial class FlickerFixSetting : Setting {
        const string PrefId = "FlickerFix";

        public ToggleOption Option { get; }
        
        public sealed override string SettingName => "Flicker Fix";
        public override bool IsVisible => !PlatformUtils.IsConsole;
        public bool Enabled => Option.Enabled;

        public FlickerFixSetting() {
            bool defaultValue =
                (SystemInfo.graphicsDeviceVendorID is 0x10DE && SystemInfo.graphicsDeviceID <= 0x1B02) || //nVidia TITAN X or older
                (SystemInfo.graphicsDeviceVendorID is 0x1002 && SystemInfo.graphicsDeviceID <= 0x67DF); //AMD RX 580 or older 
            
            Option = new ToggleOption(PrefId, SettingName, defaultValue, false);
        }
    }
}