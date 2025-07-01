using System.Collections.Generic;
using Awaken.TG.Main.Locations.Setup;

namespace Awaken.TG.Main.Locations.Spawners.Arena {
    public interface IArenaSpawnerEntry {
        string PersistentId { get; }
        string Label { get; }
        string FactionName { get; }
        float ThreatLevel { get; }
        public IEnumerable<LocationTemplate> LocationTemplates { get; }
    }
}