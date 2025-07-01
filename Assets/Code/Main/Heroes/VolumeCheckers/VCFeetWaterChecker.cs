using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    public sealed class VCFeetWaterChecker : VCHeroVolumeChecker {
        bool _raycastCollision;
        bool _volumeCollision;
        
        public static class Events {
            public static readonly Event<Hero, bool> FeetWaterCollisionChanged = new(nameof(FeetWaterCollisionChanged));
        }

        protected override void OnAttach() {
            base.OnAttach();
            Target.ListenTo(VCHeroWaterChecker.Events.FeetWaterRaycastChanged, OnRaycastCollisionChanged, this);
        }

        protected override void OnFirstVolumeEnter(Collider other) {
            _volumeCollision = true;
            HandleFeetWaterCollision();
        }

        protected override void OnAllVolumesExit() {
            _volumeCollision = false;
            HandleFeetWaterCollision();
        }

        protected override void OnStay() { }

        void OnRaycastCollisionChanged(bool inWater) {
            _raycastCollision = inWater;
            HandleFeetWaterCollision();
        }

        void HandleFeetWaterCollision() {
            bool inWater = _raycastCollision || _volumeCollision;
            Target.Trigger(Events.FeetWaterCollisionChanged, inWater);
        }
    }
}