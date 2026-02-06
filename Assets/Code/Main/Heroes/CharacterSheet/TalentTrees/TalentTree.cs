using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs;
using Awaken.TG.Main.Heroes.Development.Talents;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees {
    public partial class TalentTree : TalentTreeBase<VTalentOverviewUI, VTalentTreeTabs> {
        public TalentTree(TalentTreeTemplate tree) : base(tree) { }
    }
}