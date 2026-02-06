using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees {
    public abstract class TalentTreeBase<TView, TTabsView> : TreeTabsBase<TView, TTabsView>.TabWithoutView where TTabsView : View where TView : View, IVTalentOverview {
        Hero Hero => ParentModel.Hero;
        HeroTalents Talents => Hero.Talents;
        TalentTable TalentTable => Talents.TableOf(Tree);
        public TalentTreeTemplate Tree { get; }
        
        public static bool DebugUpgradeAnywhere { get; set; }

        public static bool IsUpgradeAvailable => DebugUpgradeAnywhere ||
            (World.HasAny<FireplaceUI>() && !World.HasAny<SarrasTalentOverviewUI>()) ||
            (World.HasAny<SarrasTalentOverviewUI>() && World.Any<SarrasShrineAction>(shrine => shrine.PointsDistributionInProgress) != null);

        public TalentTreeBase(TalentTreeTemplate tree) {
            Tree = tree;
        }
        
        protected override void OnInitialize() {
            ParentModel.FillTree(TalentTable);
        }
    }
}