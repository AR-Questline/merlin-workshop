using System;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Selections;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu.SaveLoadUI {
    [SpawnsView(typeof(VModalBlocker), false)]
    [SpawnsView(typeof(VSaveLoadUI))]
    public partial class LoadMenuUI : Model, ISaveLoadUI, IPromptHost, IUIAware {
        public sealed override bool IsNotSaved => true;

        protected SaveLoadSlotUI _hoveredSlotUI;
        protected Model _popup;
        protected EditNameUI _editNameUI;
        protected Prompt _acceptPrompt;
        
        SaveLoadSlotUI _editingSlotUI;
        Prompt _renamePrompt;
        Prompt _removePrompt;
        Prompt _inputUnfocusedConfirmPrompt; //used on popup
        Prompt _inputFocusedConfirmPrompt; //used on popup
        Action _onGamepadInputAccepted;
        
        public override Domain DefaultDomain => Domain.Globals;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown).WithPauseTime();
        public virtual string TitleName => LocTerms.LoadGame.Translate();
        public Transform SlotsParent => View<VSaveLoadUI>().SlotsParent;
        public Transform PromptsHost => View<VSaveLoadUI>().PromptsHost;
        
        protected VSaveLoadUI View => View<VSaveLoadUI>();
        
        protected override void OnFullyInitialized() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Selection.Events.SelectionChanged, this, OnSelectionChanged);
            InitializeSlots();
            InitializePrompts();
            View<VSaveLoadUI>().NewSaveSlotParent.gameObject.SetActive(false);
        }

        void InitializeSlots() {
            int i = 0;
            foreach (SaveSlot saveSlot in World.All<SaveSlot>().ToArraySlow().OrderByDescending(s => s.LastSavedTime)) {
                var slotUI = new SaveLoadSlotUI(saveSlot, i);
                AddElement(slotUI);
                World.SpawnView<VSaveLoadSlotUI>(slotUI, true, true, View<VSaveLoadUI>().SlotsParent);
                ++i;
            }
        }

        void InitializePrompts() {
            var prompts = new Prompts(this);
            AddElement(prompts);

            _renamePrompt = Prompt.Tap(KeyBindings.UI.Saving.RenameSaveSlot, LocTerms.Rename.Translate(), OpenRenamePopup);
            prompts.AddPrompt(_renamePrompt, this, false);

            _removePrompt = Prompt.Tap(KeyBindings.UI.Saving.RemoveSaveSlot, LocTerms.Remove.Translate(), OpenDeletePopup);
            prompts.AddPrompt(_removePrompt, this, false);
            
            _acceptPrompt = Prompt.VisualOnlyTap(KeyBindings.UI.Saving.SaveSlotAction, LocTerms.Override.Translate());
            _acceptPrompt.ChangeName(this is SaveMenuUI ? LocTerms.Override.Translate() : LocTerms.Load.Translate());
            prompts.AddPrompt(_acceptPrompt, this, false);
            
            var backPrompt = Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), Close);
            prompts.AddPrompt(backPrompt, this);
        }

        void OpenRenamePopup() {
            _editingSlotUI = _hoveredSlotUI;
            OpenEditNamePopup(LocTerms.Rename.Translate(), _editingSlotUI.saveSlot.DisplayName, RenameSaveSlot);
        }

        void OpenDeletePopup() {
            _editingSlotUI = _hoveredSlotUI;
            World.Only<Selection>().Select(null);
            
            _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                LocTerms.PopupSavedSlotClear.Translate(),
                PopupUI.AcceptTapPrompt(DeleteSlot),
                PopupUI.CancelTapPrompt(ClosePopup),
                LocTerms.Delete.Translate()
            );
            
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, _popup));
        }

        void DeleteSlot() {
            _editingSlotUI.DeleteSlot();
            int i = 0;
            foreach (SaveLoadSlotUI slotUI in Elements<SaveLoadSlotUI>().ToArraySlow().OrderByDescending(s => s.saveSlot.LastSavedTime)) {
                slotUI.RefreshIndex(i);
                ++i;
            }
            View.RecyclableCollectionManager.OrderChangedRefresh();
            
            ClosePopup();
        }

        void Close() {
            Discard();
        }

        void RenameSaveSlot() {
            if (!_editNameUI.Validate()) {
                return;
            }

            if (_editNameUI.InitialValueChanged) {
                _editingSlotUI.saveSlot.Rename(_editNameUI.Value);
                _editingSlotUI.TriggerChange();
            }
            
            ClosePopup();
        }

        void SetupPopupWithPrompts(string popupTitle, string slotName, Action onInputAccepted) {
            _editNameUI = new EditNameUI(slotName, onInputAccepted, ClosePopup, true);
            AddElement(_editNameUI);
            _editNameUI.ListenTo(EditNameUI.Events.InputFieldFocusChanged, isInputFocused => {
                _inputUnfocusedConfirmPrompt.SetupState(!isInputFocused, !isInputFocused);
                _inputFocusedConfirmPrompt.SetupState(isInputFocused, isInputFocused);
            }, this);
            var popupContent = new DynamicContent(_editNameUI, typeof(VEditNameUI));
            
            _inputUnfocusedConfirmPrompt = Prompt.Tap(KeyBindings.UI.Saving.RenameSaveSlotInputUnfocused, LocTerms.Confirm.Translate(), onInputAccepted, Prompt.Position.Last).AddAudio();
            _inputUnfocusedConfirmPrompt.SetupState(!_editNameUI.IsInputFieldFocused, !_editNameUI.IsInputFieldFocused);
            _inputFocusedConfirmPrompt = Prompt.Tap(KeyBindings.UI.Saving.RenameSaveSlotInputFocused, LocTerms.Confirm.Translate(), onInputAccepted).AddAudio();
            _inputFocusedConfirmPrompt.SetupState(_editNameUI.IsInputFieldFocused, _editNameUI.IsInputFieldFocused);
            
            _popup = PopupUI.SpawnSimplePopup3Choices(typeof(VSmallPopupUI),
                string.Empty,
                _inputUnfocusedConfirmPrompt,
                _inputFocusedConfirmPrompt,
                PopupUI.CancelTapPrompt(ClosePopup),
                popupTitle,
                popupContent
            );
        }
        
        protected void OpenEditNamePopup(string popupTitle, string slotName, Action onInputAccepted) {
            _onGamepadInputAccepted = onInputAccepted;
            _editingSlotUI = _hoveredSlotUI;
            World.Only<Selection>().Select(null);
            SetupPopupWithPrompts(popupTitle, slotName, onInputAccepted);
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, _popup));
        }
        
        protected void ClosePopup() {
            _editNameUI?.Discard();
            _editNameUI = null;
            _popup?.Discard();
            _popup = null;
        }

        public virtual void OnSelectionChanged(SelectionChange selectionChange) {
            SaveLoadSlotUI slotUI = selectionChange.Target as SaveLoadSlotUI;
            _hoveredSlotUI = slotUI;
            bool selectionValid = selectionChange.Selected && _hoveredSlotUI != null;
            bool renameActive = selectionValid && !_hoveredSlotUI.saveSlot.IsQuickSave && !_hoveredSlotUI.saveSlot.IsAutoSave;
            _renamePrompt?.SetupState(renameActive, renameActive);
            _removePrompt.SetActive(selectionValid);
            SetupAcceptPrompt(selectionChange);
        }

        public virtual void SaveLoadAction(SaveLoadSlotUI saveSlotUI) {
            SaveSlot slot = saveSlotUI.saveSlot;
            LoadSave.Get.Load(slot, World.HasAny<DeathUI.DeathUI>() ? "Death Load UI" : "Menu Load UI");
            Close();
        }

        public virtual void SetupAcceptPrompt(SelectionChange selectionChange) {
            _acceptPrompt.SetActive(selectionChange.Selected);
        }

        public UIResult Handle(UIEvent evt) {
            if (RewiredHelper.IsGamepad) {
                if (evt is UIKeyDownAction action && action.Name == KeyBindings.UI.Saving.RenameSaveSlotInputFocused) {
                    _onGamepadInputAccepted?.Invoke();
                    return UIResult.Accept;
                    
                }
            }
            
            return UIResult.Ignore;
        }
    }
}