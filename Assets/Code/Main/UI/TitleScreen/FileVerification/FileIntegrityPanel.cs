using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using UnityEngine;


#if !UNITY_GAMECORE && !UNITY_PS5
using Awaken.TG.Main.Analytics;
using GameAnalyticsSDK;
#endif

namespace Awaken.TG.Main.UI.TitleScreen.FileVerification {
#if !UNITY_GAMECORE && !UNITY_PS5
    [SpawnsView(typeof(VFileIntegrityPanel))]
#endif
    public partial class FileIntegrityPanel : Element<TitleScreenUI> {
        public sealed override bool IsNotSaved => true;

#if !UNITY_GAMECORE && !UNITY_PS5
        readonly ApplicationFileIntegrityChecker _checker;
        bool _verificationHandled;
        
        public new static class Events {
            public static readonly Event<FileIntegrityPanel, float> ProgressChanged = new(nameof(ProgressChanged));
        }

        public FileIntegrityPanel(ApplicationFileIntegrityChecker checker) {
            _checker = checker;
        }
        
        protected override void OnFullyInitialized() {
            RefreshProgress();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _checker.ForceEndVerification();
        }

        public void Update() {
            RefreshProgress();
            
            if (_checker.Verified && !_verificationHandled) {
                _verificationHandled = true;
                OnVerified();
            }
        }

        void RefreshProgress() {
            if (_checker.WasProgressChanged(out var progress)) {
                this.Trigger(Events.ProgressChanged, progress);
            }
        }

        void OnVerified() {
            if (_checker.Success) {
                Discard();
                _checker.MarkVerified();
                Log.Marking?.Warning("[Verifier] SUCCESS");
            } else {
                GameAnalyticsController.OnActiveSession(() => {
                    //GameAnalytics.NewErrorEvent(GAErrorSeverity.Info, "File integrity check failed");
                }, ParentModel);

                Log.Marking?.Warning("[Verifier] FAIL with: " + _checker.ErrorsInfo);
                _checker.LogFailedFiles();
                
                if (PlatformUtils.IsSteamInitialized) {
                    bool revalidateAllFiles = !_checker.ErrorsInfo.HasFlagFast(FileChecksumErrors.HashMismatch);
                    //App.Client.MarkContentCorrupt(revalidateAllFiles);
                }
                PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI), 
                    FailedMessage(),
                    LocTerms.UIFileIntegrityFailedTitle.Translate(),
                    RedirectAndClose
                );
            }
        }

        void RedirectAndClose() {
            if (PlatformUtils.IsSteamInitialized) {
                TitleScreenUI.Exit();
            } else if (PlatformUtils.IsGogInitialized) {
                Application.OpenURL(LocTerms.UIFileIntegrityFailedLinkGog.Translate());
                Discard();
            }
        }

#endif
        public static string FailedMessage() {
            if (PlatformUtils.IsSteamInitialized) {
                return LocTerms.UIFileIntegrityFailedMessageSteam.Translate();
            } else if (PlatformUtils.IsGogInitialized) {
                return LocTerms.UIFileIntegrityFailedMessageGog.Translate();
            } else {
                return LocTerms.UIFileIntegrityFailedMessageGeneric.Translate();
            }
        }
    }
}
