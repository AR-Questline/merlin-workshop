using Awaken.TG.Main.Fights.NPCs;

namespace Awaken.TG.Main.AI.Idle.Interactions.Saving {
    public interface ISavableInteraction {
        bool TryLoadAndSetupSavedData(NpcElement npc, InteractionStartReason startReason);
        SavedInteractionData SaveData(NpcElement npc);
    }
}
