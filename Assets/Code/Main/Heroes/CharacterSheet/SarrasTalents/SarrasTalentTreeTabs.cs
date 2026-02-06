using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs;

namespace Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents {
    public partial class SarrasTalentTreeTabs : TreeTabsBase<VSarrasTalentOverviewUI, VSarrasTalentTreeTabs> {
        public override VCTabButtonBase<VSarrasTalentOverviewUI, VSarrasTalentTreeTabs> GetCurrentVCButton() {
            return CurrentTabButton as VCSarrasTalentTreeTabButton;
        }
        
        protected override void ChangeTab(TreeTabTypeBase<VSarrasTalentOverviewUI, VSarrasTalentTreeTabs> type) {
            ParentModel.BackFromSubTree();
            base.ChangeTab(type);
        }
    }
}