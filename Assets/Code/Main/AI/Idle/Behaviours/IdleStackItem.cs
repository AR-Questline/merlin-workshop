using Awaken.TG.Main.AI.Idle.Interactions;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Behaviours {
    public readonly struct IdleStackItem {
        public readonly INpcInteraction interaction;
        public readonly bool isAnchor;
        public bool IsValid {
            get {
                if (interaction is Component c) {
                    return c != null;
                } else {
                    return interaction != null;
                }
            }
        }

        public IdleStackItem(INpcInteraction interaction, bool isAnchor) {
            this.interaction = interaction;
            this.isAnchor = isAnchor;
        }
    }
}