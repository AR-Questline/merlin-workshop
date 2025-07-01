using System;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_GAMECORE
using Awaken.TG.Main.SocialServices.MicrosoftServices;
#endif

namespace Awaken.TG.Main.UI.TitleScreen {
    [UsesPrefab("TitleScreen/VTitleScreenOverlayUI")]
    public class VTitleScreenOverlayUI : View<TitleScreenUI> {
        public JoinDiscordButton joinDiscordButton;
        public TextMeshProUGUI version;
        public TMP_Text gamerTag;
        public GameObject gitInfoPanel;
        public TMP_Text gitInfo;
        public Button copyHashButton;
        bool _theMessageRewardGranted;
        IEventListener _cheatListener;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            joinDiscordButton.Initialize();

            UpdateGameVersion();

            if (gamerTag != null) {
                gamerTag.gameObject.SetActive(PlatformUtils.IsXbox);
#if UNITY_GAMECORE && !UNITY_EDITOR
                gamerTag.text = LocTerms.Profile.Translate(MicrosoftManager.Instance.GamerName);
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

        [Serializable]
        public class JoinDiscordButton {
            public string url;
            [LocStringCategory(Category.UI)]
            public LocString joinDiscord;
            [SerializeField] ButtonConfig buttonConfig;
            
            public void Initialize() {
                if (PlatformUtils.IsConsole) {
                    buttonConfig.TrySetActiveOptimized(false);
                }
                
                buttonConfig.InitializeButton(() => Application.OpenURL(url), joinDiscord.Translate());
            }
        }
    }
}
