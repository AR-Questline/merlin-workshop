using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    public class VCAliveLocationDestroyer : VCVolumeChecker<Location> {
        protected override void OnAttach() {
            base.OnAttach();
            Target.OnVisualLoaded(t => {
                Target.TryGetElement<NpcElement>()?.ListenTo(IAlive.Events.BeforeDeath, OnOwnerDeath, this);
            });
        }
        
        protected override void OnFirstVolumeEnter(Collider other) {
            var aliveLocation = VGUtils.TryGetModel<AliveLocation>(other.gameObject);
            if (aliveLocation) {
                aliveLocation.HealthElement.Kill(Target.TryGetElement<ICharacter>());
            }
        }
        
        protected override void OnAllVolumesExit() { }
        protected override void OnStay() { }

        void OnOwnerDeath(DamageOutcome outcome) {
            if (gameObject) {
                Destroy(gameObject);
            }
        }
    }
}