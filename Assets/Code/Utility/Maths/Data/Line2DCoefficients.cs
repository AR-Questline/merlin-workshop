using System;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public readonly struct Line2DCoefficients {
        public readonly float a;
        public readonly float b;
        public readonly float c;

        public Line2DCoefficients(float a, float b, float c) {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }
}
