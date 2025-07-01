using Awaken.TG.Main.AI.Idle.Interactions;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Fights.FPP {
    public class FakeBowEventsListener : MonoBehaviour {
        BowShootingInteraction _bowShootingInteraction;
        
        // --- Called from animator event
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void FakePull(float duration) {
            _bowShootingInteraction?.BowPull(duration);
        }

        public void SetInteraction(BowShootingInteraction bowShootingInteraction) {
            _bowShootingInteraction = bowShootingInteraction;
        }
    }
}
