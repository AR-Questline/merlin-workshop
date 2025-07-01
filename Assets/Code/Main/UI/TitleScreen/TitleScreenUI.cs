using System;
using System.Linq;
using Awaken.TG.Assets.ShadersPreloading;
using Awaken.TG.Debugging.Logging; // for standard error log handler for exiting game
using Awaken.TG.Graphics;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Settings.GammaSettingScreen;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.TitleScreen.FileVerification;
using Awaken.TG.Main.UI.TitleScreen.PatchNotes;
using Awaken.TG.Main.UI.UITooltips;
using Awaken.TG.Main.UI.TitleScreen.ShadersPreloading;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Automation;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if !UNITY_GAMECORE && !UNITY_PS5
using System.IO;
using Awaken.TG.Main.Analytics;
using Awaken.TG.Main.Saving.Utils;
#endif

namespace Awaken.TG.Main.UI.TitleScreen {
    [SpawnsView(typeof(VTitleScreenMusic))]
    public partial class TitleScreenUI : Model, IUIStateSource, IPromptHost {
        public const string SkipPrologueUnlockKey = "skipPrologueUnlocked";
        
        public static bool SkipPrologueUnlocked => PrefMemory.GetBool(SkipPrologueUnlockKey);
        public override Domain DefaultDomain => Domain.TitleScreen;
        public sealed override bool IsNotSaved => true;

        int _sourceId;
        Model _popup;
        Prompts _prompts;
        Prompt _backPrompt;

        public UIState UIState => UIState.ModalState(HUDState.None);
        public Transform PromptsHost => View<VTitleScreenUI>().PromptsHost;

        // === Initialization
        protected override void OnInitialize() {
            World.Services.Get<FpsLimiter>().RegisterLimit(this, FpsLimiter.DefaultUIFpsLimit);
            this.ListenTo(Events.AfterFullyInitialized, () => CreateWelcomePopups().Forget(), this);
            // --- Disable biome sounds in titlescreen
            Services.Get<AudioCore>().Toggle(false);
        }

        // === Flow logic
        public void ExitGame() {
            _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                LocTerms.PopupQuitGame.Translate(),
                PopupUI.AcceptTapPrompt(Exit),
                PopupUI.CancelTapPrompt(ClosePopup),
                LocTerms.PopupQuitGameTitle.Translate()
            );
        }
        
        // === Welcome popups
        async UniTaskVoid CreateWelcomePopups() {
            if (DomainErrorPopup.Displayed) {
                return;
            }
            Log.Marking?.Warning("Starting game with version: [" + Services.Get<GameConstants>().gameVersion + "] " + Application.version);

            Automations.Prepare();
            await CreateFocusPopup();
            
#if !UNITY_GAMECORE && !UNITY_PS5
            if (!Configuration.GetBool(ApplicationScene.IsGoG)) {
                if (!PlatformUtils.IsSteamInitialized) {
                    await CreateSteamMissingPopup();
                } else {
                    await EnsureAllSavesAreInSteamDirectory();
                }
            }
#endif
            
            //await ShowGammaScreen();
            await FileVerification();
            await PreloadShaders();
            if (GameMode.IsDemo) {
                OnDataCollectionDeclined();
            } else {
                await CreatePatchNotesPopup();
                await SpawnStartupPopup();
            }
            await CreateGraphicPresetPopup();

            if (TitleScreen.autoContinueGame) {
                TitleScreen.autoContinueGame = false;
                PauseMusic();
                SaveSlot slot = SaveSlot.LastSaveSlot;
                LoadSave.Get.Load(slot, "TitleScreen Continue from PS5 activity");
            } else {
                SpawnTitleScreenUI();
            }

            if (Automations.HasAutomation) {
                await Automations.Run();
                Automations.Finish();
            }
        }

        static bool s_shownFocusPopup;
        async UniTask CreateFocusPopup() {
            if (s_shownFocusPopup || Automations.HasAutomation) {
                return;
            }
            s_shownFocusPopup = true;
            var model = World.Add(new PressStartUI());
            while (!model.WasDiscarded) {
                await UniTask.NextFrame();
            }
        }

        async UniTask FileVerification() {
#if !UNITY_GAMECORE && !UNITY_PS5
            var fileIntegrity = ApplicationFileIntegrityChecker.Instance;
            if (fileIntegrity != null) {
                var panel = AddElement(new FileIntegrityPanel(fileIntegrity));
                await AsyncUtil.WaitForDiscard(panel);
            }
#endif
        }
        
        async UniTask PreloadShaders() {
            if (ShadersPreloader.ShouldPreload() == false) {
                return;
            }
            var panel = AddElement(new ShadersPreloadingPanel());
            await AsyncUtil.WaitForDiscard(panel);
        }

        async UniTask ShowGammaScreen() {
            const string PrefKey = "Gamma_Screen";
            if (PrefMemory.GetBool(PrefKey) || Automations.HasAutomation) {
                return;
            }
            Log.Marking?.Warning("Showing gamma screen");
            await GammaScreen.ShowGammaScreen(closeable: false);
            
            PrefMemory.Set(PrefKey, true, false);
        }
        
        async UniTask SpawnStartupPopup() {
            // steam users agreed to ToS and don't need to see popup
            bool skipDataConsent = PlatformUtils.IsConsole || PlatformUtils.IsSteamInitialized || PrefMemory.GetBool("consent") || Automations.HasAutomation;
            if (skipDataConsent) {
                return;
            }
            var startupPopup = new TitlePopupUI(LocTerms.StartupPopupMessage, ContinueAfterDataCollectionPopup, OnDataCollectionDeclined);
            World.Add(startupPopup);
            TextLinkHandler.OpenLinksOf(startupPopup);
            await AsyncUtil.WaitForDiscard(startupPopup);
        }

        
#if !UNITY_GAMECORE && !UNITY_PS5
        async UniTask CreateSteamMissingPopup() {
            _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                LocTerms.PopupMissingSteamMessage.Translate(),
                PopupUI.AcceptTapPrompt(ClosePopup),
                PopupUI.ExitTapPrompt(ClosePopupAndExit),
                LocTerms.PopupMissingSteamTitle.Translate());
            
            await AsyncUtil.WaitForDiscard(_popup);
        }

        async UniTask EnsureAllSavesAreInSteamDirectory() {
            var gogMigration = new SaveFileMigrationToSteam.GoGSaveMigration();
            var defaultMigration = new SaveFileMigrationToSteam.DefaultSaveMigration();
            
            if (!gogMigration.FilesExist() 
                && !defaultMigration.FilesExist()) {
                return; // no migration needed
            }

            Log.Marking?.Warning("Displaying migration popup");
            _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                LocTerms.PopupMigrateSavesToSteamMessage.Translate(),
                PopupUI.AcceptTapPrompt(() => {
                    Log.Marking?.Warning("Migrating saves to Steam");
                    gogMigration.Migrate();
                    defaultMigration.Migrate();
                    ClosePopupAndExit();
                }),
                PopupUI.CancelTapPrompt(ClosePopup),
                LocTerms.PopupMigrateSavesToSteamTitle.Translate());
            
            await AsyncUtil.WaitForDiscard(_popup);
        }

#endif
        
        async UniTask CreatePatchNotesPopup() {
            if (Automations.HasAutomation) {
                return;
            }
            GameConstants gameConstants = Services.Get<GameConstants>(); 
            var patchNote = Services.TryGet<PatchNotesContainer>()?.For(gameConstants.gameVersion);
            bool hasPatchNote = patchNote != null;

            if (!hasPatchNote) {
                return;
            }
            string patchNoteKey = $"shown_patch_note_{patchNote.version}";
            
            if (!PrefMemory.GetBool(patchNoteKey)) {
                Log.Marking?.Warning("Showing patch notes");

                string title = patchNote.title.ToString();
                string message = patchNote.message;
                Type viewType;
                if (patchNote.artRef is { IsSet: true }) {
                    message = title + "\n\n\n" + message;
                    viewType = typeof(VFullScreenPopupUI);
                } else {
                    viewType = typeof(VSmallPopupUI);
                }

                var popup = PopupUI.SpawnNoChoicePopup(viewType, message, title);
                popup.ListenTo(Events.BeforeDiscarded, () => PrefMemory.Set(patchNoteKey, true, true), this);

                if (viewType == typeof(VFullScreenPopupUI)) {
                    popup.SetArt(patchNote.artRef.Get());
                }

                await AsyncUtil.WaitForDiscard(popup);
            }
        }
        
        async UniTask CreateGraphicPresetPopup() {
#if !UNITY_GAMECORE && !UNITY_PS5
            const string GraphicPresetPopupShown = "graphic_preset_popup_shown";
            if (PrefMemory.GetBool(GraphicPresetPopupShown) || Automations.HasAutomation) {
                return;
            }

            Log.Marking?.Warning("Showing graphic preset popup");

            var graphicPresets = World.Only<SettingsMaster>().Element<GraphicPresets>();
            string message = LocTerms.PopupGraphicsPresetMessage.Translate(graphicPresets.ActivePreset.DisplayName);
            string title = LocTerms.PopupGraphicsPresetTitle.Translate();
            _popup = PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI), message, title, () => PrefMemory.Set(GraphicPresetPopupShown, true, false));
            await AsyncUtil.WaitForDiscard(_popup);
#endif
        }

        void ClosePopup() {
            _popup?.Discard();
            _popup = null;
        }
        
        void ClosePopupAndExit() {
            ClosePopup();
            Exit();
        }

        void OnDataCollectionDeclined() {
            World.Only<CollectData>().Disable();
            ContinueAfterDataCollectionPopup();
        }

        void ContinueAfterDataCollectionPopup() {
            PrefMemory.Set("consent", true, true);
        }

        void SpawnTitleScreenUI() {
#if !UNITY_GAMECORE && !UNITY_PS5
            World.Add(new GameAnalyticsController());
#endif
            World.SpawnView(this, typeof(VTitleScreenOverlayUI));
            World.SpawnView(this, typeof(VTitleScreenUI));
            InitPrompts();
        }

        public static void Exit() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            LogsCollector.Dispose();
            // hijack unity logger to write to stderr
            // it prevents crashes from unity logging 
            new StandardErrorLogHandler().Register();
            try {
                PrefMemory.Save();
#if !UNITY_GAMECORE && !UNITY_PS5
                // GameAnalyticsSDK.GameAnalytics.EndSession();
#endif
            } catch (System.Exception e) {
                UnityEngine.Debug.LogException(e);
            }
#if UNITY_STANDALONE_WIN
            Awaken.Utility.LowLevel.WindowsKernelHelpers.KillCurrentProcess();
#endif
            UnityEngine.Application.Quit();
#endif
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Services.Get<AudioCore>().Toggle(true);
        }
        
        // === Music
        public void PauseMusic() {
            View<VTitleScreenMusic>()?.PauseMusic();
        }
        
        void InitPrompts() {
            _prompts = AddElement(new Prompts(this));
            _backPrompt = _prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), View<VTitleScreenUI>().Back), this, false, false);
        }
        
        public void RefreshPrompt(bool state) {
            _backPrompt?.SetupState(state, state);
        }
    }
}