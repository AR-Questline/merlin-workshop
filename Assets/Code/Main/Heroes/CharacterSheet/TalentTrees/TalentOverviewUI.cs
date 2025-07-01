using System;
using Awaken.TG.Main.Heroes.CharacterSheet.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees {
    public partial class TalentOverviewUI : CharacterSubTab<VTalentOverviewUI>, TalentTreeTabs.ITabParent<VTalentOverviewUI>, IUnsavedChangesPopup {
        public TalentTreeTabType CurrentType { get; set; }
        public Tabs<TalentOverviewUI, VTalentTreeTabs, TalentTreeTabType, TalentTree> TabsController { get; set; }
        public Hero Hero => Hero.Current;
        public TalentTable CurrentTable => TalentTreeUI.CurrentTable;
        public bool HasUnsavedChanges => Hero.Talents.AnyUnappliedTalentPoints();
        public VCTalentTreeTabButton CurrentTabButton => TalentTreeTabs.GetCurrentVCButton();

        CharacterSheetUI CharacterSheetUI => ParentModel.ParentModel;
        Talent CurrentTalent { get; set; }
        TalentTreeUI TalentTreeUI => Element<TalentTreeUI>();
        TalentTreeTabs TalentTreeTabs => Element<TalentTreeTabs>();
        VTalentOverviewUI View => View<VTalentOverviewUI>();

        Prompt _promptAcquire;
        Prompt _promptReset;
        Prompt _promptConfirm;

        protected override void AfterViewSpawned(VTalentOverviewUI view) {
            CharacterSheetUI.SetHeroOnRenderVisible(false);
            World.EventSystem.ListenTo(EventSelector.AnySource, Talent.Events.TalentChanged, this, OnTalentChanged);
            
            if (TalentTree.IsUpgradeAvailable) {
                _promptAcquire = Prompt.Tap(KeyBindings.UI.Talents.AcquireTalent, LocTerms.UITalentsAcquire.Translate(), Acquire);
                _promptReset = Prompt.Tap(KeyBindings.UI.Talents.ResetTalent, LocTerms.UITalentsReset.Translate(), Reset).AddAudio();
                _promptConfirm = Prompt.Tap(KeyBindings.UI.Talents.ConfirmTalents, LocTerms.Confirm.Translate(), Confirm).AddAudio();
                CharacterSheetUI.Prompts.AddPrompt(_promptAcquire, this, false);
                CharacterSheetUI.Prompts.AddPrompt(_promptReset, this, false);
                CharacterSheetUI.Prompts.AddPrompt(_promptConfirm, this, false);
            }

            AddElement(new TalentTreeUI());
            AddElement(new TalentTreeTabs());
            
            UpdateTreeLevel();
        }

        public static bool IsViewAvailable() => !World.Services.Get<SceneService>().IsPrologue || Hero.Current.HeroItems.HasItem(CommonReferences.Get.Bonfire.ToRuntimeData(Hero.Current));

        public override void Back() {
            if (TalentTreeUI.InCategory) {
                BackFromSubTree();
            } else {
                ParentModel.Element<CharacterSubTabs>().SetNone();
            }
        }
        
        public void BackFromSubTree() { 
            TalentTreeUI.Back();
        }
        
        public void FillTree(TalentTable table) {
            TalentTreeUI.Fill(table);
        }

        void Confirm() {
            if (HasUnsavedChanges) {
                PopupUIFactory.ConfirmPopup(LocTerms.UIGenericConfirmChangesPopup.Translate(), Hero.Talents.ApplyTemporaryLevels, null);
            }
        }

        public void UpdateTreeLevel() {
            View.UpdateTreeLevel(CurrentTable.CurrentTreeLevel);
        }

        void OnTalentChanged(Talent talent) {
            UpdateTreeLevel();

            if (CurrentTalent == talent) {
                RefreshPrompts();
            }
        }

        void Acquire() {
            CurrentTalent?.AcquireNextTemporaryLevel();
        }

        void Reset() {
            CurrentTalent?.DecrementTemporaryLevel();
        }

        public void RefreshPrompts(Talent talent) {
            CurrentTalent = talent;

            if (TalentTree.IsUpgradeAvailable) {
                RefreshPrompts();
            }
        }

        void RefreshPrompts() {
            _promptAcquire.SetActive(CurrentTalent is { CanBeUpgraded: true });
            _promptReset.SetActive(CurrentTalent is { CanBeReset: true } && TalentTreeUI.NotLockedByChildren(CurrentTalent));
            _promptConfirm.SetActive(HasUnsavedChanges);
        }

        public void ShowUnsavedPopup(Action continueCallback) {
            if (HasUnsavedChanges && !TalentTreeUI.InCategory) {
                PopupUIFactory.CreateUnsavedChangesPopup(LocTerms.UIGenericUnsavedChangesPopup.Translate(), continueCallback, Hero.Talents.ApplyTemporaryLevels, Hero.Talents.ClearTemporaryPoints, null);
            } else {
                continueCallback.Invoke();
            }
        }
    }
}