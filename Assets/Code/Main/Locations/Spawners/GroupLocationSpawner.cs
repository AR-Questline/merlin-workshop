using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.Locations.Spawners {
    public sealed partial class GroupLocationSpawner : BaseLocationSpawner, IRefreshedByAttachment<GroupSpawnerAttachment> {
        public override ushort TypeForSerialization => SavedModels.GroupLocationSpawner;

        ILocationSpawningMethod _spawningMethod;
        GroupSpawnerAttachment _spec;
        bool _requireFullClearForRespawn;
        HashSet<LocationTemplate> _allUniqueTemplates;

        protected override IEnumerable<LocationTemplate> AllUniqueTemplates {
            get {
                if (_allUniqueTemplates == null) {
                    var locationSpawningMethods = Elements<ILocationSpawningMethod>();
                    _allUniqueTemplates = new HashSet<LocationTemplate>(16, new LocationTemplateByGuidComparer());
                    foreach (var locationSpawningMethod in locationSpawningMethods) {
                        _allUniqueTemplates.AddRange(locationSpawningMethod.GetAllPossibleTemplates());
                    }
                }
                return _allUniqueTemplates;
            }
        }

        public void InitFromAttachment(GroupSpawnerAttachment spec, bool isRestored) {
            DiscardSpawnedLocationsOnDiscard = spec.discardSpawnedLocationsOnDiscard;
            DiscardAfterSpawn = spec.discardAfterSpawn;
            DiscardAfterAllKilled = spec.discardAfterAllKilled;
            SpawnOnlyAtNight = spec.spawnOnlyAtNight;
            IsDisabledByFlag = spec.useFlagAvailability;
            _availability = spec.availability;
            _storyOnAllKilled = spec.storyOnAllKilled;
            _requireFullClearForRespawn = spec.mustFullClearToRespawn;
            _isManualSpawner = spec.manualSpawner;
            _canTriggerAmbush = spec.CanTriggerAmbush;
            _spawnOnlyOnAmbush = spec.spawnOnlyOnAmbush;
            SpawnCooldownAfterKilled = spec.SpawnerCooldown;
            _partialCanSpawnWyrdSpawns = !_isManualSpawner && !SpawnOnlyAtNight && !IsDisabledByFlag;
            this._spec = spec;
        }
        
        protected override void OnInitialize() {
            InitElements();
            base.OnInitialize();
        }
        
        protected override void OnRestore() {
            InitElements();
            base.OnRestore();
        }

        void InitElements() {
            if (_spec.RandomizationSettings == null) {
                _spawningMethod = AddElement(new RegularLocationSpawning(_spec.LocationsToSpawn.ToList()));
                _batchQuantityToSpawn = _spec.LocationsToSpawn.Count();
            } else {
                RegenerateSpawnCooldown(_spec.RandomizationSettings);
                _batchQuantityToSpawn = _spec.RandomizationSettings.groupSpawnCap;
                _spawningMethod = AddElement(new RandomizedLocationSpawning(_spec.RandomizationSettings));
            }

            _spec = null;
        }

        public void RegenerateSpawnCooldown(SpawnerRandomizationSettings settings) {
            SpawnCooldown = settings.spawnInterval * (1 + Random.Range(-settings.spawnIntervalVariance, settings.spawnIntervalVariance));
        }

        protected override bool ShouldSpawn() => _requireFullClearForRespawn && !IsSpawning ? CurrentlySpawned == 0 : CurrentlySpawned < _spawningMethod.TargetSpawnCount();

        protected override void SpawnPrefabInternal(int currentBatchQuantitySpawned) {
            _spawningMethod.Spawn(currentBatchQuantitySpawned);
        }

        public void SpawnLocationWithOffset(LocationTemplate toSpawn, Vector3 positionOffset, Quaternion rotationOffset, int id) {
            if (toSpawn == null) {
                Debug.LogError("GroupLocationSpawner: LocationToSpawn is null!", MainView?.gameObject);
                return;
            }
            
            Vector3 position = MainView.transform.TransformPoint(positionOffset);
            position = VerifyPosition(position, toSpawn);
            
            Quaternion rotation = MainView.transform.rotation * rotationOffset;
            Location location = toSpawn.SpawnLocation(position, rotation, spawnScene: MainView.gameObject.scene);
            RepetitiveNpcUtils.Check(location);
            OnLocationSpawned(location, id);
        }
        
        class LocationTemplateByGuidComparer : IEqualityComparer<LocationTemplate> {
            public bool Equals(LocationTemplate x, LocationTemplate y) {
                return (x == null && y == null) || (x != null && y != null && x.GUID.Equals(y.GUID));
            }

            public int GetHashCode(LocationTemplate obj) {
                return obj.GUID.GetHashCode();
            }
        }
    }
}