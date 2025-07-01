using Awaken.TG.Code.Utility;
using UnityEngine;

namespace Awaken.TG.Main.General {
    /// <summary>
    /// Represents an Vector2 range with a low end and a high end, both inclusive.
    /// </summary>
    [System.Serializable]
    public struct Vector2Range {
        public Vector2 min;
        public Vector2 max;

        public Vector2Range(Vector2 min, Vector2 max) {
            this.min = new Vector2(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y));
            this.max = new Vector2(Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        // === Operations
        public Vector2 RandomPick() => new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
        public Vector2 RogueRandomPick() => new Vector2(RandomUtil.UniformFloat(min.x, max.x), RandomUtil.UniformFloat(min.y, max.y));
        public Vector2 Lerp(float t) => Vector2.Lerp(min, max, t);


        // === Conversions
        public override string ToString() => $"<{min}..{max}>";
    }
}