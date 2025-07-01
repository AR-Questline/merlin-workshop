using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using UnityEngine;
using ARDifficulty = Awaken.TG.Main.Settings.Gameplay.Difficulty;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Difficulty {
    [SpawnsView(typeof(VChooseDifficulty))]
    public partial class ChooseDifficulty : Model, IPromptHost {
        public sealed override bool IsNotSaved => true;
        public override Domain DefaultDomain => Domain.Gameplay;
        public Transform PromptsHost => View<VChooseDifficulty>().PromptsHost;
        Prompts Prompts => Element<Prompts>();
        VChooseDifficulty View => View<VChooseDifficulty>();
        
        ARDifficulty _selectedDifficulty;
        ARDifficulty _availableModeDifficulty;
        ARDifficulty _selectedModeDifficulty;
        Prompt _selectPrompt;
        Prompt _confirm;
        Prompt _storyModePrompt;
        Prompt _survivalModePrompt;
        bool _toggleMode;
        
        public new static class Events {
            public static readonly Event<ChooseDifficulty, ARDifficulty> DifficultySelected = new(nameof(DifficultySelected));
            public static readonly Event<ChooseDifficulty, ChooseDifficulty> DifficultyChooseConfirmed = new(nameof(DifficultyChooseConfirmed));
            public static readonly Event<ChooseDifficulty, bool> DifficultyModeToggled = new(nameof(DifficultyModeToggled));
        }

        protected override void OnInitialize() {
            AddElement(new Prompts(this));
        }

        protected override void OnFullyInitialized() {
            InitPrompts();
        }

        void InitPrompts() {
            _selectPrompt = Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.Select.Translate(), Prompt.Position.First);
            _confirm = Prompt.Tap(KeyBindings.UI.Settings.ApplyChanges, LocTerms.Confirm.Translate(), TryConfirm);
            _storyModePrompt = Prompt.Tap(KeyBindings.UI.Settings.RestoreDefaults, LocTerms.DifficultyStory.Translate(), ToggleMode);
            _survivalModePrompt = Prompt.Tap(KeyBindings.UI.Settings.RestoreDefaults, LocTerms.DifficultySurvival.Translate(), ToggleMode);

            Prompts.AddPrompt(_selectPrompt, this, PromptsHost).AddAudio();
            Prompts.AddPrompt(_confirm, this, PromptsHost).SetupState(true, false).AddAudio();
            Prompts.BindPrompt(_storyModePrompt, this, View.StoryModePromptUI).SetupState(false, true);
            Prompts.BindPrompt(_survivalModePrompt, this, View.SurvivalModePromptUI).SetupState(false, true);
        }

        void TryConfirm() {
            RefreshInteractivePrompts(false);
            Reference<PopupUI> popup = new();
            
            popup.item = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                LocTerms.PopupConfirmApplyingChanges.Translate(),
                PopupUI.AcceptTapPrompt(Confirm),
                PopupUI.CancelTapPrompt(() => {
                    RefreshInteractivePrompts(true);
                    popup.item.Discard();
                }),
                LocTerms.Confirm.Translate()
            );
            return;

            void Confirm() {
                if (_selectedDifficulty == ARDifficulty.Easy && _selectedModeDifficulty == ARDifficulty.Story) {
                    _selectedDifficulty = ARDifficulty.Story;
                } else if (_selectedDifficulty == ARDifficulty.Hard && _selectedModeDifficulty == ARDifficulty.Survival) {
                    _selectedDifficulty = ARDifficulty.Survival;
                }
            
                var difficultySetting = World.Only<DifficultySetting>();
                difficultySetting.SetCurrentDifficulty(_selectedDifficulty);
                
                this.Trigger(Events.DifficultyChooseConfirmed, this);
                popup.item.Discard();
            }
        }
        
        public void SelectPreset(ARDifficulty difficulty, ARDifficulty modeDifficulty) {
            if (_selectedDifficulty == difficulty) return;
            
            _confirm.SetActive(true);
            _toggleMode = false;

            _selectedDifficulty = difficulty;
            _selectedModeDifficulty = null;
            PrepareModeSection(modeDifficulty); 
            
            this.Trigger(Events.DifficultySelected, _selectedDifficulty);
        }

        void PrepareModeSection(ARDifficulty modeDifficulty) {
            _availableModeDifficulty = modeDifficulty;
            _storyModePrompt.SetVisible(modeDifficulty == ARDifficulty.Story);
            _survivalModePrompt.SetVisible(modeDifficulty == ARDifficulty.Survival);
            View.SetupModeSection(modeDifficulty != null, modeDifficulty?.Description);
        }

        public void ToggleMode() {
            if (_availableModeDifficulty == null) return;
            
            _toggleMode = !_toggleMode;
            _selectedModeDifficulty = _toggleMode ? _availableModeDifficulty : null;
            this.Trigger(Events.DifficultyModeToggled, _toggleMode);
        }

        void RefreshInteractivePrompts(bool active) {
            _confirm.SetActive(active);
            _storyModePrompt.SetActive(active);
            _survivalModePrompt.SetActive(active);
        }
    }
}