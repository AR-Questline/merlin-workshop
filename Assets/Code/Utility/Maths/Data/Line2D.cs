using System;
using Unity.Mathematics;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public readonly struct Line2D {
        public readonly float2 point1;
        public readonly float2 point2;

        public Line2D(float2 point1, float2 point2) {
            this.point1 = point1;
            this.point2 = point2;
        }
    }
}
