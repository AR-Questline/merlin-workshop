using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class HUDScale : CanvasScaleSetting {
        const string PrefId = "Accessibility_HUDScaling";

        DependentOption _dependentOption;
        public sealed override string SettingName => LocTerms.SettingsHUDScale.Translate();
        public override IEnumerable<PrefOption> Options => _dependentOption.Yield();
        
        public float HeroBarsScale => (_heroBarsOption.Value / 100f) / (MainSliderOption.Value / 100f);
        public float CompassScale => (_compassOption.Value / 100f) / (MainSliderOption.Value / 100f);
        
        readonly SliderOption _heroBarsOption;
        readonly SliderOption _compassOption;

        public HUDScale() {
            MainSliderOption = CreateSliderOption(PrefId, SettingName);
            _heroBarsOption = CreateSliderOption($"{PrefId}_HeroBars", LocTerms.SettingsHUDHeroBarsScale.Translate());
            _compassOption = CreateSliderOption($"{PrefId}_Compass", LocTerms.SettingsHUDCompassScale.Translate());

            _dependentOption = new DependentOption(MainSliderOption, _heroBarsOption, _compassOption);
            MainSliderOption.AddTooltip(LocTerms.HudScaleSettingTooltip.Translate);
            _heroBarsOption.AddTooltip(LocTerms.HeroBarsScaleSettingTooltip.Translate);
            _compassOption.AddTooltip(LocTerms.CompassScaleSettingTooltip.Translate);

            //add rest tooltips
            float mainSliderValue = MainSliderOption.Value;
            MainSliderOption.onChange += value => {
                float change = value - mainSliderValue;
                _compassOption.Value += change;
                _heroBarsOption.Value += change;
                mainSliderValue = value;
            };
        }

        protected override void UpdateSettings() {
            CanvasService canvasService = World.Services.Get<CanvasService>();
            
            Canvas hudCanvas = canvasService.HUDCanvas;
            ResizeCanvasScaler(hudCanvas);
            Canvas compassCanvas = canvasService.MapCompassCanvas;
            ResizeCanvasScaler(compassCanvas);
            
            UIDocumentProvider uiDocumentProvider = World.Services.Get<UIDocumentProvider>();
            UIDocument document = uiDocumentProvider.TryGetDocument(UIDocumentType.HUD);
            if (document != null) {
                document.panelSettings.referenceResolution = new Vector2Int((int) CurrentScale.x, (int) CurrentScale.y);
            }
        }
    }
}
