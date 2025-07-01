using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterReputation {
    public partial class FactionEntryUI : Element<CharacterReputationUI> {
        public sealed override bool IsNotSaved => true;

        readonly ReputationInfo _reputationInfo;
        
        public readonly Faction faction; 
        public readonly int famePoints;
        public readonly int infamyPoints;
        public readonly int maxReputationPoints;
        public readonly string factionEffects;
        
        public string ReputationName => _reputationInfo.name;
        public string ReputationDescription => _reputationInfo.description;
        [UnityEngine.Scripting.Preserve] public ReputationKind ReputationKind => _reputationInfo.reputationKind;

        public new static class Events {
            public static readonly Event<FactionEntryUI, FactionEntryUI> FactionChanged = new(nameof(FactionChanged));
        }
        
        public FactionEntryUI(Faction faction) {
            this.faction = faction;
            //_reputationInfo = FactionReputationUtil.GetCurrentReputationInfo(faction.Template);
            famePoints = OwnerReputationUtil.GetReputationPoints(faction.Template.GUID, ReputationType.Fame);
            infamyPoints = OwnerReputationUtil.GetReputationPoints(faction.Template.GUID, ReputationType.Infamy);
            //maxReputationPoints = this.faction.Template.MaxReputation;
            //factionEffects = FactionReputationUtil.GetFactionEffectsDescription(faction.Template, _reputationInfo.reputationKind);
        }
    }
}