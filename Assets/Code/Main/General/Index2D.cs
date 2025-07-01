using UnityEngine;

namespace Awaken.TG.Main.General {
    /// <summary>
    /// Allows fast traveling through pixels one dimension array
    /// </summary>
    public readonly struct Index2D {
        public readonly int x;
        public readonly int y;
        public readonly int index;
            
        public Index2D(int index, int height) {
            this.index = index;
            x = index % height;
            y = index / height;
        }
            
        public Index2D(int x, int y, int index) {
            this.index = index;
            this.x = x;
            this.y = y;
        }

        [UnityEngine.Scripting.Preserve] public Index2D Up(int height) => new Index2D(x, y + 1, index + height);
        [UnityEngine.Scripting.Preserve] public Index2D Right(int height) => new Index2D(x + 1, y, index + 1);
        [UnityEngine.Scripting.Preserve] public Index2D Down(int height) => new Index2D(x, y - 1, index - height);
        [UnityEngine.Scripting.Preserve] public Index2D Left(int height) => new Index2D(x - 1, y, index - 1);

        [UnityEngine.Scripting.Preserve]
        public Vector2 ToVector2Normalized(float normalizer) {
            return new Vector2(x * normalizer, y * normalizer);
        }
    }
}