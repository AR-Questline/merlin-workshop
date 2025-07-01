using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings.FirstTime;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class PerspectiveSetting : Setting {
        const string PlayerSeenTppExcuseKey = "PlayerSeenTppExcuse";
        readonly ToggleOption _toggle;

        EnumArrowsOption EnumOption { get; }
        public sealed override string SettingName => LocTerms.SettingsPerspective.Translate();
        public override IEnumerable<PrefOption> Options => EnumOption.Yield();
        
        protected override bool AutoApplyOnInit => false;

        readonly ToggleOption[] _toggleOptions = {
            new ("fpp", LocTerms.SettingsPerspectiveFPP.Translate(), true, true),
            new ("tpp", LocTerms.SettingsPerspectiveTPP.Translate(), false, true)
        };
        
        public bool IsTPP {
            get => EnumOption.Option == _toggleOptions[1];
            set {
                if (IsTPP != value) {
                    EnumOption.Option = value ? _toggleOptions[1] : _toggleOptions[0];
                    if (value) {
                        TryShowTppExcusePopup();
                    }
                    EnumOption.Apply();
                }
            } 
        }

        public PerspectiveSetting() {
            EnumOption = new EnumArrowsOption("Perspective_Setting", SettingName, _toggleOptions[0], true, _toggleOptions);
            EnumOption.SetInteractabilityFunction(static () => {
                var vHeroController = Hero.Current?.VHeroController;
                if (vHeroController == null) {
                    return true;
                }
                return !vHeroController.PerspectiveChangeInProgress;
            });
        }

        protected override void OnApply() { 
            if (Hero.TppActive != IsTPP) {
                Hero.Current?.VHeroController.ChangeHeroPerspective(IsTPP).Forget();
                TryShowTppExcusePopup();
            }
        }
        
        void TryShowTppExcusePopup() {
            if(!IsTPP || PrefMemory.GetBool(PlayerSeenTppExcuseKey) || World.Any<FirstTimeSettings>()) {
                return;
            }
            
            var title = LocTerms.SettingsThirdPersonPopupTitle.Translate();
            var message = LocTerms.SettingsThirdPersonPopupMessage.Translate();
            PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI),message, title);
            PrefMemory.Set(PlayerSeenTppExcuseKey, true, true);
            PrefMemory.Save();
        }
    }
}