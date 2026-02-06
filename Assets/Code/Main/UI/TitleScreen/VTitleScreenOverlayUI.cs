using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_GAMECORE || MICROSOFT_GAME_CORE
using Awaken.TG.Utility;
#endif

namespace Awaken.TG.Main.UI.TitleScreen {
    [UsesPrefab("TitleScreen/VTitleScreenOverlayUI")]
    public class VTitleScreenOverlayUI : View<TitleScreenUI> {
        public TextMeshProUGUI version;
        public TMP_Text gamerTag;
        public GameObject gitInfoPanel;
        public TMP_Text gitInfo;
        public Button copyHashButton;
        bool _theMessageRewardGranted;
        IEventListener _cheatListener;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            UpdateGameVersion();

            if (gamerTag != null) {
                gamerTag.gameObject.SetActive(PlatformUtils.IsMicrosoft);
#if (UNITY_GAMECORE || MICROSOFT_GAME_CORE) && !UNITY_EDITOR
                var profileLocTerm = Awaken.TG.Main.Localization.LocTerms.Profile;
                var microsoftManager = Awaken.TG.Main.SocialServices.MicrosoftServices.MicrosoftManager.Instance;
                gamerTag.text = profileLocTerm.Translate(microsoftManager.GamerName);
#endif
            }
            
            InitGitInfo();
        }

        void UpdateGameVersion() {
            version.text = $"v{GameVersion()}";
        }

        void InitGitInfo() {
            gitInfoPanel.SetActive(false);

            if (Application.isEditor) {
                return;
            }

            if (CheatController.CheatsEnabled()) {
                OnCheatsChanged();
            } else {
                _cheatListener = ModelUtils.ListenToFirstModelOfType<CheatController, Model>(Model.Events.AfterChanged, OnCheatsChanged, this);
            }
        }
        
        void OnCheatsChanged() {
            if (!CheatController.CheatsEnabled()) {
                return;
            }
            if (_cheatListener != null) {
                World.EventSystem.RemoveListener(_cheatListener);
                _cheatListener = null;
            }

            gitInfoPanel.SetActive(true);
            gitInfo.text = $"{GitDebugData.BuildBranchName} {GitDebugData.BuildCommitHash}";
            copyHashButton.onClick.AddListener(GitDebugData.CopyBuildCommitHash);

            UpdateGameVersion();
        }

        static string GameVersion() {
            var gameConstants = World.Services.Get<GameConstants>();
            string gameVersion = gameConstants.gameVersion;
            if (string.IsNullOrWhiteSpace(gameVersion)) {
                gameVersion = Application.version;
            }

            if (!Application.isEditor && CheatController.CheatsEnabled()) {
                gameVersion += $"#{GitDebugData.BuildCommitHash}";
            }

            return gameVersion;
        }
    }
}
