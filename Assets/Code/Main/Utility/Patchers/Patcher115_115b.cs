using System;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.Utility;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher115_115b : Patcher {
        protected override Version MaxInputVersion => new(1, 15, 12);
        protected override Version FinalVersion => new(1, 15, 13);

        public override void StartGamePatch() {
            if (PlatformUtils.IsConsole) {
                ClearSettings();
            }
        }

        static void ClearSettings() {
            ClearSetting(DistanceCullingSetting.PrefKey);
            ClearSetting(Shadows.ContactShadowsEnabledPrefId);
            ClearSetting(FogQuality.PrefId);
            ClearSetting(ScreenResolution.PrefIdVSync);
        }

        static void ClearSetting(string settingId) {
            if (PrefMemory.HasKey(settingId)) {
                PrefMemory.DeleteKey(settingId);
            }
        }
    }
}