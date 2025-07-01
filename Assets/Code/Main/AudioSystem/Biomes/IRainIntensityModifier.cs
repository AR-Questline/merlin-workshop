using UnityEngine;

namespace Awaken.TG.Main.AudioSystem.Biomes {
    public interface IRainIntensityModifier {
        Component Owner { get; }
        float MultiplierWhenUnderRoof { get; }
    }
}