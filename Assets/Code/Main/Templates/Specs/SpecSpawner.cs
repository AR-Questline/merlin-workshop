using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Pickables;
using Awaken.TG.Main.Locations.Regrowables;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Utility.Availability;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Templates.Specs {
    /// <summary>
    /// Responsible for spawning/restoring all specs in the scene
    /// </summary>
    public class SpecSpawner : IDomainBoundService {
        public Domain Domain => Domain.CurrentMainScene();
        public bool RemoveOnDomainChange() => true;

        List<BaseSpec> _specs;
        Dictionary<string, LocationSpec> _locationSpecById;

        public void Init(Scene[] scenes) {
            var specs = Object.FindObjectsByType<SceneSpec>(FindObjectsSortMode.None);
            _specs = new List<BaseSpec>(specs.Length);
            _locationSpecById = new Dictionary<string, LocationSpec>(_specs.Capacity);

            bool exceptionHappened = false;
            foreach (var spec in specs) {
                if (!scenes.Contains(spec.gameObject.scene)) {
                    continue;
                }

#if UNITY_EDITOR
                // we need to cache SceneId because locations change hierarchy invalidating SpecRegistry cache
                spec.CacheSceneId();
 #endif

                if (spec is not BaseSpec baseSpec) {
                    continue;
                }

                _specs.Add(baseSpec);

                if (baseSpec is not LocationSpec locSpec) {
                    continue;
                }
                try {
                    _locationSpecById.Add(locSpec.GetLocationId(), locSpec);
                } catch (Exception e) {
                    Log.Important?.Error($"Failed to add location spec {locSpec.name} to registry: {e}", locSpec.gameObject);
                    exceptionHappened = true;
                }
            }

            if (exceptionHappened) {
                throw new Exception("Failed to initialize SpecSpawner! <color=#FF0000>Errors above are critical! If you don't know what to do call programmer for help!</color>");
            }
        }

        public void Clear() {
            _specs.Clear();
            _specs = null;
            _locationSpecById.Clear();
            _locationSpecById = null;
        }

        /// <summary>
        /// Initialize specs for the first time
        /// </summary>
        public void SpawnAllSpecs() {
            foreach (var spec in _specs) {
                Model model = spec.CreateModel();
                if (!model.IsInitialized) {
                    World.Add(model);
                }
            }
            
            InitNonModelSceneSpecs();
        }

        /// <summary>
        /// Location wants to find it's spec on restore
        /// </summary>
        public LocationSpec FindSpecFor(Location location) {
            return _locationSpecById.GetValueOrDefault(location.ID);
        }

        /// <summary>
        /// Dispose specs on start, when game is loaded from persistent data
        /// </summary>
        public void RestoreSpecs() {
            // Spawn specs that are new
            foreach (var spec in _specs) {
                if (spec is LocationSpec locSpec) {
                    string id = locSpec.GetLocationId();
                    if (World.ByID(id) == null && !WasDiscarded(locSpec)) {
                        Model model = locSpec.CreateModel();
                        if (!model.IsInitialized) {
                            World.Add(model);
                        }
                    }
                } else if (spec.SpawnOnRestore) {
                    Model model = spec.CreateModel();
                    if (!model.IsInitialized) {
                        World.Add(model);
                    }
                }
            }
            
            InitNonModelSceneSpecs();
        }

        void InitNonModelSceneSpecs() {
            AvailabilityInitialization.InitializeWaiting();
            RegrowableInitialization.InitializeWaiting(World.Services.Get<RegrowableService>());
            PickableInitialization.InitializeWaiting(World.Services.Get<PickableService>());
            FactionRegionsService.InitializeWaiting(World.Services.Get<FactionRegionsService>());
        }

        // === Helpers
        static bool WasDiscarded(LocationSpec locSpec) {
            return World.Services.Get<GameplayMemory>().Context(Location.DiscardedPlacesKey).Get(locSpec.GetLocationId(), false);
        }
    }
}