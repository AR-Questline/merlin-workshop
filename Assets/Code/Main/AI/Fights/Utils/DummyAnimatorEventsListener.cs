using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Fights.FPP {
    public class DummyAnimatorEventsListener : MonoBehaviour {
        // === Fake methods to suppress errors  "Animation event has no receiver, are you missing a component"
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void Hit() {}
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void Finish() {}
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void Combat() {}
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void TriggerAnimationEvent(string evt) {}
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void TriggerAnimationEvent(Object obj) {}
    }
}