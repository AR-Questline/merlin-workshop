using System.Collections.Generic;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class ShowCreditsSetting : Setting {
        ButtonOption _button;
        
        public sealed override string SettingName => LocTerms.SettingsCredits.Translate();
        public override IEnumerable<PrefOption> Options => _button.Yield();
        
        public ShowCreditsSetting() {
            _button = new ButtonOption("Setting_CreditsButton", SettingName, () => ShowCredits().Forget());
        }

        static async UniTaskVoid ShowCredits() {
            await Credits.Show(typeof(VCredits));

            if (SocialService.Get.HasDlc(DlcCategory.Sarras)) {
                await Credits.Show(typeof(VCreditsSarras));
            }
        }
    }
}