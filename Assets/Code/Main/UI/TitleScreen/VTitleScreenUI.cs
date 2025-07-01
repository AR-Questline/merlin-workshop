using System;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.Main.UI.Menu.Artbook;
using Awaken.TG.Main.UI.Menu.OST;
using Awaken.TG.Main.UI.Menu.SaveLoadUI;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    [UsesPrefab("TitleScreen/" + nameof(VTitleScreenUI))]
    public class VTitleScreenUI : View<TitleScreenUI>, IAutoFocusBase, IFocusSource {
        [Title("Title Menu Buttons")]
        [SerializeField] GameObject defaultButtonsParent;
        [SerializeField] ButtonConfig continueGame;
        [SerializeField] ButtonConfig newGame;
        [SerializeField] ButtonConfig loadGame;
        [SerializeField] ButtonConfig options;
        [SerializeField] ButtonConfig ost;
        [SerializeField] ButtonConfig artbook;
        [SerializeField] ButtonConfig exitGame;
        [SerializeField] GameObject[] dlcObjects = Array.Empty<GameObject>();
        [Title("New Game Buttons")]
        [SerializeField] GameObject newGameButtonsParent;
        [SerializeField] ButtonConfig prologue;
        [SerializeField] ButtonConfig hornsOfTheSouth;
        [Title("Footer")]
        [SerializeField] Transform promptHost;

        ButtonConfig[] _allButtons;
        bool _isDescriptionShown;

        public Transform PromptsHost => promptHost;
        public bool ForceFocus => true;
        public Component DefaultFocus => LoadSave.Get.CanContinue() ? continueGame.button : newGame.button;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            SwitchButtons(true);
            InitializeButtons();
            
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<LoadMenuUI>(), this, model => {
                CorrectFocusOnReturn(model);
                RefreshButtons();
            });
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<AllSettingsUI>(), this, CorrectFocusOnReturn);
            
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<OstUI>(), this, () => gameObject.SetActiveOptimized(false));
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<OstUI>(), this, model => {
                gameObject.SetActiveOptimized(true);
                CorrectFocusOnReturn(model);
            });
        }

        void InitializeButtons() {
            newGame.InitializeButton(GameMode.IsDemo 
                ? StartPrologue 
                : NewGameAction
            );
            
            continueGame.InitializeButton(() => {
                Target.PauseMusic();
                FMODManager.PlayOneShotAfter(CommonReferences.Get.AudioConfig.StartGameSound, continueGame.button.clickSound, this).Forget();
                SaveSlot slot = SaveSlot.LastSaveSlot;
                LoadSave.Get.Load(slot, "TitleScreen Continue");
            });
            
            loadGame.InitializeButton(MenuUI.OpenLoadUI);
            options.InitializeButton(MenuUI.OpenSettingUI);
            
            if (HasSupportersPackDlc()) {
                ost.InitializeButton(MenuUI.OpenOstUI);
                artbook.InitializeButton(MenuUI.OpenArtbookUI);
            }
            foreach (GameObject obj in dlcObjects) {
                obj.SetActiveOptimized(HasSupportersPackDlc());
            }
            
            exitGame.InitializeButton(Target.ExitGame);
            
            prologue.InitializeButton(StartPrologue);
            hornsOfTheSouth.InitializeButton(SkipPrologue);

            RefreshButtons();
        }

        public void SwitchButtons(bool isDefault) {
            Target.RefreshPrompt(!isDefault);
            defaultButtonsParent.SetActiveOptimized(isDefault);
            newGameButtonsParent.SetActiveOptimized(!isDefault);
            World.Only<Focus>().Select(isDefault ? DefaultFocus : prologue.button);
        }

        void NewGameAction() {
            if (TitleScreenUI.SkipPrologueUnlocked) {
                SwitchButtons(false);
            } else {
                StartPrologue();
            }
        }

        void StartPrologue() {
            Target.PauseMusic();
            CommonReferences commonReferences = CommonReferences.Get;
            
            FMODManager.PlayOneShotAfter(commonReferences.AudioConfig.StartGameSound, newGame.button.clickSound, this).Forget();
            SceneSets jailTutorial = commonReferences.presetSelectorConfig.JailTutorial;
            
            StartGameData data = new() {
                withHeroCreation = true,
                sceneReference = jailTutorial.Scene,
                characterPresetData = jailTutorial.presets.FirstOrDefault()
            };
            
            TitleScreenUtils.StartNewGame(data);
        }

        void SkipPrologue() {
            Target.PauseMusic();
            CommonReferences commonReferences = CommonReferences.Get;
            
            FMODManager.PlayOneShotAfter(commonReferences.AudioConfig.StartGameSound, newGame.button.clickSound, this).Forget();
            SceneSets hosSet = commonReferences.presetSelectorConfig.HornsOfTheSouth;
            
            StartGameData data = new() {
                withHeroCreation = true,
                sceneReference = hosSet.Scene, 
                characterPresetData = hosSet.presets.FirstOrDefault()
            };
            TitleScreenUtils.StartNewGame(data);
        }
        
        public void Back() {
            SwitchButtons(true);
        }

        void RefreshButtons() {
            bool canContinue = LoadSave.Get.CanContinue();
            continueGame.button.TrySetActiveOptimized(canContinue);
            loadGame.button.TrySetActiveOptimized(canContinue);
            exitGame.button.TrySetActiveOptimized(!PlatformUtils.IsConsole);
        }
        
        void CorrectFocusOnReturn(Model model) {
            switch (model) {
                case LoadMenuUI:
                    World.Only<Focus>().Select(loadGame.button);
                    return;
                case AllSettingsUI:
                    World.Only<Focus>().Select(options.button);
                    return;
                case OstUI:
                    World.Only<Focus>().Select(ost.button);
                    return;
                case ArtbookUI:
                    World.Only<Focus>().Select(artbook.button);
                    return;
            }
        }

        static bool HasSupportersPackDlc() {
            return SocialService.Get.HasDlc(CommonReferences.Get.SupportersPackDlcId);
        }
    }
}
