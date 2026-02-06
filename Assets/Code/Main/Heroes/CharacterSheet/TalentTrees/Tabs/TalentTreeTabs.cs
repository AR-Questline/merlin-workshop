namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs {
    public partial class TalentTreeTabs : TreeTabsBase<VTalentOverviewUI, VTalentTreeTabs> {
        public override VCTabButtonBase<VTalentOverviewUI, VTalentTreeTabs> GetCurrentVCButton() {
            return CurrentTabButton as VCTalentTreeTabButton;
        }
        
        protected override void ChangeTab(TreeTabTypeBase<VTalentOverviewUI, VTalentTreeTabs> type) {
            ParentModel.BackFromSubTree();
            base.ChangeTab(type);
        }
    }
}