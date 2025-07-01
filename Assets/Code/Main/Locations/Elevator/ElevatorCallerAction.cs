using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Elevator {
    public partial class ElevatorCallerAction : LogicEmitterActionBase<ElevatorCallerAttachment> {
        public override ushort TypeForSerialization => SavedModels.ElevatorCallerAction;

        public Vector3 TargetPosition => _attachment.targetPoint.position;
        public GameObject NavmeshCutObject => _attachment.navmeshCutObject;
        
        public new static class Events {
            public static readonly Event<ElevatorCallerAction, ElevatorCallerAction> PlatformCalled = new(nameof(PlatformCalled));
        }
        
        public void SetNavmeshCutObjectActive(bool active) {
            if (NavmeshCutObject == null) {
                return;
            }
            
            NavmeshCutObject.SetActive(active);
        }

        protected override bool IsActive() => ParentModel.Interactability.interactable;

        protected override void OnAnimatorUpdate(bool _) {
            _animator.SetTrigger(TriggerHash);
        }

        protected override void SendInteractEventsToLocation(Location location, bool active) {
            ElevatorPlatform platform = location.TryGetElement<ElevatorPlatform>();
            if (platform is not {IsMoving: false}) {
                return;
            }
            
            this.Trigger(Events.PlatformCalled, this);
            platform.Trigger(ElevatorPlatform.Events.PlatformMoveRequested, new ElevatorData(TargetPosition));
        }
    }
}