using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Locations.Elevator;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    public class VCElevatorPlatformChecker : VCHeroVolumeChecker {
        const string MovingPlatformTag = "MovingPlatformVolume";
        
        VHeroController _heroController;
        ElevatorPlatform _elevatorPlatform;
        IEventListener _elevatorPlatformDiscardListener;
        bool _heroMounted;

        protected override void OnAttach() {
            Hero.AfterFullyInitialized(() => {
                _heroController = Hero.VHeroController;
                Hero.ListenTo(HeroMovementSystem.Events.MovementSystemChanged(MovementType.Mounted), OnMounted, this);
            });
        }

        void OnMounted(HeroMovementSystem mounted) {
            _heroMounted = mounted;
            
            mounted.ListenTo(Model.Events.BeforeDiscarded, () => {
                if (_elevatorPlatform != null) {
                    ParentHeroToPlatform();
                }
            }, this);
        }

        protected override void OnEnter(Collider other) {
            if (other.CompareTag(MovingPlatformTag) && _elevatorPlatform == null) {
                IModel model = VGUtils.GetModel(other.gameObject);
                _elevatorPlatform = model?.TryGetElement<ElevatorPlatform>();
                _elevatorPlatformDiscardListener = _elevatorPlatform.ListenTo(Model.Events.BeforeDiscarded, UnparentHero, _elevatorPlatform);
                if (!_heroMounted) {
                    ParentHeroToPlatform();
                }
            }
        }

        void ParentHeroToPlatform() {
            _heroController.transform.SetParent(_elevatorPlatform.PlatformParentTransform, true);
            _heroController.SetActiveTppCameraVerticalDamping(false);
        }

        protected override void OnExit(Collider other, bool destroyed = false) {
            if (destroyed || _elevatorPlatform == null) {
                return;
            }
            
            if (other.CompareTag(MovingPlatformTag)) {
                UnparentHero();
            }
        }
        
        void UnparentHero() {
            if (_elevatorPlatform != null) {
                _elevatorPlatform = null;
                World.EventSystem.TryDisposeListener(ref _elevatorPlatformDiscardListener);
                if (!_heroMounted) {
                    _heroController.transform.SetParent(Services.Get<ViewHosting>().DefaultForHero(), true);
                }
                _heroController.SetActiveTppCameraVerticalDamping(true);
            }
        }

        protected override void OnFirstVolumeEnter(Collider other) { }

        protected override void OnAllVolumesExit() { }

        protected override void OnStay() { }
    }
}