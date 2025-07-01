using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class PrivacyPolicy : Setting {

        public sealed override string SettingName => LocTerms.SettingsPrivacyPolicy.Translate();

        public override bool IsVisible => !PlatformUtils.IsConsole;
        public override IEnumerable<PrefOption> Options => _option.Yield();
        LinkOption _option;

        public PrivacyPolicy() {
            _option = new LinkOption(SettingName, OnClick);
        }

        void OnClick() {
            Application.OpenURL("https://presskit.taintedgrail.com/eula");
        }
    }
}