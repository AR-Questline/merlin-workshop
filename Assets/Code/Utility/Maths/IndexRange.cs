namespace Awaken.Utility.Maths {
    public struct IndexRange {
        public uint start;
        public uint length;

        public uint End => start + length;

        public IndexRange(uint start, uint length) {
            this.start = start;
            this.length = length;
        }

        public static IndexRange FromStartEnd(uint start, uint end) {
            return new IndexRange(start, end - start);
        }
    }
}
