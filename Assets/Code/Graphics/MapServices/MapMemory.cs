using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Unity.Mathematics;

namespace Awaken.TG.Graphics.MapServices {
    public partial class MapMemory {
        public ushort TypeForSerialization => SavedTypes.MapMemory;

        [Saved] public float2[] visitedPixels;
    }
}