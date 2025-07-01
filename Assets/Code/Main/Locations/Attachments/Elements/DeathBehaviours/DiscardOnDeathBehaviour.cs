using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class DiscardOnDeathBehaviour : MonoBehaviour, IDeathBehaviour {
        [SerializeField] float delay = 1f;

        public bool UseDeathAnimation => false;
        public bool BlockExternalCustomDeath => true;
        public NpcDeath.DeathAnimType UseCustomDeathAnimation => NpcDeath.DeathAnimType.Default;
        
        public void OnVisualLoaded(DeathElement death, Transform transform) {}

        public void OnDeath(DamageOutcome damageOutcome, Location location) {
            DelayDiscard(location).Forget();
        }
        
        async UniTaskVoid DelayDiscard(Location location) {
            if (delay > 0) {
                if (!await AsyncUtil.DelayTime(location, delay)) {
                    return;
                }
            } else {
                if (!await AsyncUtil.DelayFrame(location)) {
                    return;
                }
            }
            if (!location.HasBeenDiscarded) {
                location.Discard();
            }
        }
    }
}