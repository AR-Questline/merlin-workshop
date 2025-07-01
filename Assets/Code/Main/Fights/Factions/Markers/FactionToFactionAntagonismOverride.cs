using Awaken.Utility;
using System;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Fights.Factions.Markers {
    [Serializable]
    public partial struct FactionToFactionAntagonismOverride : IEquatable<FactionToFactionAntagonismOverride> {
        public ushort TypeForSerialization => SavedTypes.FactionToFactionAntagonismOverride;

        [Saved] public FactionTemplate TargetFactionTemplate { get; private set; }
        [Saved] public Antagonism Antagonism { get; private set; }
        
        public static void UpdateAntagonism(FactionTemplate factionTemplate, FactionTemplate targetFactionTemplate, Antagonism antagonism) {
            FactionService factionService = World.Services.Get<FactionService>();
            var newOverride = new FactionToFactionAntagonismOverride(targetFactionTemplate, antagonism);
            factionService.AddAntagonismOverride(factionTemplate, newOverride);
        }

        FactionToFactionAntagonismOverride(FactionTemplate targetFactionTemplate, Antagonism antagonism) {
            TargetFactionTemplate = targetFactionTemplate;
            Antagonism = antagonism;
        }

        public bool Equals(FactionToFactionAntagonismOverride other) {
            return TargetFactionTemplate == other.TargetFactionTemplate && Antagonism == other.Antagonism;
        }
    }
}