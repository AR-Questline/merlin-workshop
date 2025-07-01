using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs {
    public partial class TalentTreeTabs : Tabs<TalentOverviewUI, VTalentTreeTabs, TalentTreeTabType, TalentTree> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.PreviousAlt;
        protected override KeyBindings Next => KeyBindings.UI.Generic.NextAlt;

        protected override void ChangeTab(TalentTreeTabType type) {
            ParentModel.BackFromSubTree();
            base.ChangeTab(type);
        }
        
        public VCTalentTreeTabButton GetCurrentVCButton() {
            return CurrentTabButton as VCTalentTreeTabButton;
        }
    }
}