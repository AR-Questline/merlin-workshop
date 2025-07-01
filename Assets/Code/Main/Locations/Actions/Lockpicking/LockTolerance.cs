using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Locations.Actions.Lockpicking {
    public class LockTolerance : RichEnum {
        public readonly int index;
        public readonly float angle;
        /// <summary>
        /// In % 0-1 range per second
        /// </summary>
        public readonly float toolDamage;
        public readonly float xpMultiplier;

        protected LockTolerance(string enumName, int index, float angle, float toolDamage = 1f, float xpMultiplier = 1, string inspectorCategory = "") : base(enumName, inspectorCategory) {
            this.index = index;
            this.angle = angle;
            this.toolDamage = toolDamage;
            this.xpMultiplier = xpMultiplier;
        }

        [UnityEngine.Scripting.Preserve]
        public static readonly LockTolerance Trivial = new(nameof(Trivial), 0, 40, toolDamage: 0.5f),
                                             Easy = new(nameof(Easy), 1, 25),
                                             Medium = new(nameof(Medium), 2, 15, xpMultiplier: 2),
                                             Hard = new(nameof(Hard), 3, 7, toolDamage: 1.5f, xpMultiplier: 4f),
                                             Insane = new(nameof(Insane), 4, 3, toolDamage: 2f, xpMultiplier: 4f);

        public override int CompareTo(RichEnum other) {
            if (other is LockTolerance otherTolerance) {
                return angle.CompareTo(otherTolerance.angle);
            }
            return base.CompareTo(other);
        }
    }
}
