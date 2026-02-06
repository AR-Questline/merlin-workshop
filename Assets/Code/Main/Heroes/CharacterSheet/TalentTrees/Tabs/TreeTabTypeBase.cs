using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs {
    public abstract class TreeTabTypeBase<TView, TTabsView> : TreeTabsBase<TView, TTabsView>.ITabType where TView : View, IVTalentOverview where TTabsView : View {
        public abstract TalentTreeTemplate Tree { get; }
        
        public abstract TalentTreeBase<TView, TTabsView> Spawn(TalentOverviewBase<TView, TTabsView> target);
        public abstract bool IsVisible(TalentOverviewBase<TView, TTabsView> target);
    }
}