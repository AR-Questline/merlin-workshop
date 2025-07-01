using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners {
    public sealed partial class LocationSpawner : BaseLocationSpawner, IRefreshedByAttachment<LocationSpawnerAttachment> {
        public override ushort TypeForSerialization => SavedModels.LocationSpawner;

        LocationTemplate[] _locationsToSpawn;
        float _spawnerRange;
        bool _snapToGroundOnSpawn;
        bool _requireFullClearForRespawn;

        float RandomFloat => RandomUtil.UniformFloat(-_spawnerRange, _spawnerRange);
        protected override IEnumerable<LocationTemplate> AllUniqueTemplates => _locationsToSpawn;

        public void InitFromAttachment(LocationSpawnerAttachment spec, bool isRestored) {
            DiscardSpawnedLocationsOnDiscard = spec.discardSpawnedLocationsOnDiscard;
            _locationsToSpawn = spec.LocationsToSpawn.ToArray();
            _batchQuantityToSpawn = spec.spawnAmount;
            _spawnerRange = spec.spawnerRange;
            DiscardAfterSpawn = spec.discardAfterSpawn;
            DiscardAfterAllKilled = spec.discardAfterAllKilled;
            _snapToGroundOnSpawn = spec.snapToGroundOnSpawn;
            SpawnOnlyAtNight = spec.spawnOnlyAtNight;
            IsDisabledByFlag = spec.useFlagAvailability;
            _availability = spec.availability;
            _storyOnAllKilled = spec.storyOnAllKilled;
            _requireFullClearForRespawn = spec.mustFullClearToRespawn;
            SpawnCooldownAfterKilled = spec.SpawnerCooldown;

            _isManualSpawner = spec.manualSpawner;
            _canTriggerAmbush = spec.CanTriggerAmbush;
            _spawnOnlyOnAmbush = spec.spawnOnlyOnAmbush;
            
            _partialCanSpawnWyrdSpawns = !_isManualSpawner && !SpawnOnlyAtNight && !IsDisabledByFlag;
        }

        protected override bool ShouldSpawn() => _requireFullClearForRespawn && !IsSpawning ? CurrentlySpawned == 0 : CurrentlySpawned < _batchQuantityToSpawn; // We only have one batch here
        
        protected override void SpawnPrefabInternal(int currentBatchQuantitySpawned) {
            MainView.transform.GetPositionAndRotation(out var position, out var rotation);
            position += new Vector3(RandomFloat, 0, RandomFloat);
            
            if (_locationsToSpawn.Length > 0) {
                LocationTemplate templateToSpawn = IsSpawningWyrdSpawns ? WyrdSpawnTemplate() : RandomUtil.UniformSelect(_locationsToSpawn);
                position = VerifyPosition(position, templateToSpawn, _snapToGroundOnSpawn);
                Location location = templateToSpawn.SpawnLocation(position, rotation, spawnScene: MainView.gameObject.scene);
                RepetitiveNpcUtils.Check(location);
                OnLocationSpawned(location, -1);
            }
        }
    }
}