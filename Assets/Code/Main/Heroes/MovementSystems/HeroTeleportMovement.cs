using Awaken.Utility;
using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public partial class HeroTeleportMovement : HeroMovementSystem {
        public override ushort TypeForSerialization => SavedModels.HeroTeleportMovement;

        Transform _transform;
        
        TeleportDestination _teleportDestination;
        
        string _portalTag;
        SceneReference _portalScene;
        
        Action<Portal> _afterTeleported;
        bool _teleportAssigned;
        
        bool _paused;
        bool _mountedTeleport;

        public override MovementType Type => MovementType.Teleport;
        public override bool CanCurrentlyBeOverriden => false;
        public override bool RequirementsFulfilled => true;
        
        protected override void Init() {
            _transform = Controller.Transform;
        }
        protected override void SetupForceExitConditions() { }
        
        void AssignTeleportDestination(TeleportDestination destination, string portalTag, SceneReference portalScene, Action<Portal> afterTeleported, bool canOverride) {
            if (_teleportAssigned && !canOverride) {
                Debug.LogException(new InvalidOperationException("Another teleport is in progress"));
                return;
            }
            
            _teleportDestination = destination;
            _portalTag = portalTag;
            _portalScene = portalScene;
            _afterTeleported = afterTeleported;
            _teleportAssigned = true;
            
            Teleport();
        }
        
        // === Public API
        public void AssignDestinationTeleport(TeleportDestination destination, Action<Portal> afterTeleported, bool canOverride) {
            AssignTeleportDestination(destination, null, null, afterTeleported, canOverride);
        }
        
        public void AssignPortalTeleport(string portalTag, SceneReference portalScene, Action<Portal> afterTeleported, bool canOverride) {
            AssignTeleportDestination(TeleportDestination.Zero, portalTag, portalScene, afterTeleported, canOverride);
        }

        public void PauseTeleport() {
            _paused = true;
        }
        
        public void ResumeTeleport() {
            _paused = false;
            Teleport();
        }

        public void MarkAsMountedTeleport() {
            _mountedTeleport = true;
        }
        
        // === HeroMovementSystem
        public override void Update(float deltaTime) { }

        public override void FixedUpdate(float deltaTime) { }

        void Teleport() {
            if (_paused) return;
            
            Controller.Controller.enabled = false;
            ParentModel.Trigger(GroundedEvents.BeforeTeleported, ParentModel);

            Portal p = null;
            if (_portalScene != null) {
                p = Portal.FindWithTagOrDefault(_portalScene, _portalTag);
                _teleportDestination = p?.GetDestination() ?? _teleportDestination;
            }


            var newPosition = _teleportDestination.position;
            bool longTeleport = (_transform.position - newPosition).sqrMagnitude > VLocationSpawner.MaxSpawnRangeSq;

            // Teleport
            _transform.position = newPosition;
            if (_teleportDestination.Rotation != null) {
                _transform.rotation = _teleportDestination.Rotation.Value;
            }

            Controller.ApplyTransformToTarget();
            Controller.CancelTppCameraDamping();
            _afterTeleported?.Invoke(p);

            // Reset state
            _afterTeleported = null;
            _portalTag = null;
            _portalScene = null;
            _teleportDestination = TeleportDestination.Zero;

            // Events
            ParentModel.Trigger(GroundedEvents.AfterTeleported, ParentModel);
            if (longTeleport) {
                ParentModel.Trigger(Hero.Events.HeroLongTeleported, ParentModel);
            }
            
            Controller.Controller.enabled = true;
            // Reset controller velocity
            Controller.Controller.SimpleMove(Vector3.zero);
            
            if (_mountedTeleport) {
                ParentModel.CallMount();
                _mountedTeleport = false;
            } 
            
            ParentModel.ReturnToDefaultMovement();
        }
    }
}