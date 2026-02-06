using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Settings.FontChooseStartup {
    public partial class FontChooseStartup : Model {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;
        
        FontChooseSetting FontChooseSetting => World.Only<FontChooseSetting>();
        
        protected override void OnInitialize() {
            World.SpawnView<VFontChooseStartup>(this, true);
            World.Any<MenuUI>()?.Hide();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Any<MenuUI>()?.UnHide();
        }

        public void SubmitFont(FontFamily font) {
            FontChooseSetting.SetFontOption(font);
            FontChooseSetting.Apply(out _);
            PrefMemory.Save();
#if !UNITY_GAMECORE && !UNITY_PS5 && !MICROSOFT_GAME_CORE
            Awaken.TG.Main.Analytics.GeneralAnalytics.TrySetFirstTimeFontType();
#endif
        }

        public static async UniTask ShowFontChoose() {
            var fontChoose = World.Add(new FontChooseStartup());
            await AsyncUtil.WaitForDiscard(fontChoose);
        }
    }
}