using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Setup;

namespace Awaken.TG.Main.Locations.Spawners.Arena {
    public class LocationEntry : IArenaSpawnerEntry {
        public string PersistentId => _locationTemplate.name;
        public string Label { get; }
        public string FactionName { get; }
        public float ThreatLevel { get; }
        public IEnumerable<LocationTemplate> LocationTemplates => new List<LocationTemplate> { _locationTemplate };

        LocationTemplate _locationTemplate;

        public LocationEntry(LocationTemplate locationTemplate) {
            _locationTemplate = locationTemplate;
            ThreatLevel = -1;
            FactionName = "Unknown";
            var npc = _locationTemplate.GetComponent<NpcAttachment>();
            if (npc && npc.NpcTemplate) {
                ThreatLevel = npc.NpcTemplate.DifficultyScore;
                string factionName = npc.NpcTemplate.Faction?.factionName.Translate();
                if (!string.IsNullOrEmpty(factionName)) {
                    FactionName = factionName;
                }
            }
            Label = _locationTemplate.name;
        }
    }
}