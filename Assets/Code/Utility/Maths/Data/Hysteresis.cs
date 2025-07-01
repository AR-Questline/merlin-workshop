using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.Utility.Maths.Data {
    public partial struct Hysteresis {
        public ushort TypeForSerialization => SavedTypes.Hysteresis;

        [Saved] public float min;
        [Saved] public float max;
        public float Center => 0.5f * (min + max);
        
        Hysteresis(float min, float max) {
            this.min = min;
            this.max = max;
        }

        public static Hysteresis Centered(float center, float extend) => new(center - extend, center + extend);
        public static Hysteresis Ranged(float min, float max) => new(min, max);

        public Hysteresis Sq() => new(min * min, max * max);

        public enum Position {
            [UnityEngine.Scripting.Preserve]  Min,
            [UnityEngine.Scripting.Preserve]  Center,
            [UnityEngine.Scripting.Preserve]  Max,
        }
    }
}