using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations.Setup;

namespace Awaken.TG.Main.Locations.Spawners.Arena {
    /// <summary>
    /// Entry for spawning encounters in debug arenas
    /// </summary>
    public class EncounterEntry : IArenaSpawnerEntry {
        public string PersistentId { get; }
        public string Label { get; }
        public string FactionName { get; }
        public float ThreatLevel { get; }

        public IEnumerable<LocationTemplate> LocationTemplates => _encounterData.npcs.Select(n => n.npc.LocationTemplate);

        readonly EncounterData _encounterData;

        public EncounterEntry(EncounterData encounterData) {
            _encounterData = encounterData;
            Label = $"{encounterData.Whereabouts}, enemies: {_encounterData.npcs.Count}";
            FactionName = "Unknown";
            if (_encounterData.npcs.Count > 0) {
                var faction = string.Join(", ", _encounterData.npcs.Select(p => p.npc.NpcTemplate.Faction?.factionName.Translate()).Distinct());
                if (!string.IsNullOrEmpty(faction)) {
                    FactionName = faction;
                }
            }
            PersistentId = Label + string.Join(", ", LocationTemplates.Select(e => e == null ? string.Empty : e.name));
            ThreatLevel = encounterData.DifficultyScore;
        }
    }
}