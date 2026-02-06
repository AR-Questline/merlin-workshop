using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.Main.UI.Bugs;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Menu.SaveLoadUI;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using FMODUnity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Menu {
    [UsesPrefab("UI/VMenuUI")]
    public class VMenuUI : View<MenuUI>, IFocusSource, IAutoFocusBase, IPromptHost {
        [SerializeField] ButtonConfig resumeGame;
        [SerializeField] ButtonConfig startNewGamePlus;
        [SerializeField] ButtonConfig saveGame;
        [SerializeField] ButtonConfig loadGame;
        [SerializeField] ButtonConfig unstuck;
        [SerializeField] ButtonConfig options;
        [SerializeField] ButtonConfig photoMode;
        [SerializeField] ButtonConfig bugReport;
        [SerializeField] ButtonConfig exitToMenu;
        [SerializeField] ButtonConfig exitGame;
        [SerializeField] Transform promptsHost;
        [Title("xbox")] [SerializeField] TMP_Text xboxProfile;
        [Title("Audio")]
        [SerializeField] public EventReference openMenuSound;
        [SerializeField] public EventReference closeMenuSound;
        [SerializeField] ARFmodEventEmitter snapshotEmitter;

        [Title("Debug")]
        [SerializeField] TMP_Text debugGitInfo;
        [SerializeField] Button copyHashButton;

        public bool ForceFocus => true;
        public Component DefaultFocus => resumeGame.button;
        public Transform PromptsHost => promptsHost;
        
        // === Initialization
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            InitializeButtons();
            PlayAudio();
            
            InitDebugInfo();
            if (xboxProfile != null) {
                xboxProfile.TrySetActiveOptimized(PlatformUtils.IsMicrosoft);
#if UNITY_GAMECORE || MICROSOFT_GAME_CORE
                xboxProfile.text = LocTerms.Profile.Translate(SocialServices.MicrosoftServices.MicrosoftManager.Instance.GamerName);
#endif
            }
            
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<Credits>(), this, OnCredits);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<SaveMenuUI>(), this, CorrectFocusOnReturn);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<LoadMenuUI>(), this, CorrectFocusOnReturn);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<AllSettingsUI>(), this, CorrectFocusOnReturn);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<UserBugReporting>(), this, CorrectFocusOnReturn);
        }

        protected override void OnMount() {
            InitializePrompts();
        }

        void InitializePrompts() {
            var prompts = new Prompts(this);
            Target.AddElement(prompts);
            
            var cancelPrompt = Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), Target.Close, Prompt.Position.Last);
            var selectPrompt = Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.Select.Translate(), Prompt.Position.First, ControlSchemeFlag.Gamepad);
            prompts.AddPrompt(cancelPrompt, Target);
            prompts.AddPrompt(selectPrompt, Target);
        }

        void InitializeButtons() {
            resumeGame.InitializeButton(Target.Close);
            if (NewGamePlusUtils.IsAvailable) {
                startNewGamePlus.InitializeButton(NewGamePlusUtils.StartNewGamePlusDuringGameplay);
            } else {
                startNewGamePlus.gameObject.SetActive(false);
            }
            saveGame.InitializeButton(() => MenuUI.OpenSaveUI(this));
            loadGame.InitializeButton(() => MenuUI.OpenLoadUI(this));
            unstuck.InitializeButton(Target.Unstuck);
            options.InitializeButton(() => MenuUI.OpenSettingUI(this));
            
            if (PlatformUtils.IsConsole) {
                bugReport.gameObject.SetActive(false);
            } else {
                bugReport.InitializeButton(() => MenuUI.OpenBugReportUI(this));
            }
            
            photoMode.InitializeButton(Target.ShowPhotoMode);
            photoMode.button.Interactable = Hero.Current.IsPhotoModeEnabled;

            exitToMenu.InitializeButton(Target.QuitToMenu);
            exitGame.InitializeButton(Target.QuitGame);

            saveGame.button.Interactable = LoadSave.Get.CanPlayerSave();
            loadGame.button.Interactable = LoadSave.Get.LoadAllowedInMenu();
            exitGame.TrySetActiveOptimized(!PlatformUtils.IsConsole);
        }

        void Update() {
            if (startNewGamePlus.gameObject.activeInHierarchy) {
                startNewGamePlus.button.Interactable = NewGamePlusUtils.CanBeTriggeredRightNow;
            }
            saveGame.button.Interactable = LoadSave.Get.CanPlayerSave();
            loadGame.button.Interactable = LoadSave.Get.LoadAllowedInMenu();
        }

        void OnCredits(Model creditsModel) {
            StopAudio();
            creditsModel.ListenTo(Model.Events.BeforeDiscarded, PlayAudio, this);
        }

        void PlayAudio() {
            //snapshotEmitter.Play();
            if (!openMenuSound.IsNull) {
                FMODManager.PlayOneShot(openMenuSound);
            }
        }
        
        void StopAudio() {
            //snapshotEmitter.Stop();
            if (!closeMenuSound.IsNull) {
                FMODManager.PlayOneShot(closeMenuSound);
            }
        }
        
        void InitDebugInfo() {
            copyHashButton?.gameObject.SetActive(false);
            if (Application.isEditor) return;
            if (debugGitInfo != null && CheatController.CheatsEnabled()) {
                debugGitInfo.text = $"Git: {GitDebugData.BuildBranchName} ({GitDebugData.BuildCommitHash})";
                copyHashButton?.gameObject.SetActive(true);
                copyHashButton?.onClick.AddListener(GitDebugData.CopyBuildCommitHash);
            }
        }

        void CorrectFocusOnReturn(Model model) {
            switch (model) {
                case SaveMenuUI:
                    World.Only<Focus>().Select(saveGame.button);
                    return;
                case LoadMenuUI:
                    World.Only<Focus>().Select(loadGame.button);
                    return;
                case AllSettingsUI:
                    World.Only<Focus>().Select(options.button);
                    return;
                case UserBugReporting:
                    World.Only<Focus>().Select(bugReport.button);
                    break;
            }
        }
        
        protected override IBackgroundTask OnDiscard() {
            StopAudio();
            return base.OnDiscard();
        }
    }
}