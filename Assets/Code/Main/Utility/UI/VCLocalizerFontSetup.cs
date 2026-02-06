using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.MVC;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.Localization.PropertyVariants.TrackedObjects;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.Localization.Settings;

namespace Awaken.TG.Main.Utility.UI {
    /// <summary>
    /// Sets up the font variant for a GameObjectLocalizer component based on the active font chosen in FontChooseSetting.
    /// </summary>
    [RequireComponent(typeof(GameObjectLocalizer))]
    public class VCLocalizerFontSetup : ViewComponent {
        const string FontKey = "m_fontAsset";
        const string FontMaterialKey = "m_sharedMaterial";
        const string EnglishLocaleKey = "en";
        
        TextMeshProUGUI _text;
        GameObjectLocalizer _localizer;
        UnityObjectProperty _fontProperty;
        UnityObjectProperty _fontMaterialProperty;

        protected override void OnAttach() {
            CacheValues();
            ModelUtils.DoForFirstModelOfType<FontChooseSetting>(setting => {
                setting.ListenTo(Setting.Events.SettingChanged, SetupLocalizer, this);
                SetupLocalizer(setting);
            }, this);
        }
        
        void CacheValues() {
            _text = GetComponent<TextMeshProUGUI>();
            _localizer = GetComponent<GameObjectLocalizer>();
            
            var trackedText = _localizer.GetTrackedObject<TrackedUGuiGraphic>(_text);
            _fontProperty = trackedText.GetTrackedProperty<UnityObjectProperty>(FontKey);
            _fontMaterialProperty = trackedText.GetTrackedProperty<UnityObjectProperty>(FontMaterialKey);
        }
        
        void SetupLocalizer(Setting setting) {
            if (setting is not FontChooseSetting fontChooseSetting) {
                return;
            }
            
            var fontAsset = fontChooseSetting.ActiveFont.FontAsset;
            _fontProperty.SetValue(EnglishLocaleKey, fontAsset);
            _fontMaterialProperty.SetValue(EnglishLocaleKey, fontAsset.material);
            
            var selectedLocale = LocalizationSettings.SelectedLocale;
            if (selectedLocale.Identifier == EnglishLocaleKey) {
                _localizer.ApplyLocaleVariant(selectedLocale);
            }
        }
    }
}
