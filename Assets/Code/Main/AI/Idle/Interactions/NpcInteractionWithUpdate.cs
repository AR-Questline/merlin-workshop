using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    [RequireComponent(typeof(SphereCollider))]
    public abstract class NpcInteractionWithUpdate : NpcInteraction {
        public abstract void UnityUpdate();
    }
}