using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Fights.Factions.Markers {
    public partial class FactionAntagonism : AntagonismMarker, IEquatable<FactionAntagonism> {
        public sealed override bool IsNotSaved => true; // TODO: Remove this when fix entering dungeon while in combat

        /*[Saved]*/ public Faction Faction { get; private set; }

        [JsonConstructor, UnityEngine.Scripting.Preserve] FactionAntagonism() { }
        public FactionAntagonism(AntagonismLayer layer, AntagonismType type, Faction faction, Antagonism antagonism) : base(layer, type, antagonism) {
            Faction = faction;
        }

        protected override bool RefersTo(IWithFaction withFaction) {
            return Faction == withFaction.Faction;
        }
        
        public bool Equals(FactionAntagonism other) {
            return Faction == other?.Faction && base.Equals(other);
        }
    }
}