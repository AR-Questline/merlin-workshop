using System;
using Unity.Mathematics;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public readonly struct LineSegment2D {
        public readonly float2 start;
        public readonly float2 end;

        public LineSegment2D(float2 start, float2 end) {
            this.start = start;
            this.end = end;
        }
    }
}
