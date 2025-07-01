using System;

namespace Pathfinding.Util {
    [Serializable]
    public struct WaterProperties {
        public float waterDepth;
        public float waterNavMeshForgiveness;
		
        public WaterProperties(float waterDepth, float waterNavMeshForgiveness) : this()
        {
        }

        public readonly bool Equals(WaterProperties other) {
            return default;
        }

        public readonly override bool Equals(object obj)
        {
            return default;
        }

        public readonly override int GetHashCode()
        {
            return default;
        }

        public static bool operator ==(WaterProperties left, WaterProperties right)
        {
            return default;
        }

        public static bool operator !=(WaterProperties left, WaterProperties right)
        {
            return default;
        }
    }
}
