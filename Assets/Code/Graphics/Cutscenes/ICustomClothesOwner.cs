using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;

namespace Awaken.TG.Graphics.Cutscenes {
    public interface ICustomClothesOwner : IEquipTarget {
        uint? LightRenderLayerMask { get; }
        int? WeaponLayer { get; }
    }
}