using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Locations.Spawners {
    public partial class RegularLocationSpawning : Element<GroupLocationSpawner>, ILocationSpawningMethod {
        public sealed override bool IsNotSaved => true;

        readonly List<GroupSpawnerAttachment.LocationTemplateWithPosition> _locationsToSpawn;
        GroupLocationSpawner Spawner => ParentModel;

        public RegularLocationSpawning(List<GroupSpawnerAttachment.LocationTemplateWithPosition> locationsToSpawn) {
            _locationsToSpawn = locationsToSpawn;
        }
        
        public void Spawn(int currentBatchQuantitySpawned) {
            foreach (var toSpawn in _locationsToSpawn) {
                // --- don't spawn locations that has been already spawned
                if (Spawner.CurrentlySpawnedByID(toSpawn.id) > 0) {
                    continue;
                }

                var template = ParentModel.IsSpawningWyrdSpawns ? ParentModel.WyrdSpawnTemplate() : toSpawn.locationToSpawn.Get<LocationTemplate>();
                Spawner.SpawnLocationWithOffset(template, toSpawn.locationMatrix.ExtractPosition(), toSpawn.locationMatrix.ExtractRotation(), toSpawn.id);
            }
        }

        public int TargetSpawnCount() => _locationsToSpawn.Count;
        
        public IEnumerable<LocationTemplate> GetAllPossibleTemplates() => _locationsToSpawn.Select(lts => lts.locationToSpawn.Get<LocationTemplate>());
    }
}