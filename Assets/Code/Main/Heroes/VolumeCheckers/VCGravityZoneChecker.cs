using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    [RequireComponent(typeof(Collider))]
    public class VCGravityZoneChecker : VCVolumeChecker<Hero> {
        [SerializeField] public EventReference onEnterGravityField, onExitGravityField;
        
        protected override void OnFirstVolumeEnter(Collider other) {
            if (!Target.HasElement<GravityMarker>()) {
                Target.AddElement(new GravityMarker(other.GetComponent<GravityChangeZone>()));
                if (!onEnterGravityField.IsNull) {
                    FMODManager.PlayOneShot(onEnterGravityField);
                }
            }
        }

        protected override void OnAllVolumesExit() {
            Target.TryGetElement<GravityMarker>()?.Discard();
            if (!onExitGravityField.IsNull) {
                FMODManager.PlayOneShot(onExitGravityField);
            }
        }
        
        protected override void OnStay() { }
    }
}