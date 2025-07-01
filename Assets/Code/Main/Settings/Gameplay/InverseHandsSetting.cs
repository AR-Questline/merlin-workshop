using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class ReversedHandsSetting : Setting {
        public sealed override string SettingName => LocTerms.SettingReverseHands.Translate();
        static string SettingTooltip => LocTerms.SettingReverseHandsTooltip.Translate();
        public override bool IsVisible => !RewiredHelper.IsGamepad;
        public override IEnumerable<PrefOption> Options => _toggle.Yield();
        public MouseButton RightHandMouseButton => CanInverseHands ? MouseButton.RightMouseButton : MouseButton.LeftMouseButton;
        public MouseButton LeftHandMouseButton => CanInverseHands ? MouseButton.LeftMouseButton : MouseButton.RightMouseButton;

        bool ShouldInverseHands => Hero.Current.OffHandItem.IsMagic || (Hero.Current.MainHandItem.IsMagic && Hero.Current.OffHandItem is {IsShield: false, IsFists: false, IsRod: false });
        bool CanInverseHands => Enabled && ShouldInverseHands;
        bool Enabled => _toggle.Enabled;
        
        ToggleOption _toggle;

        public ReversedHandsSetting() {
            _toggle = new ToggleOption("Setting_ReverseHands", SettingName, false, true);
            _toggle.AddTooltip(() => SettingTooltip);
        }
    }
}