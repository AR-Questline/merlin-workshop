using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Interfaces {
    /// <summary>
    /// Interface which marks object as valid target to be used by <see cref="ItemEquip"/>
    /// </summary>
    public interface IEquipTarget : IWithItemSockets, IWithBodyFeature {}
}
