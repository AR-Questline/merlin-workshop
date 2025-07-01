using Awaken.TG.Code.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.General {
    /// <summary>
    /// Represents an float range with a low end and a high end, both inclusive.
    /// </summary>
    [System.Serializable]
    public partial struct FloatRange {
        public ushort TypeForSerialization => SavedTypes.FloatRange;

        [Saved] public float min;
        [Saved] public float max;

        [UnityEngine.Scripting.Preserve] public readonly float Middle => (min + max) / 2f;

        public FloatRange(float min, float max) {
            if (min <= max) {
                this.min = min;
                this.max = max;
            } else {
                this.min = max;
                this.max = min;
            }
        }

        // === Operations

        public readonly bool Contains(int number) => number >= min && number <= max;
        public readonly bool Contains(float number) => number >= min && number <= max;

        public readonly float RandomPick() => Random.Range(min, max);
        public readonly float RogueRandomPick() => RandomUtil.UniformFloat(min, max);
        public readonly float Clamp(float value) => Mathf.Clamp(value, min, max);
        public readonly float Lerp(float t) => Mathf.Lerp(min, max, t);

        public void Include(FloatRange other) {
            min = Mathf.Min(min, other.min);
            max = Mathf.Max(max, other.max);
        }

        public static FloatRange operator *(FloatRange range, float multi) => new(range.min * multi, range.max * multi);
        public static FloatRange operator *(float multi, FloatRange range) => range * multi;

        // === Conversions

        public override string ToString() => $"<{min}..{max}>";
        public string ToStringInt(bool alwaysShowAsRange = false) {
            if (alwaysShowAsRange) {
                return $"<{min:0}..{max:0}>";
            } else {
                return min == max ? $"{min:0}" : $"<{min:0}..{max:0}>";
            }
        }

        public static explicit operator FloatRange(Vector2 v) => new FloatRange(v.x, v.y);
        public static explicit operator Vector2(FloatRange r) => new Vector2(r.min, r.max);
    }
}
