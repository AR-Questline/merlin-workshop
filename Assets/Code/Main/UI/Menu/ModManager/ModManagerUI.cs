using Awaken.TG.Assets.Modding;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using Awaken.Utility.Assets.Modding;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ModMgr = Awaken.Utility.Assets.Modding.ModManager;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.UI.Menu.ModManager {
    [SpawnsView(typeof(VModalBlocker), false)]
    [SpawnsView(typeof(VModManagerUI))]
    public partial class ModManagerUI : Model, IPromptHost {
        ModEntryUI _selectedModEntryUI;
        PopupUI _popup;
        Prompt _selectPrompt;
        Prompt _modUpPrompt;
        Prompt _modDownPrompt;
        Prompt _toggleLeftPrompt;
        Prompt _toggleRightPrompt;
        
        public sealed override bool IsNotSaved => true;
        
        public override Domain DefaultDomain => Domain.Globals;
        public Transform PromptsHost => View.PromptsHost;
        public ModMgr ModManager { get; private set; }
        
        ModService ModService { get; set; }
        VModManagerUI View { get; set; }

        protected override void OnFullyInitialized() {
            ModService = Services.Get<ModService>();
            ModManager = ModService.Manager;
            View = View<VModManagerUI>();
            InitializePrompts();
            InitializeModEntries();
        }

        void InitializeModEntries() {
            for (int i = 0; i < ModManager.OrderedMods.Length; i++) {
                ModHandle modHandle = ModManager.OrderedMods[i];
                var modEntry = new ModEntryUI(i, modHandle);
                AddElement(modEntry);
                World.SpawnView<VModEntryUI>(modEntry, true, true, View.EntriesParent);
            }
        }
        
        void InitializePrompts() {
            var prompts = new Prompts(this);
            AddElement(prompts);
            
            var backPrompt = Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), TryToClose);
            _selectPrompt = Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.Select.Translate(), Prompt.Position.First);
            _modUpPrompt = Prompt.Tap(KeyBindings.UI.Mods.ModUp, LocTerms.MoveUp.Translate(), ChangeModUp);
            _modDownPrompt = Prompt.Tap(KeyBindings.UI.Mods.ModDown, LocTerms.MoveDown.Translate(), ChangeModDown);
            _toggleLeftPrompt = Prompt.Tap(KeyBindings.Gamepad.DPad_Left, LocTerms.SettingsToggle.Translate(), () => _selectedModEntryUI?.ToggleActive(), controllers: ControlSchemeFlag.Gamepad);
            _toggleRightPrompt = Prompt.Tap(KeyBindings.Gamepad.DPad_Right, LocTerms.SettingsToggle.Translate(), () => _selectedModEntryUI?.ToggleActive(), controllers: ControlSchemeFlag.Gamepad);
            var applyChangesPrompt = Prompt.Tap(KeyBindings.UI.Settings.ApplyChanges, LocTerms.SettingsApply.Translate(), ApplyChanges);
            prompts.AddPrompt(_selectPrompt, this);
            prompts.AddPrompt(applyChangesPrompt, this);
            prompts.AddPrompt(_modUpPrompt, this, false, false);
            prompts.AddPrompt(_modDownPrompt, this, false, false);
            prompts.AddPrompt(_toggleLeftPrompt, this, false, false);
            prompts.AddPrompt(_toggleRightPrompt, this, false, false);
            prompts.AddPrompt(backPrompt, this);
        }
        
        void RefreshPrompts() {
            bool isSelected = _selectedModEntryUI != null;
            _selectPrompt.SetupState(!isSelected, !isSelected);
            _modUpPrompt.SetupState(isSelected, isSelected);
            _modDownPrompt.SetupState(isSelected, isSelected);
            _toggleLeftPrompt.SetupState(isSelected, isSelected);
            _toggleRightPrompt.SetupState(isSelected, isSelected);
        }
        
        public void ChangeEntrySelection(ModEntryUI entry) {
            _selectedModEntryUI?.RefreshSelection(false);
            _selectedModEntryUI = entry;
            _selectedModEntryUI?.RefreshSelection(true);
            RefreshPrompts();
        }

        void ChangeModPosition(int direction) {
            if (_selectedModEntryUI == null) {
                return;
            }
            
            int currentIndex = _selectedModEntryUI.Index;
            int siblingCount = ModManager.OrderedMods.Length;
            int newIndex = currentIndex + direction;

            if (newIndex >= 0 && newIndex < siblingCount) {
                foreach (ModEntryUI modEntryUI in Elements<ModEntryUI>()) {
                    int targetIndex = -1;
                    if (modEntryUI.Index == newIndex && modEntryUI != _selectedModEntryUI) {
                        targetIndex = currentIndex;
                    }

                    if (modEntryUI.Index == currentIndex && modEntryUI == _selectedModEntryUI) {
                        targetIndex = newIndex;
                    }

                    if (targetIndex != -1) {
                        modEntryUI.RefreshIndex(targetIndex);
                        ModManager.OrderedMods[targetIndex] = new ModHandle(modEntryUI.ModIndex, modEntryUI.Active);
                    }
                }
                
                View.RecyclableCollectionManager.OrderChangedRefresh();
                RefreshSelection(newIndex).Forget();
            }
        }

        async UniTaskVoid RefreshSelection(int newIndex) {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            
            var entry = Elements<ModEntryUI>().FirstOrDefault(e => e.Index == newIndex);
            ChangeEntrySelection(entry);
            
            ARButton focusTarget = _selectedModEntryUI.View<VModEntryUI>().FocusTarget;
            var recyclableManager = View.RecyclableCollectionManager;
            
            World.Only<Focus>().Select(focusTarget);
            recyclableManager.FocusTarget(_selectedModEntryUI);
            if (!RewiredHelper.IsGamepad) {
                recyclableManager.AutoScroll.SnapToComponent(focusTarget);
            }
        }

        void ChangeModUp() {
            ChangeModPosition(-1);
        }

        void ChangeModDown() {
            ChangeModPosition(1);
        }
        
        void ApplyChanges() {
            ModManager.Refresh();
            ModService.Save();
            Services.Get<TemplatesProvider>().Reload();
            Close();
        }
        
        void TryToClose() {
            bool anyModModified = Elements<ModEntryUI>().Any(e => e.HasBeenModified);
            if (anyModModified) {
                _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                    LocTerms.PopupNotAppliedSettings.Translate(),
                    PopupUI.AcceptTapPrompt(() => {
                        _popup.Discard();
                        ApplyChanges();
                    }).AddAudio(new PromptAudio { TapSound = CommonReferences.Get.AudioConfig.ButtonApplySound }),
                    PopupUI.CancelTapPrompt(Close),
                    LocTerms.PopupNotAppliedSettingsTitle.Translate()
                );
            } else {
                Close();
            }
        }

        void Close() {
            _popup?.Discard();
            _popup = null;
            Discard();
        }
    }
}