using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.Main.Scenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Maths;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Patchers {
    public abstract class Patcher_RestoreOnFastTravel : Patcher, IListenerOwner {
        protected static readonly SceneReference CampaignMap = SceneByGuid("3f87cb1c5e4dacb4bbd48153dcc4b7c8");

        readonly SceneReference[] _scenesToWorksOn;
        // For preventing race condition/making sure that we have correct restored hero position to analyze
        bool _heroPositionRestored;
        bool _loadingCompleted;

        protected Patcher_RestoreOnFastTravel(SceneReference[] scenesToWorksOn) {
            _scenesToWorksOn = scenesToWorksOn;
        }

        public override void BeforeDeserializedModel(Model model) {
            if (model is Hero hero) {
                hero.ModifyRestorePosition = ModifyRestorePosition;
                World.EventSystem.LimitedListenTo(SceneLifetimeEvents.Get.ID, SceneLifetimeEvents.Events.OnFullSceneLoaded, this, _ => ModifyRestorePositionWithTeleport(Hero.Current.Coords), 1);
            }
        }

        bool ShouldModifyRestorationPosition() {
            if (_scenesToWorksOn == null) {
                return false;
            }
            var sceneService = World.Services.Get<SceneService>();
            var currentScene = sceneService.AdditiveSceneRef ?? sceneService.MainSceneRef;
            foreach (var sceneToWorkOn in _scenesToWorksOn) {
                if (sceneToWorkOn == currentScene) {
                    return true;
                }
            }
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

        static bool GetNewDestinationPosition(Vector3 desiredPosition, out Vector3 closestFastTravelPosition) {
            var currentDomain = Domain.CurrentScene();
            bool hasFastTravelPosition = false;
            var closestFastTravelDistanceSq = 0f;
            closestFastTravelPosition = Vector3.zero;
            
            foreach (var locationDiscovery in World.All<LocationDiscovery>()) {
                if (locationDiscovery.IsFastTravel && locationDiscovery.Discovered && locationDiscovery.CurrentDomain == currentDomain) {
                    var fastTravelPosition = locationDiscovery.FastTravelPoint;
                    var distanceSq = fastTravelPosition.SquaredDistanceTo(desiredPosition);
                    if (!hasFastTravelPosition || distanceSq < closestFastTravelDistanceSq) {
                        hasFastTravelPosition = true;
                        closestFastTravelDistanceSq = distanceSq;
                        closestFastTravelPosition = fastTravelPosition;
                    }
                }
            }

            if (!hasFastTravelPosition) {
                var entry = Portal.FindDefaultEntry();
                if (entry != null) {
                    hasFastTravelPosition = true;
                    closestFastTravelPosition = entry.GetDestination().position;
                }
            }

            return hasFastTravelPosition;
        }

        static SceneReference SceneByGuid(string guid) {
            return SceneReference.ByAddressable(new ARAssetReference(guid));
        }
    }
}