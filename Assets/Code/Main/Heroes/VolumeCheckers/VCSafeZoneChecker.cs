using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Heroes.Interactions;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    [RequireComponent(typeof(Collider))]
    public sealed class VCSafeZoneChecker : VCHeroVolumeChecker {
        protected override void OnFirstVolumeEnter(Collider other) {
            if (!Target.HasElement<PacifistMarker>()) {
                Target.AddElement(new PacifistMarker());
            }
        }

        protected override void OnAllVolumesExit() {
            Target.TryGetElement<PacifistMarker>()?.Discard();
        }
        
        protected override void OnStay() { }
    }
}