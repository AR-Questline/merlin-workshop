using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public interface INpcInteractionSearchable {
        /// <summary> Check if npc searching its neighbourhood can perform this interaction </summary>
        bool AvailableFor(NpcElement npc, IInteractionFinder finder);

        /// <summary> Check if searchable is still valid and can be used </summary>
        bool IsValid() => this is not MonoBehaviour mb || mb != null;
    }
}