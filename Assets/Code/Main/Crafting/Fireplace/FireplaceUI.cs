using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.Resting;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
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
            _prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.Select.Translate(), Prompt.Position.First, ControlSchemeFlag.Gamepad), this);
        }

        public Model CookAction() {
            UpdateUiVisibility(false);
            return World.Add(new CraftingTabsUI(_cookingTabSet));
        }

        public Model AlchemyAction() {
            UpdateUiVisibility(false);
            return World.Add(new CraftingTabsUI(_alchemyTabSet));
        }
        
        public Model HandcraftingAction() {
            UpdateUiVisibility(false);
            var handCrafting = GameConstants.Get.GetBonfireCraftingUpgrade(Hero.Current.Development.BonfireCraftingLevel);
            var tabsDictionary = new Dictionary<CraftingTabTypes, CraftingTemplate>() {
                { CraftingTabTypes.RecipeHandcrafting, handCrafting }
            };
            var craftingTabSet = new TabSetConfig(tabsDictionary);
            return World.Add(new CraftingTabsUI(craftingTabSet));
        }

        public virtual void Upgrade() {
            IsUpgraded = true;
        }

        public Model GoToSleepAction() {
            UpdateUiVisibility(false);
            _restPopup = World.Add(new RestPopupUI(Services.Get<ViewHosting>().OnMainCanvas(), true));
            _restPopup.ListenTo(RestPopupUI.Events.RestingInitiated, Resting, this);
            return _restPopup;
        }

        public Model LevelUpAction() {
            UpdateUiVisibility(false);
            return CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Character, true, CharacterSheetTabType.LevelUpTabs);
        }

        public Model OpenHeroStorage() {
            UpdateUiVisibility(false);
            return Hero.Current.Storage.Open();
        }

        public void SaveGame() {
            SaveGame(false);
        }
        
        public void UpdateUiVisibility(bool state) {
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