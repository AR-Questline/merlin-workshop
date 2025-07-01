using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Mobs;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Interfaces {
    /// <summary>
    /// Marks target for <see cref="ItemEquip"/> which is kind of npc.
    /// <para>Strongly related to Npc visuals changes from items.</para>
    /// </summary>
    public interface INpcEquipTarget : IEquipTarget {
        NpcTemplate Template { get; }
        NpcClothes NpcClothes { get; }
        bool CanEquip { get; }
    }
}
