using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UI {
    public class TextSettingsProvider : MonoBehaviour, IService {
        [field: Required, SerializeField] public TextSettings UTKTextSettings { get; private set; }
        FontChooseSetting _fontChooseSetting;
        
        public void Init() {
            _fontChooseSetting = World.Only<FontChooseSetting>();
            _fontChooseSetting.ListenTo(Setting.Events.SettingRefresh, RefreshFont, this);
            RefreshFont();
        }
        
        void RefreshFont() {
            var documents = World.Services.Get<UIDocumentProvider>().AllDocuments;
            var fontAsset = _fontChooseSetting.ActiveFont.FontAsset;
            
            foreach (var document in documents) {
                var root = document.rootVisualElement;
                root.style.unityFontDefinition = new StyleFontDefinition(fontAsset);
            }
            
            UTKTextSettings.defaultFontAsset = fontAsset;
            TMP_Settings.defaultFontAsset = fontAsset;
        }
    }
}