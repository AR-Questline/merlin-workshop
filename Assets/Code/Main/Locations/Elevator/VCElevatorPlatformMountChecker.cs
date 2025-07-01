using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Elevator {
    /// <summary>
    /// Attached in mount prefab
    /// </summary>
    public class VCElevatorPlatformMountChecker : ViewComponent<Location> {
        const string MovingPlatformTag = "MovingPlatformVolume";

        MountElement _mountElement;
        ElevatorPlatform _elevatorPlatform;
        LocationParent _locationParent;
        Transform _locationParentParent;
        bool _onThePlatform;
        CharacterController _characterController;

        protected override void OnAttach() {
            _locationParent = GetComponentInParent<LocationParent>();
        }
        
        void OnTriggerEnter(Collider other) {
            if (other.CompareTag(MovingPlatformTag) && !_onThePlatform && Target.IsVisualLoaded) {
                IModel model = VGUtils.GetModel(other.gameObject);
                _elevatorPlatform = model?.TryGetElement<ElevatorPlatform>();
                if (_elevatorPlatform != null && Target.TryGetElement(out _mountElement)) {
                    Transform locationParentTransform = _locationParent.transform;
                    _locationParentParent = locationParentTransform.parent;
                    locationParentTransform.SetParent(_elevatorPlatform.PlatformParentTransform, true);
                    _mountElement.ParentModel.AddElement(new MovingPlatform(_elevatorPlatform));
                    _onThePlatform = true;
                }
            }
        }

        void OnTriggerExit(Collider other) {
            if (other.CompareTag(MovingPlatformTag) && _onThePlatform) {
                Target.TryGetElement<MovingPlatform>()?.Discard();
                _elevatorPlatform = null;
                
                if (_mountElement != null) {
                    Transform locationParentTransform = _locationParent.transform;
                    locationParentTransform.SetParent(_locationParentParent, true);
                    _locationParentParent = null;
                    _onThePlatform = false;
                    _mountElement = null;
                }
            }
        }
    }
}