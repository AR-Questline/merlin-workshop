using System.Collections.Generic;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.UI;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.MVC {
    /// <summary>
    /// This service is used by views to find their desired hosts (parent transforms
    /// that they want to be added to). Mostly this is just calling one of the methods
    /// here from View.DetermineHost().
    /// </summary>
    public class ViewHosting : IService {
        const int MaxDynamicLocationsChildren = 3;
        const int MaxMapMarkersChildren = 15;

        // === Supported root containers
        Transform _mainCanvasRoot;
        Transform _hudRoot;
        Transform _alwaysOnHUDRoot;
        Transform _tutorialsRoot;
        Transform _mapCompassRoot;
        Transform _tooltipRoot;
        Transform _spawned;
        Transform _spawnedForHero;
        DynamicParentData _mapMarkers;
        Dictionary<int, DynamicParentData> _locationsParents = new();

        CanvasService CanvasService => World.Services.Get<CanvasService>();
        
        // === Constructors

        public ViewHosting(Transform sceneRoot) {
            _mainCanvasRoot = CanvasService.MainCanvas.transform;
            _hudRoot = CanvasService.HUDCanvas.GrabChild<Transform>("HUD");
            _alwaysOnHUDRoot = CanvasService.HUDCanvas.GrabChild<Transform>("AlwaysOnHUD");
            _tutorialsRoot = CanvasService.TutorialCanvas.transform;
            _mapCompassRoot = CanvasService.MapCompassCanvas.transform;
            _tooltipRoot = CanvasService.TooltipCanvas.transform;

            var spawnedScene = sceneRoot.gameObject.scene;
            _spawned = new GameObject("Spawned").transform;
            _spawned.SetParent(sceneRoot, true);

            _mapMarkers = new("MapMarkers", spawnedScene, MaxMapMarkersChildren);

            _spawnedForHero = new GameObject("HeroSpawned").transform;
            _spawnedForHero.hierarchyCapacity = 5;
            SceneManager.MoveGameObjectToScene(_spawnedForHero.gameObject, spawnedScene);

            SceneManager.sceneUnloaded += ClearLocationsParentForUnloadedScene;
        }

        // === Possible hosts

        /// <summary>
        /// Returns the default Transform views are spawned in.
        /// </summary>
        public Transform DefaultHost() => _spawned;

        /// <summary>
        /// Returns the default Transform views are spawned in.
        /// </summary>
        public Transform DefaultForHero() => _spawnedForHero;
        
        /// <summary>
        /// Used by views that want to spawn inside some object in the HUD canvas. Can be disabled by user settings.
        /// </summary>
        public Transform OnHUD(params string[] containerPath) => _hudRoot.GrabChild<Transform>(containerPath);
        
        /// <summary>
        /// Used by views that want to spawn inside some object in the HUD canvas. Always visible no matter user settings.
        /// </summary>
        public Transform OnAlwaysVisibleHUD(params string[] containerPath) => _alwaysOnHUDRoot.GrabChild<Transform>(containerPath);

        /// <summary>
        /// Used by views that want to spawn inside the main full-screen canvas.
        /// </summary>
        public Transform OnMainCanvas(params string[] containerPath) => _mainCanvasRoot.GrabChild<Transform>(containerPath);
        
        /// <summary>
        /// Used by views that want to spawn inside the tutorial canvas.
        /// </summary>
        public Transform OnTutorials(params string[] containerPath) => _tutorialsRoot.GrabChild<Transform>(containerPath);
        
        /// <summary>
        /// Used by map compass only.
        /// </summary>
        public Transform OnMapCompass(params string[] containerPath) => _mapCompassRoot.GrabChild<Transform>(containerPath);

        /// <summary>
        /// Used by views that want to spawn inside some object in the sticker canvas.
        /// </summary>
        public Transform OnTooltipCanvas(params string[] containerPath) => _tooltipRoot.GrabChild<Transform>(containerPath);

        /// <summary>
        /// Used by views that want to spawn on the main "Cameras" object.
        /// </summary>
        public Transform OnCamera() => World.Only<CameraStateStack>().MainCamera.transform;

        /// <summary>
        /// Used by views spawned for <see cref="Awaken.TG.Main.Locations.Location"/>.
        /// </summary>
        public Transform LocationsHost(Domain domain, Scene? preferredScene = null) {
            Scene destinationScene = preferredScene ?? SceneManager.GetActiveScene();
            if (domain == Domain.Gameplay) {
                destinationScene = _spawned.gameObject.scene;
            }
            return DynamicHost(destinationScene);
        }

        public Transform DynamicHost(Scene scene) {
            if (!_locationsParents.TryGetValue(scene.handle, out var locationParent)) {
                locationParent = new("SpawnedLocations", scene, MaxDynamicLocationsChildren);
                _locationsParents[scene.handle] = locationParent;
            }
            return locationParent.GetParent();
        }

        /// <summary>
        /// Used by views spawned for <see cref="MapMarker"/>.
        /// </summary>
        public Transform MapMarkersHost() => _mapMarkers.GetParent();

        void ClearLocationsParentForUnloadedScene(Scene scene) {
            if (!_locationsParents.TryGetValue(scene.handle, out var locationParent)) {
                return;
            }
            locationParent.Clear();
            _locationsParents.Remove(scene.handle);
        }

        class DynamicParentData {
            readonly Scene _targetScene;
            readonly string _baseName;
            readonly int _capacity;

            int _counter;
            int _nameCounter;

            Transform _currentParent;

            public DynamicParentData(string baseName, Scene targetScene, int capacity) {
                _targetScene = targetScene;
                _baseName = baseName;
                _capacity = capacity;
                CreateNewParent();
            }

            public Transform GetParent() {
                if (_counter > _capacity || !_currentParent) {
                    CreateNewParent();
                }
                ++_counter;
                return _currentParent;
            }

            public void Clear() {
                // Don't destroy because it was/will be destroyed with the scene
                _currentParent = null;
            }

            void CreateNewParent() {
                _counter = 0;
                var name = $"{_baseName}_{_nameCounter++}";
                var locationParentGameObject = new GameObject(name);
                _currentParent = locationParentGameObject.transform;
                _currentParent.hierarchyCapacity = _capacity;
                SceneManager.MoveGameObjectToScene(locationParentGameObject, _targetScene);
            }
        }
    }
}
