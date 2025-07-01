using Awaken.TG.Debugging;
using Awaken.TG.Graphics;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.Main.UI.Bugs;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Menu.Artbook;
using Awaken.TG.Main.UI.Menu.OST;
using Awaken.TG.Main.UI.Menu.SaveLoadUI;
using Awaken.TG.Main.UI.PhotoMode;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.UI.Menu {
    [SpawnsView(typeof(VModalBlocker), false)]
    [SpawnsView(typeof(VMenuUI))]
    public partial class MenuUI : Model, IUIStateSource {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        // === Properties
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden).WithPauseTime();

        Model _popup;
        
        // === Initialization
        protected override void OnInitialize() {
            World.Services.Get<FpsLimiter>().RegisterLimit(this, FpsLimiter.DefaultUIFpsLimit);
            MemoryClear.ReferencesCachesRevalidate();
            this.ListenTo(VModalBlocker.Events.ModalDismissed, Close, this);
        }

        public static void OpenLoadUI() {
            World.Add(new LoadMenuUI());
        }

        public static void OpenSaveUI() {
            World.Add(new SaveMenuUI());
        }

        public static void OpenSettingUI() {
            World.Add(new AllSettingsUI());
        }
        
        public static void OpenOstUI() {
            World.Add(new OstUI());
        }
        
        public static void OpenArtbookUI() {
            World.Add(new ArtbookUI());
        }

        public static void OpenBugReportUI() {
#if !UNITY_GAMECORE
            World.Add(new UserBugReporting(false));
#endif
        }
        
        public void QuitToMenu() {
            _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                UnsavedSavesInfo.GetUnsavedGameWarning(),
                PopupUI.AcceptTapPrompt(ExitToMenu),
                PopupUI.CancelTapPrompt(ClosePopup),
                LocTerms.PopupQuitToTitle.Translate()
            );
        }
        
        public void QuitGame() {
            _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                UnsavedSavesInfo.GetUnsavedGameWarning(),
                PopupUI.AcceptTapPrompt(ExitGame),
                PopupUI.CancelTapPrompt(ClosePopup),
                LocTerms.PopupQuitToSystem.Translate()
            );
        }
        
        public void ReportBug() {
            OpenBugReportUI();
        }
        
        public void ShowOptions() {
            OpenSettingUI();
        }
        
        public void Unstuck() {
            HeroUnstuck.Unstuck().Forget();
            Close();
        }

        public void ShowPhotoMode() {
            World.Add(new PhotoModeUI());
            Close();
        }
        
        // === Execution
        void ClosePopup() {
            _popup?.Discard();
            _popup = null;
        }

        void ExitToMenu() {
            Log.Marking?.Warning("Exit to menu");
            ClosePopup();
            LoadTitleScreen();
        }

        public static void LoadLastCheckpoint() {
            ScenePreloader.Load(SaveSlot.LastSaveSlot, "Death Last Save");
        }

        public static void LoadTitleScreen() {
            ScenePreloader.LoadTitleScreen();
        }

        public static void ExitGame() {
            TitleScreenUI.Exit();
        }

        // === Discarding
        public void Close() {
            _popup?.Discard();
            _popup = null;
            Discard();
        }
    }
}
