using Awaken.TG.MVC;

namespace Awaken.TG.Main.Fights.Factions {
    public interface IWithFaction : IModel {
        Faction Faction { get; }
        FactionTemplate GetFactionTemplateForSummon();
        void OverrideFaction(FactionTemplate faction, FactionOverrideContext context = FactionOverrideContext.Default);
        void ResetFactionOverride(FactionOverrideContext context = FactionOverrideContext.Default);
    }
}