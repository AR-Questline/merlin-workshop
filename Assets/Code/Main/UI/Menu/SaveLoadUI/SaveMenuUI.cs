using System;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Selections;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using FMODUnity;

namespace Awaken.TG.Main.UI.Menu.SaveLoadUI {
    [SpawnsView(typeof(VModalBlocker), false)]
    [SpawnsView(typeof(VSaveLoadUI))]
    public partial class SaveMenuUI : LoadMenuUI {
#if UNITY_PS5
        public const int MaxSlotsCount = 50;
#else
        public const int MaxSlotsCount = 100;
#endif

        bool _isSaving;
        bool _canOverride;
        
        public override string TitleName => LocTerms.SaveGame.Translate();
        public bool CanCreateNewSaveSlot => Elements<SaveLoadSlotUI>().CountLessThan(MaxSlotsCount);
        
        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            AddElement<NewSaveSlotUI>();
        }

        // used only for overriding save slot; creating new save slot is handled through VNewSaveSlotUI
        public override void SaveLoadAction(SaveLoadSlotUI saveSlotUI) {
            if (!_canOverride) {
                // RuntimeManager.PlayOneShot(CommonReferences.Get.AudioConfig.StrongNegativeFeedbackSound);
                return;
            }
            
            if (_isSaving || !LoadSave.Get.CanPlayerSave() || _popup != null) {
                return;
            }
            
            _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                LocTerms.PopupSavedSlotOverride.Translate(),
                PopupUI.AcceptTapPrompt(() => {
                    ClosePopup();
                    OverrideSave(saveSlotUI).Forget();
                }),
                PopupUI.CancelTapPrompt(ClosePopup),
                LocTerms.Override.Translate()
            );
        }

        public void OpenNewSavePopup(Action onInputAccepted = null) {
            if (_isSaving || !LoadSave.Get.CanPlayerSave() || _editNameUI != null) {
                return;
            }
            Action acceptCallback = onInputAccepted ?? (() => CreateNewSave().Forget());
            OpenEditNamePopup(LocTerms.Create.Translate(), LocTerms.Slot.Translate(), acceptCallback);
        }

        public override void SetupAcceptPrompt(SelectionChange selectionChange) {
            bool isNewSaveSlot = selectionChange.Target is NewSaveSlotUI;
            _acceptPrompt.ChangeName(isNewSaveSlot ? LocTerms.Create.Translate() : LocTerms.Override.Translate());
            if (isNewSaveSlot) {
                _acceptPrompt.SetupState(true, true);
                return;
            }

            _canOverride = !_hoveredSlotUI.saveSlot.IsAutoSave && !_hoveredSlotUI.saveSlot.IsQuickSave;
            _acceptPrompt.SetupState(_canOverride, _canOverride);
        }

        async UniTaskVoid CreateNewSave() {
            if (_isSaving || _editNameUI == null || !_editNameUI.Validate()) {
                return;
            }

            _isSaving = true;
            View.SetActiveSavingBlend(true);
            
            string slotNewName = _editNameUI.Value;
            bool withCustomName = _editNameUI.InitialValueChanged;
            ClosePopup();

            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            
            var saveSlot = SaveSlot.GetAndSave(slotNewName, withCustomName);

            if (!await AsyncUtil.WaitUntil(this, () => !World.HasAny<SavingWorldMarker>())) {
                return;
            }

            if (saveSlot.HasBeenDiscarded) {
                View.RecyclableCollectionManager.OrderChangedRefresh();
            } else {
                AddNewSaveSlot(saveSlot);
            }
            View.SetActiveSavingBlend(false);
            _isSaving = false;
        }
        
        void AddNewSaveSlot(SaveSlot newSaveSlot) {
            int i = 0;
            foreach (SaveLoadSlotUI saveLoadSlotUI in Elements<SaveLoadSlotUI>().ToArraySlow().OrderByDescending(s => s.saveSlot.LastSavedTime)) {
                saveLoadSlotUI.RefreshIndex(++i);
            }
            var saveSlotUI = new SaveLoadSlotUI(newSaveSlot, 0);
            AddElement(saveSlotUI);
            World.SpawnView<VSaveLoadSlotUI>(saveSlotUI, true, true, View<VSaveLoadUI>().SlotsParent);
            View.RecyclableCollectionManager.OrderChangedRefresh();
        }
        
        async UniTaskVoid OverrideSave(SaveLoadSlotUI saveSlotUI) {
            if (_isSaving) {
                return;
            }
            _isSaving = true;
            View.SetActiveSavingBlend(true);

            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }

            var newSaveSlot = SaveSlot.OverrideAndSave(saveSlotUI.saveSlot);
            
            if (!await AsyncUtil.WaitUntil(this, () => !World.HasAny<SavingWorldMarker>())) {
                return;
            }
            
            if (newSaveSlot.HasBeenDiscarded) {
                View.RecyclableCollectionManager.OrderChangedRefresh();
            } else {
                AddNewSaveSlot(newSaveSlot);
            }
            View.SetActiveSavingBlend(false);
            _isSaving = false;
        }
    }
}