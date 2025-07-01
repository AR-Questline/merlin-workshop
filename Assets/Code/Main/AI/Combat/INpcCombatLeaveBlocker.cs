using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.AI.Combat {
    public interface INpcCombatLeaveBlocker : IElement<NpcElement> {
        bool BlocksCombatExit { get; }
    }
}
