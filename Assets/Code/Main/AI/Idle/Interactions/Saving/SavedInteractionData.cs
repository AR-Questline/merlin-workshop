using Awaken.TG.Main.AI.Idle.Behaviours;

namespace Awaken.TG.Main.AI.Idle.Interactions.Saving {
    public abstract partial class SavedInteractionData {
        public abstract ushort TypeForSerialization { get; }
        
        public abstract INpcInteraction TryToGetInteraction(IdleBehaviours behaviours);
    }
}
