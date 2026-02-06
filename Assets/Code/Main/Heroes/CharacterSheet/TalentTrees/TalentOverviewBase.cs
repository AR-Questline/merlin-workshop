using System;
using Awaken.TG.Main.Heroes.CharacterSheet.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees {
    public interface ITalentOverview : IModel {
        public TalentTreeTemplate Tree { get; }
        public TalentTable CurrentTable { get; }
        public bool HasHiddenTalentLevels { get; }
        public void RefreshPromptsActive(Talent talent);
        public void RefreshFeedback(bool state);
    }

    public interface IVTalentOverview : ITabParentView {
        public void UpdateTreeLevel(int level);
        public void SetupRequiredInfo(bool zoomIn);
    }

    public abstract class TalentOverviewBase<TView, TTabsView> : CharacterSubTab<TView>, TreeTabsBase<TView, TTabsView>.ITabParent<TView>, ITalentOverview, IUnsavedChangesPopup where TTabsView : View where TView : View, IVTalentOverview, ITabParentView {
        Prompt _promptAcquire;
        Prompt _promptReset;
        Prompt _promptConfirm;
        
        public virtual bool HasHiddenTalentLevels => false;
        public TreeTabTypeBase<TView, TTabsView> CurrentType { get; set; }
        public Tabs<TalentOverviewBase<TView, TTabsView>, TTabsView, TreeTabTypeBase<TView, TTabsView>, TalentTreeBase<TView, TTabsView>> TabsController { get; set; }
        public abstract bool HasUnsavedChanges { get; }
        public VCTabButtonBase<TView, TTabsView> CurrentTabButton => TalentTreeTabs.GetCurrentVCButton();
        public TalentTreeTemplate Tree => CurrentType.Tree;
        public TalentTable CurrentTable => TalentTreeUI.CurrentTable;
        public Hero Hero => Hero.Current;
        public TalentTreeUI TalentTreeUI => Element<TalentTreeUI>();
        public CharacterSheetUI CharacterSheetUI => ParentModel.ParentModel;
        Talent CurrentTalent { get; set; }
        TreeTabsBase<TView, TTabsView> TalentTreeTabs => Element<TreeTabsBase<TView, TTabsView>>();
        TView View => View<TView>();
        
        protected override void AfterViewSpawned(TView view) {
            CharacterSheetUI.SetHeroOnRenderVisible(false);
            World.EventSystem.ListenTo(EventSelector.AnySource, Talent.Events.TalentChanged, this, OnTalentChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, TalentTreeUI.Events.TreeZoomedIn, this, RefreshPromptsVisible);

            if (TalentTree.IsUpgradeAvailable) {
                _promptAcquire = Prompt.Tap(KeyBindings.UI.Talents.AcquireTalent, LocTerms.UITalentsAcquire.Translate(), Acquire);
                _promptReset = Prompt.Tap(KeyBindings.UI.Talents.ResetTalent, LocTerms.UITalentsReset.Translate(), Reset).AddAudio();
                _promptConfirm = Prompt.Tap(KeyBindings.UI.Talents.ConfirmTalents, LocTerms.Confirm.Translate(), Confirm).AddAudio();
                CharacterSheetUI.Prompts.AddPrompt(_promptAcquire, this, false, false);
                CharacterSheetUI.Prompts.AddPrompt(_promptReset, this, false, false);
                CharacterSheetUI.Prompts.AddPrompt(_promptConfirm, this, false);
            }

            AddElement(new TalentTreeUI());
            CreateTabs();
            
            UpdateTreeLevel();
        }

        public abstract void CreateTabs();
        
        public void RefreshFeedback(bool state) {
            CurrentTabButton?.RefreshFeedback(state);
        }
        
        public void FillTree(TalentTable table) {
            TalentTreeUI.Fill(table);
        }
        
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
        
        public void ShowUnsavedPopup(Action continueCallback) {
            if (HasUnsavedChanges && !TalentTreeUI.InCategory) {
                PopupUIFactory.CreateUnsavedChangesPopup(LocTerms.UIGenericUnsavedChangesPopup.Translate(), continueCallback, Hero.Talents.ApplyTemporaryLevels, Hero.Talents.ClearTemporaryPoints, null);
            } else if (TalentTreeUI.InCategory) {
                BackFromSubTree();   
            } else {
                continueCallback.Invoke();
            }
        }
        
        public void UpdateTreeLevel() {
            View.UpdateTreeLevel(CurrentTable.CurrentTreeLevel);
        }

        void OnTalentChanged(Talent talent) {
            UpdateTreeLevel();

            if (CurrentTalent == talent) {
                RefreshPromptsActive();
            }
        }

        void Acquire() {
            CurrentTalent?.AcquireNextTemporaryLevel();
        }

        void Reset() {
            CurrentTalent?.DecrementTemporaryLevel();
        }
        
        void Confirm() {
            if (HasUnsavedChanges) {
                PopupUIFactory.ConfirmPopup(LocTerms.UIGenericConfirmChangesPopup.Translate(), ConfirmPoints, null);
            }

            void ConfirmPoints() {
                Hero.Talents.ApplyTemporaryLevels();
                RefreshPromptsActive();
            }
        }

        public void RefreshPromptsActive(Talent talent) {
            CurrentTalent = talent;

            if (TalentTree.IsUpgradeAvailable) {
                RefreshPromptsActive();
            }
        }

        void RefreshPromptsActive() {
            _promptAcquire.SetActive(CurrentTalent is { CanBeUpgraded: true });
            _promptReset.SetActive(CurrentTalent is { CanBeReset: true } && TalentTreeUI.NotLockedByChildren(CurrentTalent));
            _promptConfirm.SetActive(HasUnsavedChanges);
        }
        
        void RefreshPromptsVisible(bool visible) {
            if (TalentTree.IsUpgradeAvailable) {
                _promptAcquire.SetVisible(visible);
                _promptReset.SetVisible(visible);
            }
        }
    }
}