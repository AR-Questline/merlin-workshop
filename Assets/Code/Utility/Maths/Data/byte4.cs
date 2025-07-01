namespace Awaken.Utility.Maths.Data {
    [System.Serializable]
    public struct byte4 {
        public byte x;
        public byte y;
        public byte z;
        public byte w;

        public byte4(byte x, byte y, byte z, byte w) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }
}
