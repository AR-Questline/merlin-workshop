using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class ScreenDisplay : Setting {
        const string PrefId = "ScreenDisplayNo";

        // === Options
        List<DisplayInfo> _displays = new();
        EnumArrowsOption _option;
        
        public sealed override string SettingName => LocTerms.SettingsDisplayNumber.Translate();
        public override bool IsVisible => DisplayOption != null;
        protected override bool AutoApplyOnInit => false;

        public override IEnumerable<PrefOption> Options {
            get {
                var displayOption = DisplayOption;
                if (PlatformUtils.IsConsole || displayOption == null) {
                    return Enumerable.Empty<PrefOption>();
                }
                return displayOption.Yield();
            }
        }

        EnumArrowsOption DisplayOption {
            get {
                if (PlatformUtils.IsConsole) {
                    return null;
                }

                Screen.GetDisplayLayout(_displays);
                if (_displays.Count > 1) {
                    if (_option == null || _option.Options.Count() != _displays.Count) {
                        ConfigureOption();
                    }

                    return _option;
                } else {
                    return null;
                } 
            }
        }

        void ConfigureOption() {
            List<ToggleOption> options = new();
            for (int i = 0; i < _displays.Count; i++) {
                var display = _displays[i];
                bool enabled = Screen.mainWindowDisplayInfo.Equals(display);
                options.Add(new ToggleOption($"{PrefId}_{i}", (i+1).ToString(), enabled, false));
            }

            if (!options.Any(o => o.Enabled)) {
                options.First().Enabled = true;
            }

            _option = new EnumArrowsOption(PrefId, SettingName, options.First(o => o.Enabled), false, options.ToArray());
        }

        protected override void OnApply() {
            if (PlatformUtils.IsConsole) {
                return;
            }

            if (DisplayOption != null) {
                int index = DisplayOption.OptionInt;
                if (index < 0 || index >= _displays.Count) {
                    index = 0;
                    DisplayOption.Option = DisplayOption.Options.First();
                }

                // Taken from https://github.com/Unity-Technologies/DesktopSamples/blob/master/MoveWindowSample/Assets/Scripts/SettingsMenuScript.cs
                DisplayInfo display = _displays[index];
                Vector2Int targetCoordinates = new(0, 0);

                if (Screen.fullScreenMode != FullScreenMode.Windowed) {
                    // Target the center of the display. Doing it this way shows off
                    // that MoveMainWindow snaps the window to the top left corner
                    // of the display when running in fullscreen mode.
                    targetCoordinates.x += display.width / 2;
                    targetCoordinates.y += display.height / 2;
                }

                var asyncOp = Screen.MoveMainWindowTo(display, targetCoordinates);

                World.Only<ScreenResolution>().ResetScreenResolution(asyncOp).Forget();
            }
        }
    }
}