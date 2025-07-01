using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Awaken.TG.Main.UI.Helpers {
    [DisallowMultipleComponent]
    public class VCAccessibility : ViewComponent<IModel> {
        StructList<TextMeshProUGUI> _tmps;
        StructList<TextSettings> _textSettings;

        public float fontSizeMultiplier = 1f;
        public bool disableScaling;

        protected override void OnAttach() {
            _tmps = StructList<TextMeshProUGUI>.Empty;
            _textSettings = StructList<TextSettings>.Empty;

            InitWithDelay().Forget();
        }

        public void AddTextMeshPro(TextMeshProUGUI textMeshProUGUI) {
            FontSizeMultiplier multiplier = textMeshProUGUI.GetComponent<FontSizeMultiplier>();
            float fontMultiplier = multiplier != null ? multiplier.fontSizeMultiplier : fontSizeMultiplier;
            var tmpIndex = _tmps.IndexOf(textMeshProUGUI);
            if (tmpIndex != -1) {
                _textSettings[tmpIndex] = new TextSettings(textMeshProUGUI.fontSize, textMeshProUGUI.font, fontMultiplier);
            } else {
                tmpIndex = _tmps.Count;
                _tmps.Add(textMeshProUGUI);
                _textSettings.Add(new TextSettings(textMeshProUGUI.fontSize, textMeshProUGUI.font, fontMultiplier));
            }

            float fontSizeChange = World.Any<FontSizeSetting>()?.ActiveFontSize.FontSizeChange ?? 0f;
            Refresh(fontSizeChange, tmpIndex);
        }

        async UniTaskVoid InitWithDelay() {
            await UniTask.DelayFrame(1);
            if (this == null || disableScaling) {
                return;
            }

            foreach (var textMesh in GetComponentsInChildren<TextMeshProUGUI>(true)) {
                if (textMesh.GetComponentInParent<VCAccessibility>() == this) {
                    float fontSize = textMesh.fontSize;
                    // --- We delay one frame, so that correct font size is already set, then we take the size that was set by autosize and scale it.
                    // --- We need to disable auto-sizing because it will undo our changes.
                    textMesh.enableAutoSizing = false;
                    FontSizeMultiplier multiplier = textMesh.GetComponent<FontSizeMultiplier>();
                    float fontMultiplier = multiplier != null ? multiplier.fontSizeMultiplier : fontSizeMultiplier;

                    _tmps.Add(textMesh);
                    _textSettings.Add(new TextSettings(fontSize, textMesh.font, fontMultiplier));
                }
            }

            Refresh();
            World.Any<ConsoleUISetting>()?.ListenTo(Setting.Events.SettingRefresh, RefreshFontSize, this);
            World.Any<FontSizeSetting>()?.ListenTo(Setting.Events.SettingRefresh, Refresh, this);
        }
        
        void RefreshFontSize() {
            if (this == null) {
                return;
            }
            
            for (int i = 0; i < _tmps.Count; i++) {
                _textSettings[i] = new TextSettings(_tmps[i].fontSize, _tmps[i].font, _textSettings[i].Multiplier);
            }
        }

        void Refresh() {
            if (this == null) {
                return;
            }
            
            float fontSizeChange = World.Any<FontSizeSetting>()?.ActiveFontSize.FontSizeChange ?? 0f;

            for (int i = 0; i < _tmps.Count; i++) {
                Refresh(fontSizeChange, i);
            }
        }

        void Refresh(float fontSizeChange, int index) {
            TextMeshProUGUI textMesh = _tmps[index];
            TextSettings textSettings = _textSettings[index];
            textMesh.fontSize = textSettings.FontSize + (fontSizeChange * textSettings.Multiplier);
        }
        
        readonly struct TextSettings {
            public float FontSize { get; }
            public FontAsset FontAsset { [UnityEngine.Scripting.Preserve] get; }
            public float Multiplier { get; }

            public TextSettings(float fontSize, FontAsset fontAsset, float multiplier) {
                FontSize = fontSize;
                FontAsset = fontAsset;
                Multiplier = multiplier;
            }
        }
    }
}