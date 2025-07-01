using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.MVC;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit {
    public class UTKPanelSettingsService : MonoBehaviour, IService {
        [field: SerializeField] public PanelSettings MainPanelSettings { get; private set; }
        
        [field: SerializeField] public ThemeStyleSheet PCSmallTheme { get; private set; }
        [field: SerializeField] public ThemeStyleSheet PCTheme { get; private set; }
        [field: SerializeField] public ThemeStyleSheet PCBigTheme { get; private set; }
        [field: SerializeField] public ThemeStyleSheet PCHugeTheme { get; private set; }
        [field: SerializeField] public ThemeStyleSheet ConsoleSmallTheme { get; private set; }
        [field: SerializeField] public ThemeStyleSheet ConsoleTheme { get; private set; }
        [field: SerializeField] public ThemeStyleSheet ConsoleBigTheme { get; private set; }
        
        public void Init() {
            InitTheme().Forget();
        }

        async UniTaskVoid InitTheme() {
            await UniTask.WaitUntil(() => World.Any<FontSizeSetting>());
            World.Only<FontSizeSetting>().ListenTo(Setting.Events.SettingRefresh, RefreshTheme, this);
            World.Only<ConsoleUISetting>().ListenTo(Setting.Events.SettingRefresh, RefreshTheme, this);
            RefreshTheme();
        }

        void RefreshTheme() {
            MainPanelSettings.themeStyleSheet = World.Only<ConsoleUISetting>().Enabled ? GetConsoleTheme() : GetPCTheme();
        }

        ThemeStyleSheet GetConsoleTheme() {
            var setting = World.Only<FontSizeSetting>().ActiveFontSize.FontSizeChange;
            return setting switch {
                FontSize.SmallChangeValue => ConsoleSmallTheme,
                FontSize.MediumChangeValue => ConsoleTheme,
                FontSize.BigChangeValue => ConsoleBigTheme,
                FontSize.HugeChangeValue => ConsoleBigTheme,
                _ => ConsoleTheme
            };
        }
        
        ThemeStyleSheet GetPCTheme() {
            var setting = World.Only<FontSizeSetting>().ActiveFontSize.FontSizeChange;
            return setting switch {
                FontSize.SmallChangeValue => PCSmallTheme,
                FontSize.MediumChangeValue => PCTheme,
                FontSize.BigChangeValue => PCBigTheme,
                FontSize.HugeChangeValue => PCHugeTheme,
                _ => PCTheme
            };
        }
        
#if UNITY_EDITOR
        [Button]
        public static void DebugConsole(bool isConsolePlatform) {
            World.Services.Get<UTKPanelSettingsService>().RefreshTheme();
        }
#endif
    }
}