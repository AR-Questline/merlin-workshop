using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Maths;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.Locations.Spawners {
    public partial class RandomizedLocationSpawning : Element<GroupLocationSpawner>, ILocationSpawningMethod {
        public sealed override bool IsNotSaved => true;

        readonly SpawnerRandomizationSettings _settings;
        readonly int _totalSpawnCap;
        readonly List<SpawnerRandomizationSettings.LocationTemplateRandomSpawn> _spawnCandidates;
        GroupLocationSpawner Spawner => ParentModel;

        Vector3 _batchSpawnPoint;
        public RandomizedLocationSpawning(SpawnerRandomizationSettings settings, byte totalSpawnCap) {
            _settings = settings;
            _totalSpawnCap = totalSpawnCap;
            _spawnCandidates = new(_settings.RandomLocationsToSpawn.Count());
            
            int id = 0;
            foreach (var locationTemplateRandomSpawn in _settings.RandomLocationsToSpawn) {
                locationTemplateRandomSpawn.id = id++;
            }
        }
        
        public void Spawn(int currentBatchQuantitySpawned) {
            if (currentBatchQuantitySpawned == 0) {
                _batchSpawnPoint = (Random.insideUnitCircle * (_settings.spawnRadius - _settings.GroupSpawnRadius)).X0Y() + _settings.spawnOffsetFromSpawner;
            }
            
            bool anySpawned = false;
            
            foreach (var toSpawn in _settings.RandomLocationsToSpawn) {
                if (ShouldSpawn(toSpawn)) {
                    Spawn(toSpawn);
                    anySpawned = true;
                    break;
                }
            }

            if (!anySpawned && _settings.shouldAlwaysSpawnSuccessfully && _spawnCandidates.Count > 0) {
                var selected = RandomUtil.WeightedSelect(_spawnCandidates, c => c.spawnChancePerInterval);
                Spawn(selected);
            }
            _spawnCandidates.Clear();
        }

        bool ShouldSpawn(SpawnerRandomizationSettings.LocationTemplateRandomSpawn target) {
            int spawnedCount = CurrentlySpawnedByID(target.id);
            if (spawnedCount >= target.spawnCap) return false;
            
            bool result = Random.value <= target.spawnChancePerInterval;
            
            if (!result) {
                _spawnCandidates.Add(target);
            }
            return result;
        }

        int CurrentlySpawnedByID(int id) => Spawner.CurrentlySpawnedByID(id) + Spawner.KilledLocationCount(id);

        void Spawn(SpawnerRandomizationSettings.LocationTemplateRandomSpawn target) {
            Vector3 position = _batchSpawnPoint + (Random.insideUnitCircle * _settings.GroupSpawnRadius).X0Y();
            Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            var template = ParentModel.IsSpawningWyrdSpawns ? ParentModel.WyrdSpawnTemplate() : target.locationToSpawn.Get<LocationTemplate>();
            Spawner.SpawnLocationWithOffset(template, position, rotation, target.id, !_settings.skipSnapToGround);
            Spawner.RegenerateSpawnCooldown(_settings);
        }

        public int TargetSpawnCount() => _totalSpawnCap;
        
        public IEnumerable<LocationTemplate> GetAllPossibleTemplates() => _spawnCandidates.Select(lts => lts.locationToSpawn.Get<LocationTemplate>());
    }
}