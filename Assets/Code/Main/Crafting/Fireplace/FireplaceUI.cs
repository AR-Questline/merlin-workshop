using System.Collections.Generic;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Resting;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Fireplace {
    public partial class FireplaceUI : Model, IUIStateSource, IPromptHost {
        public sealed override bool IsNotSaved => true;

        readonly TabSetConfig _cookingTabSet;
        readonly TabSetConfig _alchemyTabSet;

        Prompt _closePrompt;
        Prompts _prompts;
        PopupUI _tutorialPopup;
        RestPopupUI _restPopup;

        public override Domain DefaultDomain => Domain.Gameplay;
        public virtual UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden | HUDState.QuestTrackerHidden).WithPauseWeatherTime();
        public Transform PromptsHost { get; private set; }
        public bool UiVisible { get; private set; }
        public bool IsUpgraded { get; private set; }

        public FireplaceUI(TabSetConfig cookingTabSetConfig, TabSetConfig alchemyTabSetConfig, bool manualRestTime, bool startUpgraded = false) {
            _cookingTabSet = cookingTabSetConfig;
            _alchemyTabSet = alchemyTabSetConfig;
            UiVisible = true;
            IsUpgraded = startUpgraded;
        }

        protected override void OnFullyInitialized() {
            PromptsHost = View<VFireplaceUI>().PromptHost;
            InitPrompts();
            Hero.Current.ListenTo(ICharacter.Events.CombatEntered, _ => Close(false), this);
            Hero.Current.ListenTo(Hero.Events.FastTraveled, _ => Close(true), this);
            if (Hero.TppActive) {
                Hero.Current.VHeroController.HeroCamera.SetPitch(0);
            }
        }
        
        void InitPrompts() {
            _prompts = AddElement(new Prompts(this));
            var view = View<VFireplaceUI>();
            _closePrompt = _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), () => Close(true)), this, view.ClosePrompt, view.ClosePromptActive);
        }

        public void CookAction() {
            VGUtils.ToggleCrafting(_cookingTabSet);
        }

        public void AlchemyAction() {
            VGUtils.ToggleCrafting(_alchemyTabSet);
        }
        
        public void HandcraftingAction() {
            var handCrafting = GameConstants.Get.GetBonfireCraftingUpgrade(Hero.Current.Development.BonfireCraftingLevel);
            var tabsDictionary = new Dictionary<CraftingTabTypes, CraftingTemplate>() {
                { CraftingTabTypes.RecipeHandcrafting, handCrafting }
            };
            var craftingTabSet = new TabSetConfig(tabsDictionary);
            VGUtils.ToggleCrafting(craftingTabSet);
        }

        public virtual void Upgrade() {
            IsUpgraded = true;
        }

        public void GoToSleepAction() {
            _restPopup = World.Add(new RestPopupUI(View<VFireplaceUI>().transform, true));
            _restPopup.ListenTo(RestPopupUI.Events.RestingInitiated, Resting, this);
        }

        public void LevelUpAction() {
            UpdateUiVisibility(false);
            var characterSheetUI = CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Character, true, CharacterSheetTabType.LevelUpTabs);
            characterSheetUI.ListenTo(Events.AfterDiscarded, () => UpdateUiVisibility(true), this);
        }

        public void SaveGame() {
            SaveGame(false);
        }
        
        protected void UpdateUiVisibility(bool state) {
            if (UiVisible == state) return;
            UiVisible = state;
            var view = View<VFireplaceUI>();
            _closePrompt.SetupState(state, state && view.ClosePromptActive);
            this.TriggerChange();
        }

        protected virtual void Resting() {
            Close(false);
        }

        void Close(bool saveOnExit) {
            _tutorialPopup?.Discard();
            _restPopup?.Discard();
            Discard();
            
            if (saveOnExit) {
                SaveGame(true);
            }
        }
        
        // === Helpers
        static void SaveGame(bool autoSave) {
            if (LoadSave.Get.CanSystemSave()) {
                if (autoSave) {
                    World.Services.TryGet<AutoSaving>()?.AutoSaveWithRecurringRetry();
                } else {
                    LoadSave.Get.QuickSave().Forget();
                }
            }
        }
    }
}