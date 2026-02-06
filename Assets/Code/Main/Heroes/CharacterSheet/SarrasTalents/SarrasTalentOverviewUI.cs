using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.Heroes.CharacterSheet.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Heroes.Development.SarrasPowers;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents {
    public partial class SarrasTalentOverviewUI : TalentOverviewBase<VSarrasTalentOverviewUI, VSarrasTalentTreeTabs> {
        Prompt _promptChooseTreeBranch;
        SarrasHeroTreeBranches _sarrasHeroTreeBranches;
        
        TalentTreeBranchType _currentBranchTypeHovered = TalentTreeBranchType.None;

        public override bool HasHiddenTalentLevels => true;
        public override bool HasUnsavedChanges => Hero.Talents.AnyUnappliedTalentPoints();
        
        public new static class Events {
            public static readonly Event<IModel, TalentTreeBranchType> TalentTreeBranchHovered = new(nameof(TalentTreeBranchHovered));
        }

        protected override void AfterViewSpawned(VSarrasTalentOverviewUI view) {
            _sarrasHeroTreeBranches = World.Only<SarrasHeroTreeBranches>();
            bool trinketCharged = _sarrasHeroTreeBranches.IsFirstCharged;
            
            _promptChooseTreeBranch = Prompt.Tap(KeyBindings.UI.Talents.ChooseTreeBranchAsActive, LocTerms.UITalentTreeChooseAsActive.Translate(), OnTalentTreeBranchChosen);
            CharacterSheetUI.Prompts.AddPrompt(_promptChooseTreeBranch, this, false, trinketCharged);
            
            if (!trinketCharged) {
                World.EventSystem.ListenTo(EventSelector.AnySource, SarrasHeroTreeBranches.Events.FirstChargeCommitted, this, OnFirstChargeCommitted);
            }
            
            World.EventSystem.ListenTo(EventSelector.AnySource, Events.TalentTreeBranchHovered, this, OnTalentTreeBranchHovered);
            base.AfterViewSpawned(view);
        }
        
        public override void Back() {
            if (TalentTreeUI.InCategory) {
                BackFromSubTree();
            } else {
                if (World.Any<SarrasShrineAction>(shrine => shrine.PointsDistributionInProgress)) {
                    ParentModel.ParentModel.Discard();
                    return;
                }
                
                ParentModel.Element<CharacterSubTabs>().SetNone();
            }
        }

        public override void CreateTabs() {
            AddElement(new SarrasTalentTreeTabs());
        }
        public static bool IsViewAvailable() => World.Only<SarrasHeroTreeBranches>().IsUnlocked && !World.HasAny<FireplaceUI>();
        
        void OnTalentTreeBranchHovered(TalentTreeBranchType branchType) {
            _currentBranchTypeHovered = branchType;
            if (_currentBranchTypeHovered == TalentTreeBranchType.None) {
                _promptChooseTreeBranch.SetupState(false, false);
                return;
            }
            
            bool isVisible = branchType != _sarrasHeroTreeBranches.CurrentlySelected;
            _promptChooseTreeBranch.SetupState(isVisible, isVisible);
        }
        
        void OnTalentTreeBranchChosen() {
            if (_currentBranchTypeHovered == TalentTreeBranchType.None) {
                return;
            }
            
            _promptChooseTreeBranch.SetupState(false, false);
            _sarrasHeroTreeBranches.SelectTalentTreeBranch(_currentBranchTypeHovered);
        }

        void OnFirstChargeCommitted() {
            _promptChooseTreeBranch.SetVisible(true);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            // set default branch if none is chosen
            if (_sarrasHeroTreeBranches.IsFirstCharged && _sarrasHeroTreeBranches.CurrentlySelected == TalentTreeBranchType.None) {
                _sarrasHeroTreeBranches.SelectTalentTreeBranch(TalentTreeBranchType.SarrasWarrior);
            }
        }
    }
}