using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.Main.UI.Menu.SaveLoadUI;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu.DeathUI {
    [UsesPrefab("UI/VDeathUI")]
    public class VDeathUI : View<DeathUI>, IAutoFocusBase {
        const float FadeTime = 1.5f;
        
        [SerializeField] CanvasGroup mainContent;
        [SerializeField] public EventReference deathSound;
        [SerializeField] TextMeshProUGUI screenTitleText;
        [SerializeField] ButtonConfig revive;
        [SerializeField] ButtonConfig loadLastCheckpoint;
        [SerializeField] ButtonConfig loadGame;
        [SerializeField] ButtonConfig options;
        [SerializeField] ButtonConfig bugReport;
        [SerializeField] ButtonConfig exitToMenu;
        [SerializeField] ButtonConfig exitGame;

        DirectTimeMultiplier _timeMultiplier;
        bool _reviveEnabled;
        bool _updateDisabled;
        
        public Component DefaultFocus => loadLastCheckpoint.button;
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        static bool AllowExitGameButton => !PlatformUtils.IsConsole;

        protected override void OnInitialize() {
            if (!deathSound.IsNull) {
                FMODManager.PlayOneShot(deathSound);
            }
            SetInteractability(false);
            SlowDownTime();
            FadeIn().Forget();
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<ISaveLoadUI>(), Target, OnSaveLoadOpened);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<ISaveLoadUI>(), Target, OnSaveLoadClosed);

        }

        protected override void OnFullyInitialized() {
            World.Only<CheatController>().ListenTo(Model.Events.AfterChanged, HandleReviveButton, this);
            screenTitleText.SetText(LocTerms.GameOver.Translate());
            InitializeButtons();
        }
        
        void OnSaveLoadOpened() {
            gameObject.SetActive(false);
        }
        
        void OnSaveLoadClosed() {
            gameObject.SetActive(true);
        }

        void HandleReviveButton() {
            bool reviveEnabled = CheatController.CheatsEnabled();
            revive.button.Interactable = reviveEnabled;
            revive.button.gameObject.SetActive(reviveEnabled);
        }

        void InitializeButtons() {
            HandleReviveButton();
            
            revive.InitializeButton(Target.Revive);
            loadLastCheckpoint.InitializeButton(MenuUI.LoadLastCheckpoint);
            loadGame.InitializeButton(MenuUI.OpenLoadUI);
            options.InitializeButton(MenuUI.OpenSettingUI);
#if UNITY_GAMECORE
            bugReport.gameObject.SetActive(false);
#else
            bugReport.InitializeButton(MenuUI.OpenBugReportUI);
#endif
            
            if (World.Only<DifficultySetting>().Difficulty.SaveRestriction.HasFlagFast(SaveRestriction.Hardcore)) {
                HardcoreSetup();
                _updateDisabled = true;
            } else {
                exitToMenu.InitializeButton(MenuUI.LoadTitleScreen);
                exitGame.InitializeButton(MenuUI.ExitGame);
                RefreshLoadButtons();
            }
            exitGame.gameObject.SetActive(AllowExitGameButton);
        }

        void Update() {
            if (_updateDisabled) return;
            
            RefreshLoadButtons();
        }

        void RefreshLoadButtons() {
            var loadActive = World.HasAny<SaveSlot>() && LoadSave.Get.LoadAllowedInMenu();
            loadGame.button.Interactable = loadActive;
            loadLastCheckpoint.button.Interactable = loadActive;
        }

        void HardcoreSetup() {
            loadLastCheckpoint.button.gameObject.SetActive(false);
            loadGame.button.gameObject.SetActive(false);
            exitToMenu.button.gameObject.SetActive(true);
            exitGame.button.gameObject.SetActive(true);
            exitToMenu.InitializeButton(() => HardcoreExit(true));
            exitToMenu.SetText("<sprite name=\"t\" color=#ff0000> " + exitToMenu.Text);
            exitGame.InitializeButton(() => HardcoreExit(false));
            exitGame.SetText("<sprite name=\"t\" color=#ff0000> " + exitGame.Text);
        }
        
        void HardcoreExit(bool toMenu) {
            // Delete all save slots with current hero id
            World.All<SaveSlot>()
                .Where(s => s.HeroId == Hero.Current.HeroID)
                .ToArray()
                .ForEach(s => s.Discard());
                
            if (toMenu) {
                MenuUI.LoadTitleScreen();
            } else {
                MenuUI.ExitGame();
            }
        }

        void SlowDownTime() {
            _timeMultiplier = new DirectTimeMultiplier(1, ID);
            World.Only<GlobalTime>().AddTimeModifier(_timeMultiplier);
            DOTween.To(() => Time.timeScale, _timeMultiplier.Set, 0f, FadeTime * 2f).SetUpdate(true);
        }
        
        async UniTaskVoid FadeIn() {
            mainContent.alpha = 0;
            await mainContent.DOFade(1, FadeTime).SetUpdate(true);
            
            SetInteractability(true);
        }

        void SetInteractability(bool interactable) {
            mainContent.blocksRaycasts = interactable;
            if (interactable) {
                World.Only<Focus>().Select(DefaultFocus);
            }
        }
        
        protected override IBackgroundTask OnDiscard() {
            _timeMultiplier.Remove();
            return base.OnDiscard();
        }
    }
}