namespace Awaken.Utility.Maths.Data {
    [System.Serializable]
    public struct ushort4 {
        public ushort x;
        public ushort y;
        public ushort z;
        public ushort w;

        public ushort4(ushort x, ushort y, ushort z, ushort w) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static ushort4 operator |(ushort4 a, ushort4 b) {
            unchecked {
                return new ushort4((ushort)(a.x | b.x), (ushort)(a.y | b.y), (ushort)(a.z | b.z), (ushort)(a.w | b.w));
            }
        }
    }
}
