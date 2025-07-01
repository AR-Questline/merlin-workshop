using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI {
    /// <summary>
    /// Adjust font size of TMP_Text component based on platform.
    /// Assume font size set in the editor is for PC platform, this is the default setting.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class VCDeviceFontAdjuster : ViewComponent {
        [SerializeField] int consoleFontSizeAdjustment = 8;
        
        TMP_Text _text;
        float _pcFontSize;
        static bool s_debugConsolePlatform;
        
        protected override void OnAttach() {
            CacheValues();
            
            ModelUtils.DoForFirstModelOfType<ConsoleUISetting>(setting => {
                setting.ListenTo(Setting.Events.SettingChanged, SetFontSize, this);
                SetFontSize(setting);
            }, this);
        }

        void CacheValues() {
            _text = GetComponent<TMP_Text>();
            _pcFontSize = _text.fontSize;
        }

        void SetFontSize(Setting setting) {
            if (setting is ConsoleUISetting consoleUISetting) {
                _text.fontSize = consoleUISetting.Enabled ? _pcFontSize + consoleFontSizeAdjustment : _pcFontSize;
            }
        }
        
#if UNITY_EDITOR
        [Button]
        public static void DebugSetAll(bool isConsolePlatform) {
            s_debugConsolePlatform = isConsolePlatform;
            var result = FindObjectsByType<VCDeviceFontAdjuster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var fontAdjuster in result) {
                if (fontAdjuster._text == null) {
                    fontAdjuster.CacheValues();
                }
                
                fontAdjuster.SetFontSize(World.Any<ConsoleUISetting>());
            }
        }
#endif
    }
}
