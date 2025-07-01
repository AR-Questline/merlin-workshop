using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.Items.Weapons {
    public class WeaponType : RichEnum {
        public int AnimatorLayerIndex { [UnityEngine.Scripting.Preserve] get; }

        [UnityEngine.Scripting.Preserve]
        public static readonly WeaponType
            OneHanded = new WeaponType(nameof(OneHanded), 2),
            OneHandedDagger = new WeaponType(nameof(OneHandedDagger), 2),
            OneHandedSword = new WeaponType(nameof(OneHandedSword), 3),
            OneHandedAxe = new WeaponType(nameof(OneHandedAxe), 4),
            TwoHanded = new WeaponType(nameof(TwoHanded), 5),
            TwoHandedAxe = new WeaponType(nameof(TwoHandedAxe), 6),
            TwoHandedHalberd = new WeaponType(nameof(TwoHandedHalberd), 7),
            TwoHandedSword = new WeaponType(nameof(TwoHandedSword), 8),
            RangedLongbow = new WeaponType(nameof(RangedLongbow), 9),
            RangedShortbow = new WeaponType(nameof(RangedShortbow), 10),
            RangedCrossbow = new WeaponType(nameof(RangedCrossbow), 11);

        protected WeaponType(string enumName, int animatorLayerIndex, string inspectorCategory = "") : base(enumName, inspectorCategory) {
            AnimatorLayerIndex = animatorLayerIndex;
        }
    }
}