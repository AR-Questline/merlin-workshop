using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners {
    public sealed partial class HideSpotLocationSpawner : BaseLocationSpawner, IRefreshedByAttachment<HideSpotLocationSpawnerAttachment> {
        public override ushort TypeForSerialization => SavedModels.HideSpotLocationSpawner;

        [Saved] bool _hideSpotOccupied = true;
        [Saved] WeakModelRef<Location> _spawnedPickableActivator;

        HideSpotLocationSpawnerAttachment _spec;
        GameObject _visualSpawner;
        bool _spawnerInVisualBand;
        bool _spawnedLocationInVisualBand;
        bool _locationSpawning;
        
        protected override IEnumerable<LocationTemplate> AllUniqueTemplates => new [] { _spec.LocationToSpawn };
        Location OwnSpawnedLocation => spawnedLocations.FirstOrDefault().location.Get();
        bool PickableActivatorCollected => !_spawnedPickableActivator.Exists();
        
        protected override bool ShouldSpawn() => CurrentlySpawned == 0 && !IsSpawning && _hideSpotOccupied;
        
        public void InitFromAttachment(HideSpotLocationSpawnerAttachment spec, bool isRestored) {
            _batchQuantityToSpawn = 1;
            SpawnOnlyAtNight = false;
            DiscardAfterSpawn = false;
            DiscardAfterAllKilled = true;
            _storyOnAllKilled = spec.storyOnKilled;
            _spec = spec;
            _isManualSpawner = true;
            _partialCanSpawnWyrdSpawns = false;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            CreatePickableActivator();
        }
        
        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            InitializePickableActivator();
            ParentModel.OnVisualLoaded(AfterVisualFullyLoaded);
        }
        
        void AfterVisualFullyLoaded(Transform transform) {
            _visualSpawner = transform.gameObject;
            if (_hideSpotOccupied || CurrentlySpawned == 0) {
                ShowSpawner();
            } else {
                FinalizeLocationSpawn(true);
            }
            
            ParentModel.GetOrCreateTimeDependent().WithUpdate(ProcessUpdate);
            ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, 
                RefreshSpawnerDistanceBand, ParentModel);
            RefreshSpawnerDistanceBand(ParentModel.GetCurrentBandSafe(100));
        }
        
        void RefreshSpawnerDistanceBand(int band) {
            bool previousVisualBandState = _spawnerInVisualBand;
            _spawnerInVisualBand = LocationCullingGroup.InNpcVisibilityBand(band);
            if (previousVisualBandState && !_spawnerInVisualBand) {
                TryHideLocationInHideSpot();
            }
        }
        
        void CreatePickableActivator() {
            var pickableActivatorTemplate = _spec.PickableActivatorTemplate;
            if (pickableActivatorTemplate) {
                ParentModel.MainView.transform.GetPositionAndRotation(out var position, out var rotation);
                position = VerifyPosition(position, pickableActivatorTemplate);
            
                _spawnedPickableActivator = pickableActivatorTemplate.SpawnLocation(position, rotation, spawnScene: ParentModel.MainView.gameObject.scene);
            }
        }

        void InitializePickableActivator() {
            if (_spawnedPickableActivator.Exists()) {
                _spawnedPickableActivator.Get().ListenTo(PickItemAction.Events.ItemPicked, OnActivatorPicked, this);
            }
        }
        
        void OnActivatorPicked(PickItemAction.ItemPickedData data) {
            StartLocationSpawn();
        }
        
        void ProcessUpdate(float deltaTime) {
            if (ParentModel == null || ParentModel.HasBeenDiscarded) {
                return;
            }

            HandleProximityFallback();
        }

        void HandleProximityFallback() {
            if (!PickableActivatorCollected || !ShouldSpawn()) {
                return;
            }

            bool isCloseEnough = (ParentModel.Coords - Hero.Current.Coords).magnitude <= _spec.fallbackActivationDistance;
            
            if (isCloseEnough) {
                StartLocationSpawn();
            }
        }

        void StartLocationSpawn() {
            if (_locationSpawning || !ShouldSpawn()) {
                return;
            }

            _locationSpawning = true;
            SpawnPrefab().Forget();
        }
        
        protected override void SpawnPrefabInternal(int currentBatchQuantitySpawned) {
            var locationToSpawn = _spec.LocationToSpawn;

            if (!locationToSpawn) {
                Log.Minor?.Error("HideSpotLocationSpawner has no location set to spawn!");
                return;
            }
            
            ParentModel.MainView.transform.GetPositionAndRotation(out var position, out var rotation);
            position = VerifyPosition(position, locationToSpawn);
            
            Location location = locationToSpawn.SpawnLocation(position, rotation, spawnScene: ParentModel.MainView.gameObject.scene);
            location.ViewParent.localScale = Vector3.zero;
            
            RepetitiveNpcUtils.Check(location);
            OnLocationSpawned(location, -1);
            OnLocationSpawningInProgress();
            
            _hideSpotOccupied = false;
        }

        void OnLocationSpawningInProgress() {
            SetSpawnedLocationVisibility(false);
            
            if (OwnSpawnedLocation.TryGetElement(out NpcElement npc)) {
                npc.StartInSpawn = true;
                npc.ListenToLimited(NpcElement.Events.NpcSpawning, () => FinalizeLocationSpawn(false), this);
            } else {
                FinalizeLocationSpawn(false);
            }
        }

        void FinalizeLocationSpawn(bool onRestore) {
            if (!onRestore) {
                SetSpawnedLocationVisibility(true);
                CreateAndReturnSpawnVFX();
            }
            
            OwnSpawnedLocation.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, 
                RefreshLocationDistanceBand, OwnSpawnedLocation);
            _spawnedLocationInVisualBand = true;

            HideSpawner();
            _locationSpawning = false;
        }
        
        void SetSpawnedLocationVisibility(bool visible) {
            if (OwnSpawnedLocation) {
                var nearZeroScale = Vector3.one * 0.01f;
                OwnSpawnedLocation.ViewParent.localScale = visible ? Vector3.one : nearZeroScale;
            }
        }
        
        void RefreshLocationDistanceBand(int band) {
            bool previousVisualBandState = _spawnedLocationInVisualBand;
            _spawnedLocationInVisualBand = LocationCullingGroup.InNpcVisibilityBand(band);
            if (previousVisualBandState && !_spawnedLocationInVisualBand) {
                TryHideLocationInHideSpot();
            }
        }

        void CreateAndReturnSpawnVFX() {
            if (_spec.spawnVFX.IsSet) {
                var position = Ground.SnapToGround(ParentModel.Coords);
                PrefabPool.InstantiateAndReturn(_spec.spawnVFX, position, ParentModel.Rotation, _spec.vfxDuration).Forget();
            }
        }

        void TryHideLocationInHideSpot() {
            if (ShouldHideLocationInHideSpot()) {
                OwnSpawnedLocation?.Discard();
                ShowSpawner();
            }
        }
        bool ShouldHideLocationInHideSpot() {
            if (!_hideSpotOccupied && !_spawnerInVisualBand && !_spawnedLocationInVisualBand) {
                if (!OwnSpawnedLocation || _spec.hideLocationOutsideVisualBand) {
                    return true;
                }
            }
            return false;
        }
        
        void HideSpawner() {
            _hideSpotOccupied = false;
            _visualSpawner.SetActive(false);
        }
        
        void ShowSpawner() {
            _hideSpotOccupied = true;
            _visualSpawner.SetActive(true);
        }
    }
}