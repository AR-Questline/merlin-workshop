using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Containers;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Times;
using Sirenix.Utilities;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class FurnitureSlotLootHandler : Element<FurnitureSlotBase> {
        public override ushort TypeForSerialization => SavedModels.FurnitureSlotLootHandler;

        [Saved] Dictionary<string, List<ItemSpawningDataRuntime>> _lootInFurniture = new();
        [Saved] ARDateTime _renewDateTime;
        string _currentFurnitureTemplateGUID;
        IEventListener _containerEmptiedListener;
        FurnitureSearchAction _currentSearchAction;
        GameRealTime _gameRealTime;
        GameTimeEvents _gameTimeEvents;
        TimedEvent _renewLootEvent;

        protected override void OnInitialize() {
            _gameRealTime = World.Only<GameRealTime>();
            _gameTimeEvents = World.Only<GameTimeEvents>();
        }

        public void TrackLootForSpawnedFurniture(Location furnitureLocation, bool variantChangedByPlayer) {
            World.EventSystem.TryDisposeListener(ref _containerEmptiedListener);
            TryRemoveGameTimeEvent();
            
            _currentSearchAction = furnitureLocation.Element<FurnitureSearchAction>();
            _containerEmptiedListener = furnitureLocation.ListenTo(Events.AfterElementsCollectionModified, OnContainerUIDiscarded);
            _currentFurnitureTemplateGUID = furnitureLocation.Template.GUID;
            
            ResolveLootInSpawnedFurniture(variantChangedByPlayer);
        }

        void ResolveLootInSpawnedFurniture(bool variantChangedByPlayer) {
            // newly spawned furniture variant - add to tracking
            if (!_lootInFurniture.TryGetValue(_currentFurnitureTemplateGUID, out List<ItemSpawningDataRuntime> storedLootItems)) {
                _currentSearchAction.GenerateLoot();
                _lootInFurniture.Add(_currentFurnitureTemplateGUID, _currentSearchAction.ItemsInsideContainer);
                
                return;
            }

            // furniture variant tracked and has some loot inside
            if (!storedLootItems.IsNullOrEmpty()) {
                _currentSearchAction.GenerateLoot(storedLootItems);
                
                return;
            }
            
            // furniture variant tracked but has no loot inside - reset timer if player changed furniture variant; don't reset if player left scene and came back
            if (variantChangedByPlayer) {
                _renewDateTime = _gameRealTime.WeatherTime + _currentSearchAction.RenewLootRate;
            }
            
            AddGameTimeEvent();
        }
        
        void AddGameTimeEvent() {
            _renewLootEvent = new TimedEvent(_renewDateTime.Date, OnRenewLootEvent);
            _gameTimeEvents.AddEvent(_renewLootEvent);
        }
        
        void TryRemoveGameTimeEvent() {
            if (_renewLootEvent == null) {
                return;
            }
            
            _gameTimeEvents.RemoveEvent(_renewLootEvent);
            _renewLootEvent = null;
        }

        void OnContainerUIDiscarded(Element element) {
            if (element is ContainerUI {IsEmpty: true}) {
                _renewDateTime = _gameRealTime.WeatherTime + _currentSearchAction.RenewLootRate;
                _lootInFurniture[_currentFurnitureTemplateGUID] = new List<ItemSpawningDataRuntime>();
                AddGameTimeEvent();
            }
        }

        void OnRenewLootEvent() {
            _currentSearchAction.GenerateLoot();
            _renewDateTime = default;
            _lootInFurniture[_currentFurnitureTemplateGUID] = _currentSearchAction.ItemsInsideContainer;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            TryRemoveGameTimeEvent();
        }
    }
}