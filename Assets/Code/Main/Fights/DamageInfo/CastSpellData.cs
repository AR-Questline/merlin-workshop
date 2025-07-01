using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;

namespace Awaken.TG.Main.Fights.DamageInfo {
    // This should be readonly struct, but it's not supported by Unity's Visual Scripting
    public struct CastSpellData {
        public CastingHand CastingHand { get; set; }
        public Item Item { [UnityEngine.Scripting.Preserve] get; set; }
    }
}