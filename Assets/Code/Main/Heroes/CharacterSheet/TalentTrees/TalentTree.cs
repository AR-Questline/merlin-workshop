using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees {
    public partial class TalentTree : TalentTreeTabs.TabWithoutView {
        public static bool IsUpgradeAvailable => World.Any<FireplaceUI>();
        
        TalentTreeTemplate Tree { get; }
        Hero Hero => ParentModel.Hero;
        HeroTalents Talents => Hero.Talents;
        TalentTable TalentTable => Talents.TableOf(Tree);
        
        public TalentTree(TalentTreeTemplate tree) {
            Tree = tree;
        }

        protected override void OnInitialize() {
            ParentModel.FillTree(TalentTable);
        }
    }
}