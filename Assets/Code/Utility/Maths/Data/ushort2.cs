namespace Awaken.Utility.Maths.Data {
    public struct ushort2 {
        public ushort x;
        public ushort y;

        public ushort2(ushort x, ushort y) {
            this.x = x;
            this.y = y;
        }

        public override string ToString() {
            return $"({x}, {y})";
        }
    }
}
