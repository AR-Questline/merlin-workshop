using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs {
    public abstract class TreeTabsBase<TView, TTabsView> : Tabs<TalentOverviewBase<TView, TTabsView>, TTabsView, TreeTabTypeBase<TView, TTabsView>, TalentTreeBase<TView, TTabsView>> where TTabsView : View where TView : View, IVTalentOverview {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.PreviousAlt;
        protected override KeyBindings Next => KeyBindings.UI.Generic.NextAlt;
        public abstract VCTabButtonBase<TView, TTabsView> GetCurrentVCButton();
    }
}