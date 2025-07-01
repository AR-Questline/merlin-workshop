using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Elevator {
    /// <summary>
    /// Attached in elevator platform prefab
    /// Checker for npc on the elevator platform. We want to parent only dead bodies to the platform and don't parent alive npcs.
    /// </summary>
    public class VCElevatorPlatformNpcChecker : ViewComponent<Location> {
        readonly Dictionary<Location, ElevatorLocationData> _platformLocations = new();
        ElevatorPlatform _elevatorPlatform;

        protected override void OnAttach() {
            _elevatorPlatform = Target.TryGetElement<ElevatorPlatform>();
        }
        
        void AfterNpcElementDiscarded(Model model) {
            if (model is NpcElement npcElement) {
                Location location = npcElement.ParentModel;

                if (location.HasBeenDiscarded) {
                    if (_platformLocations.TryGetValue(location, out ElevatorLocationData data)) {
                        if (data.eventListener != null) {
                            World.EventSystem.RemoveListener(data.eventListener);
                        }
                        
                        _platformLocations.Remove(location);
                    }
                    return;
                }

                if (location.HasElement<NpcDummy>()) {
                    AttachDummyToPlatform(location);
                }
            }
        }

        void AttachDummyToPlatform(Location location) {
            if (_platformLocations.TryGetValue(location, out ElevatorLocationData data)) {
                if (data.eventListener != null) {
                    World.EventSystem.RemoveListener(data.eventListener);
                }
                    
                data.locationParent.transform.SetParent(_elevatorPlatform.PlatformParentTransform, true);
            }
        }

        void OnTriggerEnter(Collider other) {
            IModel model = VGUtils.GetModel(other.gameObject);

            if (model is Location location) {
                if (location.IsVisualLoaded) {
                    TryAttachLocation(location, other.gameObject);
                } else {
                    location.OnVisualLoaded(locationTransform => {
                        TryAttachLocation(location, locationTransform.gameObject);
                    });
                }
            }
        }

        void TryAttachLocation(Location location, GameObject other) {
            if (!location.HasElement<MovingPlatform>() && !_platformLocations.ContainsKey(location)) {
                if (location.HasElement<NpcElement>()) {
                    location.AddElement(new MovingPlatform(_elevatorPlatform));
                    IEventListener npcElementDiscarded = location.Element<NpcElement>().ListenTo(Model.Events.AfterDiscarded, AfterNpcElementDiscarded, this);
                    LocationParent locationParent = other.GetComponentInParent<LocationParent>();
                    _platformLocations[location] = new ElevatorLocationData {
                        locationParent = locationParent,
                        originParent = locationParent.transform.parent,
                        eventListener = npcElementDiscarded
                    };
                }

                if (location.HasElement<NpcDummy>()) {
                    location.AddElement(new MovingPlatform(_elevatorPlatform));
                    LocationParent locationParent = other.GetComponentInParent<LocationParent>();
                    _platformLocations.TryAdd(location, new ElevatorLocationData {
                        locationParent = locationParent,
                        originParent = locationParent.transform.parent
                    });
                    
                    AttachDummyToPlatform(location);
                }
            }
        }

        void OnTriggerExit(Collider other) {
            IModel model = VGUtils.GetModel(other.gameObject);
                    
            if (model is Location location && _platformLocations.TryGetValue(location, out ElevatorLocationData data)) {
                location.TryGetElement<MovingPlatform>()?.Discard();
                
                if (data.eventListener != null) {
                    World.EventSystem.RemoveListener(data.eventListener);
                }
                        
                data.locationParent.transform.SetParent(data.originParent, true);
                _platformLocations.Remove(location);
            }
        }

        struct ElevatorLocationData {
            public LocationParent locationParent;
            public Transform originParent;
            public IEventListener eventListener;
        }
    }
}