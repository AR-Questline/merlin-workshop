using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet.Map;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.Pool;

namespace Awaken.TG.Main.Maps.Compasses {
    [SpawnsView(typeof(VMapCompass))]
    public partial class Compass : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.Compass;

        ObjectPool<VCompassElement> _compassElementPool;
        VMapCompass _view;
        Transform _mainCameraTransform;
        CompassVisualState _visualState;
        int _searchAreaSemaphore;
        
        public bool CompassEnabled { get; private set; }
        public Vector3 Forward => _mainCameraTransform ? _mainCameraTransform.forward : Vector3.zero;
        public Vector3 Position => _mainCameraTransform ? _mainCameraTransform.position : Vector3.zero;
        public Location CustomMarkerLocation => _customMarkerLocation.Get();
        public Location SpyglassMarkerLocation => _spyglassMarkerLocation.Get();

        static LocationTemplate CustomMarkerTemplate => Services.Get<CommonReferences>().CustomMarkerTemplate;
        static LocationTemplate SpyglassMarkerTemplate => Services.Get<CommonReferences>().SpyglassMarkerTemplate;
        public Vector3? SpyglassMarkerCoords => _spyglassMarkerCoords;
        
        WeakModelRef<Location> _customMarkerLocation;
        WeakModelRef<Location> _spyglassMarkerLocation;
        [Saved] Vector3? _customMarkerCoords;
        [Saved] Vector3? _spyglassMarkerCoords;

        public new static class Events {
            public static readonly Event<Compass, CompassVisualState> VisualStateChanged = new(nameof(VisualStateChanged));
            public static readonly Event<Compass, bool> SearchAreaStateChanged = new(nameof(SearchAreaStateChanged));
        }

        public Compass() {
            ModelElements.SetInitCapacity(600);
            ModelElements.SetInitCapacity(typeof(NpcCompassMarker), 1, 444);
            ModelElements.SetInitCapacity(typeof(CompassMarker), 2, 124);
        }

        protected override void OnFullyInitialized() {
            _view = View<VMapCompass>();
            
            _compassElementPool = new ObjectPool<VCompassElement>(
                createFunc: () => Object.Instantiate(_view.CompassElementPrefab, _view.MarkerParent),
                actionOnGet: ce => ce.gameObject.SetActive(true),
                actionOnRelease: ce => ce.gameObject.SetActive(false),
                actionOnDestroy: ce => {
#if UNITY_EDITOR
                    if (ce == null) {
                        return;
                    }
#endif
                    Object.Destroy(ce.gameObject);
                },
                defaultCapacity: 10,
                maxSize: 25
            );
            
            var showUIHUD = World.Only<ShowUIHUD>();
            CompassEnabled = showUIHUD.CompassEnabled;
            showUIHUD.ListenTo(Setting.Events.SettingChanged, _ => CompassEnabled = showUIHUD.CompassEnabled, this);
            
            _mainCameraTransform = World.Only<GameCamera>().MainCamera.transform;
            _visualState = new CompassVisualState();
            
            ParentModel.AfterFullyInitialized(AfterHeroFullyInitialized);
            
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.AfterSceneStoriesExecuted, this, OnAfterSceneFullyInitialized);
        }

        void AfterHeroFullyInitialized() {
            InitializeWorldDirectionElements();
            
            foreach (var marker in World.All<ICompassMarker>().Where(m => m.IsFullyInitialized)) {
                CompassElement compassElement = marker.CompassElement;
                if (compassElement is not null) {
                    AddElement(compassElement);
                }
            }
            
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<ICompassMarker>(), this, OnCompassMarkerFullyInitialized);
            World.EventSystem.ListenTo(EventSelector.AnySource, CompassElement.Events.StateChanged, this, ToggleCompassElementState);

            var trespassingTracker = ParentModel.Element<TrespassingTracker>();
            if (trespassingTracker.IsTrespassing) {
                _visualState.trespassing = trespassingTracker.IsTrespassing;
                UpdateState();
            }
            trespassingTracker.ListenTo(TrespassingTracker.Events.TrespassingStateChanged, trespassing => {
                _visualState.trespassing = trespassing;
                UpdateState();
            }, this);
        }
        
        public void CreateSpyglassMarker(Vector3 coords) {
            _spyglassMarkerLocation.Get()?.Discard();
            
            Location location = SpyglassMarkerTemplate.SpawnLocation(coords);
            location.MarkedNotSaved = true;
            _spyglassMarkerLocation = location;
            _spyglassMarkerCoords = coords;
            CreateMarker(location);
        }

        public void TryReleaseCompassElementView(CompassElement compassElement) {
            if (!compassElement.IsDisplayed) {
                return;
            }

            _compassElementPool.Release(compassElement.TemporaryView);
            compassElement.CleanupView();
        }

        void ToggleCompassElementState(CompassElement compassElement) {
            if (compassElement.ShouldBeDisplayed) {
                TrySetupCompassElementView(compassElement);
            } else {
                TryReleaseCompassElementView(compassElement);
            }
        }

        void OnCompassMarkerFullyInitialized(Model model) {
            AddMarker((ICompassMarker) model);
        }

        void UpdateState() {
            this.Trigger(Events.VisualStateChanged, _visualState);
        }
        
        void InitializeWorldDirectionElements() {
            TrySetupCompassElementView(AddElement(new WorldDirection(new Vector3(0, 0, 1), _view.north)));
            TrySetupCompassElementView(AddElement(new WorldDirection(new Vector3(0, 0, -1), _view.south)));
            TrySetupCompassElementView(AddElement(new WorldDirection(new Vector3(1, 0, 0), _view.east)));
            TrySetupCompassElementView(AddElement(new WorldDirection(new Vector3(-1, 0, 0), _view.west)));
        }

        void AddMarker(ICompassMarker marker) {
            CompassElement compassElement = marker.CompassElement;
            if (compassElement is not null) {
                AddElement(compassElement);

                if (compassElement.ShouldBeDisplayed) {
                    TrySetupCompassElementView(compassElement);
                }
            }
        }
        
        void TrySetupCompassElementView(CompassElement compassElement) {
            if (compassElement.IsDisplayed) {
                return;
            }
            
            _compassElementPool.Get(out VCompassElement vCompassElement);
            compassElement.SetupView(vCompassElement);
        }
        


        MapMarker CreateCustomMarker(Vector3 coords) {
            _customMarkerLocation.Get()?.Discard();
            
            Location location = CustomMarkerTemplate.SpawnLocation(coords);
            location.MarkedNotSaved = true;
            _customMarkerLocation = location;
            _customMarkerCoords = coords;
            return CreateMarker(location);
        }

        static MapMarker CreateMarker(Location location) {
            LocationMarker baseCompassMarker = location.Element<LocationMarker>();
            MarkerData markerData = baseCompassMarker.MarkerData;
            int order = MapMarkerOrder.CustomMarker.ToInt();
            var marker = location.AddElement(new PointMapMarker(new WeakModelRef<IGrounded>(location), () => location.DisplayName, markerData, order, true));
            CompassMarker compassElement = baseCompassMarker.CompassElement;
            compassElement.SetShowDistance(true);
            return marker;
        }

        /// <summary>
        /// We need to check every instance of AfterSceneFullyInitialized because we can change scenes without discarding the compass,
        /// and changing scenes discards the custom marker
        /// </summary>
        void TryRestoreCustomMarker() {
            if (!MapUI.IsOnSceneWithMap()) {
                return;
            } 
            
            if (_customMarkerCoords is null || _customMarkerLocation.Get() != null ) {
                return;
            }
            
            RestoreCustomMarker();
        }

        void OnAfterSceneFullyInitialized(SceneLifetimeEventData _) {
            TryRestoreCustomMarker();
            TryRestoreSpyglassMarker();
        }

        void TryRestoreSpyglassMarker() {
            if (!MapUI.IsOnSceneWithMap()) {
                return;
            } 
            
            if (_spyglassMarkerCoords is null || _spyglassMarkerLocation.Get() != null ) {
                return;
            }

            RestoreSpyglassMarker();
        }
        
        void RestoreCustomMarker() {
            CreateCustomMarker(_customMarkerCoords!.Value);
        }

        void RestoreSpyglassMarker() {
            CreateSpyglassMarker(_spyglassMarkerCoords!.Value);
        }
        
        public MapMarker PlaceCustomMarker(Vector3 coords) {
            if (_customMarkerCoords == coords) {
                _customMarkerLocation.Get()?.Discard();
                _customMarkerCoords = null;
                return null;
            }
                
            if (_spyglassMarkerCoords == coords) { 
                _spyglassMarkerLocation.Get()?.Discard();
                _spyglassMarkerCoords = null;
                return null;
            }

            return CreateCustomMarker(coords);
        }

        public bool TryRemoveCustomMarker(Location locationToRemove) {
            if (_customMarkerLocation.TryGet(out Location marker) && marker == locationToRemove) {
                marker.Discard();
                _customMarkerCoords = null;
                return true;
            }
            return false;
        }
        
        public bool TryRemoveSpyglassMarker(Location locationToRemove) {
            if (_spyglassMarkerLocation.TryGet(out Location marker) && marker == locationToRemove) {
                marker.Discard();
                _spyglassMarkerCoords = null;
                return true;
            }
            return false;
        }

        public void NotifyEnterSearchArea() {
            _searchAreaSemaphore++;
            if (_searchAreaSemaphore == 1) {
                this.Trigger(Events.SearchAreaStateChanged, true);
            }
        }

        public void NotifyExitSearchArea() {
            _searchAreaSemaphore--;
            if (_searchAreaSemaphore == 0) {
                this.Trigger(Events.SearchAreaStateChanged, false);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _compassElementPool.Dispose();
        }
        
#if UNITY_EDITOR
        public void EDITOR_DEBUG_ShowCompass(bool show) {
            _view.TrySetActiveOptimized(show);
        }
#endif
    }

    public struct CompassVisualState {
        public bool trespassing;
    }
}