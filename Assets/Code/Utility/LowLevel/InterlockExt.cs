using System.Threading;

namespace Awaken.Utility.LowLevel {
    public static class InterlockExt {
        public static void Min(ref int location, int newValue) {
            int valueInLocation;
            do {
                valueInLocation = location;
                if (valueInLocation <= newValue) {
                    break;
                }
            }
            while (Interlocked.CompareExchange(ref location, newValue, valueInLocation) != valueInLocation);
        }

        public static void Min(ref float location, float newValue) {
            float valueInLocation;
            do {
                valueInLocation = location;
                if (valueInLocation <= newValue) {
                    break;
                }
            }
            while (Interlocked.CompareExchange(ref location, newValue, valueInLocation) != valueInLocation);
        }
    }
}
