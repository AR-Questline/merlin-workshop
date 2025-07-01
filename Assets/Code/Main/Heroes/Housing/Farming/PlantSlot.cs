using System;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Times;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Heroes.Housing.Farming {
    public partial class PlantSlot : Element<Flowerpot> {
        public override ushort TypeForSerialization => SavedModels.PlantSlot;

        [Saved] PlantedSeedData _seed;
        [Saved] GrowingPartData[] _growingParts;
        [Saved] bool _isPlanted;
        [Saved] public PlantSlot parent;
        [Saved] public PlantSlot[] children;
        
        public PlantSize plantSize;
        Transform _markerTransform;
        Vector3 _plantPosition;
        Quaternion _plantRotation;
        int _stageIndex;
        bool _spawningNewPart;
        IEventListener _gameTimeListener;

        public PlantedSeedData PlantedSeedData => _seed;
        public string DisplayState => HousingUtils.GetPlantStateName(State);
        [UnityEngine.Scripting.Preserve] public string DisplaySize => HousingUtils.GetPlantSizeName(plantSize);
        public string PlantName => IsEmpty ? string.Empty : _seed.plantName;
        public bool IsEmpty => State is PlantState.ReadyForPlanting or PlantState.Blocked;
        public ARDateTime PlantedTime => _isPlanted ? _growingParts[0].growingStartTime : default;
        public ARTimeSpan TotalTimeLeft => _isPlanted && GameRealTime != null ? PlantedTime + _seed.totalGrowthTime - GameRealTime.WeatherTime : default;
        
        public PlantState State {
            get {
                if (AnyParentGrowing || AnyChildGrowing) {
                    return PlantState.Blocked;
                }
                
                if (_seed.resultItem == null) {
                    return PlantState.ReadyForPlanting;
                }
                
                if (_stageIndex >= Stages.Length) {
                    return PlantState.FullyGrown;
                }
                
                return _isPlanted ? PlantState.Growing : PlantState.ReadyForPlanting;
            }
        }

        bool AnyParentGrowing {
            get {
                if (parent == null) {
                    return false;
                }

                return parent._isPlanted || parent.AnyParentGrowing;
            }
        }

        bool AnyChildGrowing => children != null && children.Any(t => t._isPlanted || t.AnyChildGrowing);

        PlantStage[] Stages => _seed.stages;
        ItemSpawningData ItemData => _seed.resultItem;
        GameRealTime GameRealTime { get; set; }

        public new static class Events {
            public static readonly Event<PlantSlot, PlantSlot> PlantSlotStateChanged = new(nameof(PlantSlotStateChanged));
            public static readonly Event<PlantSlot, PlantSlot> PlantGrowthTimeChanged = new(nameof(PlantGrowthTimeChanged));
            public static readonly Event<PlantSlot, PlantSlot> FullyGrownPlantHarvested = new(nameof(FullyGrownPlantHarvested));
        }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public PlantSlot() { }

        public PlantSlot(PlantSlotMarkerInteraction marker) {
            plantSize = marker.plantSize;
            _markerTransform = marker.transform;
            _plantPosition = _markerTransform.position;
            _plantRotation = _markerTransform.rotation;
        }

        protected override void OnRestore() {
            if (!_isPlanted) {
                return;
            }

            int stagesCount = Stages.Length;
            if (_growingParts.Length != stagesCount) {
                Array.Resize(ref _growingParts, stagesCount);
            }

            //let the growing process resolve by itself with saved data
            ListenToGameRealTime();
        }

        public void BuildStructure(PlantSlot parentSlot, PlantSlot[] childrenSlots) {
            parent = parentSlot;
            children = childrenSlots;
        }

        public void UpdatePlantSlot(PlantSlotMarkerInteraction marker) {
            _markerTransform = marker.transform;
            _plantPosition = _markerTransform.position;
            _plantRotation = _markerTransform.rotation;
            plantSize = marker.plantSize;

            if (_growingParts == null) {
                return;
            }

            foreach (GrowingPartData asset in _growingParts.Where(r => r.spawnedObject != null)) {
                asset.spawnedObject.transform.position = _plantPosition;
                asset.spawnedObject.transform.rotation = _plantRotation;
            }
        }

        public void Plant(ItemSeed itemSeed) {
            // Logs will be handled better on the UI in the future
            if (itemSeed == null) {
                Log.Important?.Error("Planting null item seed!");
                return;
            }

            if (State == PlantState.Blocked) {
                Log.Important?.Error("Blocked by another slot!");
                return;
            }

            if (itemSeed.plantSize != plantSize) {
                Log.Important?.Error($"Plant size mismatch! Slot: {plantSize}, Seed: {itemSeed.plantSize}");
                return;
            }

            bool sameSeed = State is PlantState.Growing && Equals(_seed.seedTemplate, itemSeed.SeedItem.Template);
            if (sameSeed) {
                return;
            }

            if (State is PlantState.FullyGrown or PlantState.Growing) {
                Harvest();
            }

            _seed = new PlantedSeedData(itemSeed);
            itemSeed.ParentModel.DecrementQuantity();
            PlantInternal();
        }

        void PlantInternal() {
            _stageIndex = 0;
            ListenToGameRealTime();

            _growingParts = new GrowingPartData[Stages.Length];
            _growingParts[_stageIndex] = new GrowingPartData {
                growingStartTime = GameRealTime.WeatherTime
            };

            _isPlanted = true;
            TriggerPlantStateChange();
        }

        public void Harvest() {
            if (State == PlantState.FullyGrown) {
                var regrownItemData = ItemData.ToRuntimeData(this);
                if (Hero.Current.Development.CanGatherAdditionalPlants) {
                    regrownItemData.quantity++;
                }

                regrownItemData.ChangeQuantity(Hero.Current.Inventory);
                this.Trigger(Events.FullyGrownPlantHarvested, this);
            }

            if (State == PlantState.Growing) {
                _seed.seedTemplate.ChangeQuantity(Hero.Current.Inventory, 1);
            }

            _isPlanted = false;
            _stageIndex = 0;
            TriggerPlantStateChange();
            RemoveGameRealTimeListener();
            DespawnRegrowableParts();
            _seed = default;
        }

        public void TriggerPlantStateChange() {
            this.Trigger(Events.PlantSlotStateChanged, this);
        }

        void ListenToGameRealTime() {
            RemoveGameRealTimeListener(); //just in case
            GameRealTime = World.Only<GameRealTime>();
            _gameTimeListener = GameRealTime.ListenTo(GameRealTime.Events.GameTimeChanged, OnGameTimeChanged, this);
        }

        void RemoveGameRealTimeListener() {
            World.EventSystem.TryDisposeListener(ref _gameTimeListener);
            GameRealTime = null;
        }

        void OnGameTimeChanged(ARDateTime weatherTime) {
            if (_spawningNewPart || !_isPlanted) {
                return;
            }

            var timeElapsed = (_growingParts[_stageIndex].growingStartTime + Stages[_stageIndex].growthTime) - weatherTime;
            this.Trigger(Events.PlantGrowthTimeChanged, this);
            if (timeElapsed <= TimeSpan.Zero) {
                Spawn();
            }
        }

        void Spawn() {
            _spawningNewPart = true;

            var currentRegrowablePart = Stages[_stageIndex].regrowablePart;
            _growingParts[_stageIndex].arAssetReference = currentRegrowablePart;
            var prefabLoading = currentRegrowablePart.LoadAsset<GameObject>();
            prefabLoading.OnComplete(OnPrefabLoaded);
        }

        void OnPrefabLoaded(ARAsyncOperationHandle<GameObject> handle) {
            if (handle.Status == AsyncOperationStatus.Failed) {
                Log.Minor?.Error($"Failed to load regrowable asset! PlantSlot: {this}");
                return;
            }

            var parentTransform = Services.Get<ViewHosting>().LocationsHost(CurrentDomain);
            var spawnedRegrowablePart = Object.Instantiate(handle.Result, _plantPosition, _plantRotation, parentTransform);
            spawnedRegrowablePart.SetUnityRepresentation(new IWithUnityRepresentation.Options {
                linkedLifetime = true,
                movable = true
            });
            
            if (_stageIndex > 0) {
                DespawnPart(_stageIndex - 1);
            }

            _growingParts[_stageIndex].spawnedObject = spawnedRegrowablePart;
            _spawningNewPart = false;
            _stageIndex++;
            
            if (_stageIndex >= Stages.Length) {
                TriggerPlantStateChange();
                RemoveGameRealTimeListener();

                // disable collider for pickable part - it interferes with the plant slot interaction collider
                var pickableCollider = spawnedRegrowablePart.GetComponentInChildren<Collider>();
                if (pickableCollider != null) {
                    pickableCollider.enabled = false;
                }

                return;
            }

            // if it's not default then it is being restored so we prevent overriding growing start time
            if (_growingParts[_stageIndex].growingStartTime == default) {
                _growingParts[_stageIndex].growingStartTime = _stageIndex == 0
                    ? GameRealTime.WeatherTime
                    : _growingParts[_stageIndex - 1].growingStartTime + Stages[_stageIndex - 1].growthTime;
            }
        }

        void DespawnRegrowableParts() {
            if (_growingParts == null) {
                return;
            }

            for (int index = 0; index < _growingParts.Length; index++) {
                DespawnPart(index);
            }

            _growingParts = null;
        }

        void DespawnPart(int index) {
            if (_growingParts == null) {
                return;
            }
            
            GrowingPartData regrowableAsset = _growingParts[index];
            if (regrowableAsset.arAssetReference is { IsSet: true }) {
                Object.Destroy(regrowableAsset.spawnedObject);
                regrowableAsset.arAssetReference.ReleaseAsset();
                regrowableAsset.spawnedObject = null;
                regrowableAsset.arAssetReference = null;
                _growingParts[index] = regrowableAsset;
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            DespawnRegrowableParts();
            RemoveGameRealTimeListener();
        }
    }
}