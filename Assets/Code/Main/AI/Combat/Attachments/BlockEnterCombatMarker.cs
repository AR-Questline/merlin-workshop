using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.AI.Combat.Attachments {
    /// <summary>
    /// Marker element to disallow entering combat for this NPC.
    /// </summary>
    public partial class BlockEnterCombatMarker : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;
    }
}