using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Controls {
    public partial class FOVSetting : Setting {
        const float AspectRatio = 16 / 9f;
        const string DisplayFormat = "{0}";
        static string SettingTooltip => LocTerms.SettingsFOVTooltip.Translate();

        readonly SliderOption _slider;
        
        public sealed override string SettingName => LocTerms.SettingsFOV.Translate();
        public override IEnumerable<PrefOption> Options => _slider.Yield();

        public float FOV {
            get {
                float horizontalFoV = _slider.Value;
                float verticalFoV = 2f * Mathf.Atan(Mathf.Tan(horizontalFoV * Mathf.Deg2Rad / 2f) / AspectRatio) * Mathf.Rad2Deg;
                return verticalFoV;
            }
        }

        public FOVSetting() {
            _slider = new SliderOption("Settings_HorizontalFOV", SettingName, 90f, 120f, true, DisplayFormat, 100f, false, 1f);
            _slider.AddTooltip(() => SettingTooltip);
        }
    }
}