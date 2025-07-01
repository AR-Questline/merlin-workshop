namespace Awaken.Utility.Maths {
    public static class SimdExt {
        public static uint SimdTrailing(this uint value) => (value >> 2) << 2;
        public static long SimdTrailing(this long value) => (value >> 2) << 2;
        public static int SimdTrailing(this int value) => (value >> 2) << 2;
    }
}
