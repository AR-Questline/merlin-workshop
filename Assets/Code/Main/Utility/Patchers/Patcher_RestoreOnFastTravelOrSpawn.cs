using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Patchers {
    public abstract class Patcher_RestoreOnFastTravelOrSpawn : Patcher, IListenerOwner {
        protected static readonly SceneReference CampaignMapHoS = SceneByGuid("3f87cb1c5e4dacb4bbd48153dcc4b7c8");
        protected static readonly SceneReference CampaignMapCuanacht = SceneByGuid("ef5ed804b2393314cad3ee1703f610e7");
        protected static readonly SceneReference CampaignMapForlorn = SceneByGuid("d4ac3bda5d6875f428002f6bb2423c8a");

        readonly SceneReference[] _scenesToWorksOn;
        // For preventing race condition/making sure that we have correct restored hero position to analyze
        bool _heroPositionRestored;
        bool _loadingCompleted;

        protected Patcher_RestoreOnFastTravelOrSpawn(SceneReference[] scenesToWorksOn) {
            _scenesToWorksOn = scenesToWorksOn;
        }

        public sealed override void BeforeDeserializedModel(Model model) {
            if (model is Hero hero) {
                _heroPositionRestored = false;
                _loadingCompleted = false;
                hero.ModifyRestorePosition = ModifyRestorePosition;
                World.EventSystem.LimitedListenTo(SceneLifetimeEvents.Get.ID, SceneLifetimeEvents.Events.OnFullSceneLoaded, this, _ => ModifyRestorePositionWithTeleport(Hero.Current.Coords), 1);
            }
            OnBeforeDeserializedModel(model);
        }

        protected virtual void OnBeforeDeserializedModel(Model model) { }

        bool ShouldModifyRestorationPosition() {
            if (_scenesToWorksOn == null) {
                Log.Important?.Error($"{this.GetType().Name} shouldn't modify position. No scenes to work on defined.");
                return false;
            }
            var sceneService = World.Services.Get<SceneService>();
            var currentScene = sceneService.AdditiveSceneRef ?? sceneService.MainSceneRef;
            foreach (var sceneToWorkOn in _scenesToWorksOn) {
                if (sceneToWorkOn == currentScene) {
                    Log.Important?.Error($"{this.GetType().Name} should modify position. Hero on a correct scene.");
                    return true;
                }
            }
            Log.Important?.Error($"{this.GetType().Name} shouldn't modify position. Hero is not on a correct scene.");
            return false;
        }

        Vector3 ModifyRestorePosition(Vector3 desiredPosition) {
            _heroPositionRestored = true;
            if (!_loadingCompleted) {
                return desiredPosition;
            }
            
            if (!ShouldModifyRestorationPosition()) {
                return desiredPosition;
            }
            
            bool hasFastTravelPosition = GetNewDestinationPosition(desiredPosition, out Vector3 closestFastTravelPosition);
            return hasFastTravelPosition ? closestFastTravelPosition : desiredPosition;
        }

        void ModifyRestorePositionWithTeleport(Vector3 desiredPosition) {
            _loadingCompleted = true;
            if (!_heroPositionRestored) return;
            if (!ShouldModifyRestorationPosition()) return;
            
            if (GetNewDestinationPosition(desiredPosition, out Vector3 closestFastTravelPosition)) {
                Hero.Current.TeleportTo(closestFastTravelPosition, Hero.Current.Rotation, overrideTeleport: true);
            }
        }

        bool GetNewDestinationPosition(Vector3 desiredPosition, out Vector3 closestFastTravelPosition) {
            var currentDomain = Domain.CurrentScene();
            bool hasFastTravelPosition = false;
            var closestFastTravelDistanceSq = 0f;
            closestFastTravelPosition = Vector3.zero;
            Log.Important?.Error($"{this.GetType().Name} trying to find new destination for hero from {desiredPosition}");
            
            foreach (var locationDiscovery in World.All<LocationDiscovery>()) {
                if (locationDiscovery.IsFastTravel && locationDiscovery.Discovered && locationDiscovery.CurrentDomain == currentDomain) {
                    var fastTravelPosition = locationDiscovery.FastTravelPoint;
                    var distanceSq = fastTravelPosition.SquaredDistanceTo(desiredPosition);
                    if (!hasFastTravelPosition || distanceSq < closestFastTravelDistanceSq) {
                        hasFastTravelPosition = true;
                        closestFastTravelDistanceSq = distanceSq;
                        closestFastTravelPosition = fastTravelPosition;
                        Log.Important?.Error($"{this.GetType().Name} trying to find new destination success. Setup from fast travel ({LogUtils.GetDebugName(locationDiscovery.ParentModel)}) {closestFastTravelPosition}");
                    }
                }
            }

            if (!hasFastTravelPosition) {
                var entry = Portal.FindDefaultEntry();
                if (entry != null) {
                    hasFastTravelPosition = true;
                    closestFastTravelPosition = entry.GetDestination().position;
                    Log.Important?.Error($"{this.GetType().Name} trying to find new destination success. Setup from entry portal {closestFastTravelPosition}");
                } else {
                    Log.Important?.Error($"{this.GetType().Name} trying to find new destination failed. No fast travel and no entry.");
                }
            }

            return hasFastTravelPosition;
        }

        protected static SceneReference SceneByGuid(string guid) {
            return SceneReference.ByAddressable(new ARAssetReference(guid));
        }
    }
}