using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.UI.TitleScreen.FileVerification;
using Awaken.TG.Main.UI.UITooltips;
using Awaken.TG.Main.Utility;
using Awaken.TG.Utility;

namespace Awaken.TG.MVC {
    public static class DomainErrorPopup {
        public static bool Displayed { get; private set; }
        
        public static void Display() {
            if (Displayed) {
                return;
            }
            Displayed = true;
            
            var prompt = Prompt.Tap(
                KeyBindings.UI.Generic.Accept, 
                LocTerms.PopupDomainErrorButton.Translate(), 
#if UNITY_PS5
                Awaken.TG.Main.SocialServices.PlayStationServices.PlayStationUtils.RestartGame
#else
                TitleScreenUI.Exit
#endif
            ).AddAudio().SetupState(true, true);

            var popup = PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI),
                LocTerms.PopupDomainErrorTitle.Translate(),
                LocTerms.PopupDomainErrorInfo.Translate(FileIntegrityPanel.FailedMessage()),
                prompt
            );
            
            TextLinkHandler.OpenLinksOf(popup);
        }
    }
}